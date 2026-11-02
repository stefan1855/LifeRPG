use std::io::{BufRead, BufReader, Write};
use std::process::{Command, Stdio};
use std::sync::mpsc;
use std::time::Duration;
use serde_json::{json, Value};

use crate::models::{McpServerConfig, McpToolCallResult, McpToolDefinition};

const MCP_SESSION_TIMEOUT_SECS: u64 = 25;

/// 通用 MCP 客户端（基于 stdio JSON-RPC 2.0 规范，带硬超时与参数引号解析保护）
pub struct McpService;

impl McpService {
    /// 支持双引号与单引号的命令行参数切分器，防止带空格的 Windows 路径被拆断
    pub fn split_args(raw: &str) -> Vec<String> {
        let mut out = Vec::new();
        let mut current = String::new();
        let mut quote: Option<char> = None;

        for ch in raw.chars() {
            match quote {
                Some(q) => {
                    if ch == q {
                        quote = None;
                    } else {
                        current.push(ch);
                    }
                }
                None => {
                    if ch == '"' || ch == '\'' {
                        quote = Some(ch);
                    } else if ch.is_whitespace() {
                        if !current.is_empty() {
                            out.push(std::mem::take(&mut current));
                        }
                    } else {
                        current.push(ch);
                    }
                }
            }
        }
        if !current.is_empty() {
            out.push(current);
        }
        out
    }

    /// 与指定的 MCP 伺服进程进行带硬超时的通信会话
    fn execute_stdio_session<F>(
        cfg: &McpServerConfig,
        action: F,
    ) -> Result<Value, String>
    where
        F: FnOnce(&mut dyn Write, &mut dyn BufRead) -> Result<Value, String> + Send + 'static,
    {
        let mut cmd = Command::new(&cfg.command);
        // MCP is UTF-8; PYTHONIOENCODING alone does not affect subprocess text decoding.
        cmd.env("PYTHONUTF8","1").env("PYTHONIOENCODING","utf-8");

        // 若前端传入单个含空格字符串或正常数组，兼容处理
        let normalized_args = cfg.args.clone();

        cmd.args(&normalized_args)
            .stdin(Stdio::piped())
            .stdout(Stdio::piped())
            .stderr(Stdio::null());

        for (k, v) in &cfg.env {
            cmd.env(k, v);
        }

        #[cfg(windows)]
        {
            use std::os::windows::process::CommandExt;
            const CREATE_NO_WINDOW: u32 = 0x08000000;
            cmd.creation_flags(CREATE_NO_WINDOW);
        }

        let mut child = cmd
            .spawn()
            .map_err(|e| format!("启动 MCP 进程 [{}] 失败: {}", cfg.name, e))?;

        let mut stdin = child
            .stdin
            .take()
            .ok_or_else(|| "无法获取 MCP stdin 句柄".to_string())?;
        let stdout = child
            .stdout
            .take()
            .ok_or_else(|| "无法获取 MCP stdout 句柄".to_string())?;

        let (tx, rx) = mpsc::channel();
        std::thread::spawn(move || {
            let mut reader = BufReader::new(stdout);
            let res = action(&mut stdin, &mut reader);
            let _ = tx.send(res);
        });

        let outcome = match rx.recv_timeout(Duration::from_secs(MCP_SESSION_TIMEOUT_SECS)) {
            Ok(res) => res,
            Err(mpsc::RecvTimeoutError::Timeout) => Err(format!(
                "MCP 进程 [{}] 响应超时（超过 {} 秒），已自动终止子进程",
                cfg.name, MCP_SESSION_TIMEOUT_SECS
            )),
            Err(mpsc::RecvTimeoutError::Disconnected) => {
                Err(format!("MCP 进程 [{}] 通信通道意外断开", cfg.name))
            }
        };

        let _ = child.kill();
        let _ = child.wait();

        outcome
    }

    /// 发送并等待一条单行 JSON-RPC 响应
    fn rpc_roundtrip(
        stdin: &mut dyn Write,
        reader: &mut dyn BufRead,
        request: &Value,
    ) -> Result<Value, String> {
        let mut req_str = request.to_string();
        req_str.push('\n');

        stdin
            .write_all(req_str.as_bytes())
            .map_err(|e| format!("向 MCP 写入请求失败: {}", e))?;
        stdin.flush().map_err(|e| format!("刷新管道失败: {}", e))?;

        let mut line = String::new();
        let max_attempts = 60;
        for _ in 0..max_attempts {
            line.clear();
            let bytes = reader
                .read_line(&mut line)
                .map_err(|e| format!("从 MCP 读取响应失败: {}", e))?;
            if bytes == 0 {
                return Err("MCP 进程意外关闭输出流（请检查脚本路径与依赖环境是否正确）".to_string());
            }
            let trimmed = line.trim();
            if trimmed.is_empty() {
                continue;
            }
            if let Ok(val) = serde_json::from_str::<Value>(trimmed) {
                if let Some(method) = val.get("method").and_then(Value::as_str) {
                    if let Some(id) = val.get("id") {
                        let reply = if method == "ping" { json!({"jsonrpc":"2.0","id":id,"result":{}}) }
                            else {json!({"jsonrpc":"2.0","id":id,"error":{"code":-32601,"message":"Unsupported client method"}})};
                        writeln!(stdin,"{}",reply).and_then(|_| stdin.flush()).map_err(|e|e.to_string())?;
                    }
                    continue;
                }
                if val.get("id") != request.get("id") { continue; }
                if val.is_object() && (val.get("result").is_some() || val.get("error").is_some()) {
                    if let Some(err) = val.get("error") {
                        return Err(format!("MCP 服务端报错: {}", err));
                    }
                    return Ok(val.get("result").cloned().unwrap_or(Value::Null));
                }
            }
        }
        Err(format!("未能收到 MCP 的标准 JSON-RPC 响应，最后输出: {}", line.trim()))
    }

    /// 标准握手与握手通知
    fn perform_handshake(
        stdin: &mut dyn Write,
        reader: &mut dyn BufRead,
    ) -> Result<(), String> {
        let init_req = json!({
            "jsonrpc": "2.0",
            "id": 1,
            "method": "initialize",
            "params": {
                "protocolVersion": "2024-11-05",
                "capabilities": {
                    "tools": {}
                },
                "clientInfo": {
                    "name": "LifeRPG-Client",
                    "version": env!("CARGO_PKG_VERSION")
                }
            }
        });

        Self::rpc_roundtrip(stdin, reader, &init_req)?;

        let notify = json!({
            "jsonrpc": "2.0",
            "method": "notifications/initialized"
        });
        let mut notify_str = notify.to_string();
        notify_str.push('\n');
        stdin.write_all(notify_str.as_bytes()).and_then(|_| stdin.flush()).map_err(|e|e.to_string())?;

        Ok(())
    }

    /// 测试 MCP 握手并拉取其暴露的工具清单
    pub fn list_tools_for_server(cfg: &McpServerConfig) -> Result<Vec<McpToolDefinition>, String> {
        let result = Self::execute_stdio_session(cfg, |stdin, reader| {
            Self::perform_handshake(stdin, reader)?;

            let mut all = Vec::new();
            let mut cursor: Option<Value> = None;
            let mut seen = std::collections::HashSet::new();
            for page in 0..20 {
                let params = cursor.as_ref().map(|c| json!({"cursor":c})).unwrap_or(json!({}));
                let result = Self::rpc_roundtrip(stdin,reader,&json!({"jsonrpc":"2.0","id":2+page,"method":"tools/list","params":params}))?;
                all.extend(result.get("tools").and_then(Value::as_array).ok_or("MCP 工具清单格式无效")?.iter().cloned());
                cursor = result.get("nextCursor").filter(|c| !c.is_null()).cloned();
                if let Some(c) = &cursor { if !seen.insert(c.to_string()) { return Err("MCP 返回了重复分页游标".into()); } }
                else { return Ok(json!({"tools":all})); }
            }
            Err("MCP 工具清单超过分页上限".into())
        })?;

        let mut defs = Vec::new();
        if let Some(tools) = result.get("tools").and_then(|t| t.as_array()) {
            for t in tools {
                let name = t.get("name").and_then(|v| v.as_str()).unwrap_or("").to_string();
                if name.is_empty() {
                    continue;
                }
                let desc = t.get("description").and_then(|v| v.as_str()).map(|s| s.to_string());
                let schema = t.get("inputSchema").cloned().unwrap_or_else(|| json!({"type": "object"}));

                defs.push(McpToolDefinition {
                    server_id: cfg.id.clone(),
                    server_name: cfg.name.clone(),
                    name,
                    description: desc,
                    input_schema: schema,
                });
            }
        }

        Ok(defs)
    }

    /// 执行指定的 MCP 工具调用
    pub fn call_tool(
        cfg: &McpServerConfig,
        tool_name: &str,
        args: &Value,
    ) -> Result<McpToolCallResult, String> {
        if !args.is_object(){return Err("工具参数必须是 JSON 对象".into());}
        let t_name = tool_name.to_string();
        let mut t_args = args.clone();
        if tool_name=="douyin_analyze_account" {
            let raw=args["url"].as_str().ok_or("请填写抖音账号主页或账号分享链接")?;
            t_args["url"]=Value::String(Self::douyin_url(raw)?);
        }

        let result = Self::execute_stdio_session(cfg, move |stdin, reader| {
            Self::perform_handshake(stdin, reader)?;

            let call_req = json!({
                "jsonrpc": "2.0",
                "id": 3,
                "method": "tools/call",
                "params": {
                    "name": t_name,
                    "arguments": t_args
                }
            });

            Self::rpc_roundtrip(stdin, reader, &call_req)
        })?;

        let mut is_error = result.get("isError").and_then(|v| v.as_bool()).unwrap_or(false);
        let mut content_text = String::new();

        if let Some(content_arr) = result.get("content").and_then(|c| c.as_array()) {
            for item in content_arr {
                if let Some(text) = item.get("text").and_then(|t| t.as_str()) {
                    if !content_text.is_empty() {
                        content_text.push('\n');
                    }
                    content_text.push_str(text);
                }
            }
        } else {
            content_text = result.to_string();
        }
        // Some existing servers put an error envelope in text but omit isError.
        if Self::business_error(&content_text){is_error=true;}

        Ok(McpToolCallResult {
            is_error,
            content: content_text,
        })
    }

    fn business_error(text:&str)->bool {
        serde_json::from_str::<Value>(text).ok().and_then(|v|v.as_object().cloned()).map(|o|{
            o.get("error").map(|v|!v.is_null() && v!=false && v!="").unwrap_or(false)
                && o.keys().all(|k|["error","code","message","troubleshooting_hint","detail","details"].contains(&k.as_str()))
        }).unwrap_or(false)
    }
    fn douyin_url(raw:&str)->Result<String,String>{
        let start=raw.find("https://").ok_or("请提供 HTTPS 抖音账号主页或账号分享链接")?;
        let url=raw[start..].split_whitespace().next().unwrap_or("").trim_end_matches(['，','。',',','；',')',']','】']);
        if !url.is_ascii() || url.chars().any(|c|"'\"`$\\;<>|".contains(c)) {return Err("链接包含不支持的字符，请重新复制账号主页链接".into());}
        let mut parsed=reqwest::Url::parse(url).map_err(|_|"抖音链接格式无效")?;
        let profile=matches!(parsed.host_str(),Some("www.douyin.com"|"douyin.com")) && parsed.path().starts_with("/user/") && parsed.path().len()>6;
        let share=parsed.host_str()==Some("v.douyin.com") && parsed.path().len()>1;
        if parsed.scheme()!="https" || !parsed.username().is_empty() || parsed.password().is_some() || parsed.port().is_some() || (!profile&&!share) {return Err("推荐首页无法查询账号，请提供 /user/ 个人主页或 v.douyin.com 账号分享链接".into());}
        if profile { parsed.set_query(None); parsed.set_fragment(None); }
        Ok(parsed.to_string())
    }

    /// 聚合所有已启用的 MCP 伺服的工具清单
    pub fn list_all_enabled_tools(servers: &[McpServerConfig]) -> Vec<McpToolDefinition> {
        let mut all = Vec::new();
        for s in servers {
            if !s.enabled {
                continue;
            }
            if let Ok(tools) = Self::list_tools_for_server(s) {
                all.extend(tools);
            }
        }
        all
    }
}

#[cfg(test)]
mod protocol_tests {
    use super::*;
    #[test]
    fn matches_rpc_id_and_answers_server_ping(){
        let mut input=std::io::Cursor::new(b"{\"jsonrpc\":\"2.0\",\"id\":999,\"result\":{\"wrong\":true}}\n{\"jsonrpc\":\"2.0\",\"id\":80,\"method\":\"ping\"}\n{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"right\":true}}\n".to_vec());
        let mut output=Vec::new();let result=McpService::rpc_roundtrip(&mut output,&mut input,&json!({"jsonrpc":"2.0","id":2,"method":"tools/list"})).unwrap();assert_eq!(result["right"],true);let output=String::from_utf8(output).unwrap();assert!(output.contains("\"id\":80"));
    }
    #[test]
    fn business_error_envelope_is_not_a_successful_tool_result(){
        assert!(McpService::business_error(r#"{"error":"access denied","code":500125,"message":"whitelist"}"#));
        assert!(McpService::business_error(r#"{"error":"network failure"}"#));
        assert!(!McpService::business_error(r#"{"error":null,"data":[]}"#));assert!(!McpService::business_error(r#"{"error":"described incident","record":1}"#));
    }
    #[test]
    fn douyin_profile_and_share_only_without_shell_metacharacters(){
        assert!(McpService::douyin_url("https://www.douyin.com/?recommend=1").is_err());
        assert!(McpService::douyin_url("https://www.douyin.com/user/test123").is_ok());
        assert_eq!(McpService::douyin_url("复制打开抖音 https://v.douyin.com/test123/ 查看账号").unwrap(),"https://v.douyin.com/test123/");
        assert!(McpService::douyin_url("https://www.douyin.com/user/$(whoami)").is_err());assert!(McpService::douyin_url("https://www.douyin.com.evil/user/test").is_err());
    }
}
