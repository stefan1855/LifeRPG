use serde::{Deserialize, Serialize};
use serde_json::Value;
use std::time::Duration;

use crate::services::config_service::{ConfigService, WidgetSettings};

/// AI 军师：走用户本地网关（OpenAI 兼容协议），可选开关，失败静默降级
pub struct AiService;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AiConfig {
    pub enabled: bool,
    pub base_url: String,
    /// 复盘等重任务用的模型
    pub model: String,
    /// 建任务等轻任务用的模型（可空则回落 model）
    pub model_light: Option<String>,
    pub api_key: Option<String>,
}

impl Default for AiConfig {
    fn default() -> Self {
        Self {
            enabled: false,
            base_url: String::new(),
            model: String::new(),
            model_light: None,
            api_key: None,
        }
    }
}

impl AiConfig {
    pub fn load() -> Self {
        Self::from_settings(ConfigService::load())
    }

    fn from_settings(cfg: WidgetSettings) -> Self {
        Self {
            enabled: cfg.ai_enabled.unwrap_or(false),
            base_url: cfg
                .ai_base_url
                .filter(|s| !s.trim().is_empty())
                .unwrap_or_default(),
            model: cfg
                .ai_model
                .filter(|s| !s.trim().is_empty())
                .unwrap_or_default(),
            model_light: cfg.ai_model_light.filter(|s| !s.trim().is_empty()),
            api_key: cfg.ai_api_key,
        }
    }
}

#[derive(Debug, Clone)]
pub struct ChatResult {
    pub content: String,
}

impl AiService {
    pub fn chat(cfg: &AiConfig, model: &str, system: &str, user: &str) -> Result<ChatResult, String> {
        if !cfg.enabled {
            return Err("AI 未开启：请到「设置 → AI 军师」打开".into());
        }
        let base = cfg.base_url.trim().trim_end_matches('/');
        if base.is_empty() {
            return Err("AI 网关地址未填写：请到「设置 → AI 军师」填写并保存".into());
        }
        let parsed = reqwest::Url::parse(base)
            .map_err(|_| "AI 网关地址无效：请填写完整的 HTTP 或 HTTPS 地址".to_string())?;
        if !matches!(parsed.scheme(), "http" | "https") || parsed.host_str().is_none() {
            return Err("AI 网关地址无效：请填写完整的 HTTP 或 HTTPS 地址".into());
        }
        let model = model.trim();
        if model.is_empty() {
            return Err("AI 模型未填写：请到「设置 → AI 军师」填写并保存".into());
        }
        let url = format!("{}/chat/completions", base);

        let body = serde_json::json!({
            "model": model,
            "messages": [
                {"role": "system", "content": system},
                {"role": "user", "content": user}
            ],
            "temperature": 0.5
        });

        let client = reqwest::blocking::Client::builder()
            .timeout(Duration::from_secs(120))
            .build()
            .map_err(|e| format!("网络客户端创建失败: {}", e))?;

        let mut req = client
            .post(&url)
            .header("Content-Type", "application/json")
            .json(&body);
        if let Some(key) = &cfg.api_key {
            if !key.trim().is_empty() {
                req = req.bearer_auth(key.trim());
            }
        }

        let resp = req.send().map_err(|e| format!("连接 AI 网关失败: {}", e))?;
        let status = resp.status();
        let text = resp.text().map_err(|e| format!("读取响应失败: {}", e))?;
        if !status.is_success() {
            return Err(format!("AI 网关返回 {}: {}", status, truncate(&text, 300)));
        }

        let v: Value = serde_json::from_str(&text).map_err(|e| format!("解析响应失败: {}", e))?;
        let content = v["choices"][0]["message"]["content"]
            .as_str()
            .map(|s| s.trim().to_string())
            .filter(|s| !s.is_empty())
            .ok_or_else(|| "AI 返回了空内容".to_string())?;
        Ok(ChatResult { content })
    }

    /// 取 JSON 的宽容解析：剥掉 ```json 围栏、截取首个 { 或 [
    pub(crate) fn parse_json(content: &str) -> Result<Value, String> {
        let s = content.trim();
        let s = s
            .strip_prefix("```json")
            .or_else(|| s.strip_prefix("```"))
            .unwrap_or(s)
            .trim()
            .trim_end_matches("```")
            .trim();
        let start = s.find(['{', '[']).ok_or("响应中没有 JSON")?;
        let end = s.rfind(['}', ']']).map(|i| i + 1).ok_or("JSON 不完整")?;
        serde_json::from_str(&s[start..end]).map_err(|e| format!("JSON 解析失败: {}", e))
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn parse_json_handles_fenced_and_prose() {
        // 裸 JSON
        let v = AiService::parse_json(r#"{"title":"健身","quadrant":"Q2"}"#).unwrap();
        assert_eq!(v["title"], "健身");

        // ```json 围栏
        let fenced = "```json\n{\"title\":\"读书\",\"experience\":25}\n```";
        let v2 = AiService::parse_json(fenced).unwrap();
        assert_eq!(v2["title"], "读书");
        assert_eq!(v2["experience"], 25);

        // 无语言标记的围栏
        let plain_fence = "```\n{\"a\":1}\n```";
        assert_eq!(AiService::parse_json(plain_fence).unwrap()["a"], 1);

        // 前后有解释文字
        let prose = "好的，解析结果如下：\n{\"title\":\"跑步\",\"dueDate\":null}\n希望有帮助。";
        let v3 = AiService::parse_json(prose).unwrap();
        assert_eq!(v3["title"], "跑步");
        assert!(v3["dueDate"].is_null());

        // 数组也能解析
        let arr = "```json\n[{\"attributeKey\":\"mind\",\"bonusValue\":5}]\n```";
        assert_eq!(AiService::parse_json(arr).unwrap()[0]["bonusValue"], 5);

        // 没有 JSON 时报错而不是 panic
        assert!(AiService::parse_json("完全没有 JSON 的一段话").is_err());
    }

    #[test]
    fn ai_disabled_by_default() {
        // 默认配置下 AI 关闭，调用应返回明确错误而不是发起网络请求
        let cfg = AiConfig {
            enabled: false,
            ..AiConfig::default()
        };
        let err = AiService::chat(&cfg, "any", "sys", "user").unwrap_err();
        assert!(err.contains("AI 未开启"), "unexpected error: {}", err);
    }

    #[test]
    fn runtime_ai_config_keeps_blanks_and_existing_user_values() {
        let defaults = AiConfig::default();
        assert!(defaults.base_url.is_empty());
        assert!(defaults.model.is_empty());
        assert!(defaults.model_light.is_none());
        assert!(defaults.api_key.is_none());
        let blank = AiConfig::from_settings(WidgetSettings::default());
        assert!(blank.base_url.is_empty());
        assert!(blank.model.is_empty());
        assert!(blank.model_light.is_none());

        let cfg = AiConfig::from_settings(WidgetSettings {
            ai_enabled: Some(true), ai_base_url: Some("https://user.example/v1".into()),
            ai_model: Some("user-model".into()), ai_model_light: Some("  ".into()),
            ai_api_key: Some("test-key".into()), ..Default::default()
        });
        assert!(cfg.enabled);
        assert_eq!(cfg.base_url, "https://user.example/v1");
        assert_eq!(cfg.model, "user-model");
        assert!(cfg.model_light.is_none());
        assert_eq!(cfg.api_key.as_deref(), Some("test-key"));
    }

    #[test]
    fn incomplete_ai_config_fails_before_network_request() {
        let mut cfg = AiConfig { enabled: true, ..Default::default() };
        let err = AiService::chat(&cfg, "", "sys", "user").unwrap_err();
        assert!(err.contains("网关地址未填写"));
        for url in ["localhost:8317", "file:///test", "not a url"] {
            cfg.base_url = url.into();
            let err = AiService::chat(&cfg, "main", "sys", "user").unwrap_err();
            assert!(err.contains("网关地址无效"), "{}", err);
        }
        cfg.base_url = "https://gateway.example/v1".into();
        let err = AiService::chat(&cfg, "  ", "sys", "user").unwrap_err();
        assert!(err.contains("模型未填写"));
    }

    #[test]
    fn gateway_error_truncation_respects_unicode_boundaries(){
        let text="错误码：请求失败。".repeat(100);let result=super::truncate(&text,300);
        assert_eq!(result.chars().count(),301);assert!(result.ends_with('…'));
    }

    /// 读取本机 DSH 凭据里的网关 key（仅用于本地联调测试）
    fn local_gateway_key() -> Option<String> {
        let home = std::env::var("USERPROFILE").ok()?;
        let path = std::path::Path::new(&home).join(".dsh").join(".credentials.yaml");
        let text = std::fs::read_to_string(path).ok()?;
        text.lines()
            .find_map(|l| l.strip_prefix("  EASYCLIPROXYAPI_API_KEY:"))
            .map(|v| v.trim().trim_matches('"').to_string())
    }

    fn live_cfg() -> AiConfig {
        AiConfig {
            enabled: true,
            base_url: "http://127.0.0.1:8317/v1".into(),
            model: "gemini-3-flash".into(),
            model_light: Some("gemini-3-flash".into()),
            api_key: local_gateway_key(),
        }
    }

    /// 真实网关联调（默认跳过，需手动 `cargo test -- --ignored --nocapture` 运行）
    #[test]
    #[ignore]
    fn live_gateway_chat() {
        let cfg = live_cfg();
        let r = AiService::chat(&cfg, &cfg.model, "你是连通性测试。", "只回复两个字：在线").unwrap();
        println!("[live] chat reply = {}", r.content);
        assert!(!r.content.trim().is_empty());
    }

    /// 真实网关联调：自然语言 → 结构化任务（验证 prompt 到 JSON 的整条链路）
    #[test]
    #[ignore]
    fn live_parse_task() {
        let r = parse_task(&live_cfg(), "明天下午三点去健身房练一小时，挺重要的", "2026-10-29").unwrap();
        println!(
            "[live] parsed = title={} quadrant={} attr={} exp={} due={:?}",
            r.title, r.quadrant, r.growth_attribute, r.experience, r.due_date
        );
        assert!(!r.title.trim().is_empty());
        assert!(["Q1", "Q2", "Q3", "Q4"].contains(&r.quadrant.as_str()));
        assert!([
            "physique", "mind", "discipline", "expression", "creativity", "resources"
        ]
        .contains(&r.growth_attribute.as_str()));
        assert!(r.experience > 0);
    }
}

fn truncate(s: &str, n: usize) -> String {
    let value: String = s.chars().take(n).collect();
    if s.chars().count()>n {format!("{}…",value)} else {value}
}

// ==================== 四个能力 ====================

/// 1) 周期复盘：任务 + 记录 + 属性 + (可选外接神域 MCP 工具数据) → 一篇复盘
pub fn review(cfg: &AiConfig, summary: &Value) -> Result<String, String> {
    if !cfg.enabled { return Err("AI 未开启：请到设置中启用 AI 军师".into()); }
    let system = String::from("你是暗黑哥特风人生 RPG 的军师。用中文写一篇简短有力的复盘，\
        结构：① 本期战果（列举关键完成）② 六维契约观察（谁强谁弱、为什么）③ 下期建议（最多三条，具体可执行）。\
        克制、务实，不超过 500 字。只根据提供的任务、记录和属性分析；没有提供的外部会议、账号或工具数据不得臆测。");

    AiService::chat(&cfg, &cfg.model, &system, &summary.to_string()).map(|r| r.content)
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ParsedTask {
    pub title: String,
    pub quadrant: String,
    pub growth_attribute: String,
    pub experience: i32,
    pub due_date: Option<String>,
    pub due_at: Option<String>,
}

/// 2) 自然语言 → 结构化任务
pub fn parse_task(cfg: &AiConfig, text: &str, today: &str) -> Result<ParsedTask, String> {
    let model = cfg.model_light.clone().unwrap_or_else(|| cfg.model.clone());
    let system = format!(
        "你是任务解析器。把一句中文话解析成任务 JSON。当前本地时间为 {}。\
         只输出 JSON：{{\"title\":\"简短任务名\",\"quadrant\":\"Q1|Q2|Q3|Q4\",\
         \"growthAttribute\":\"physique|mind|discipline|expression|creativity|resources\",\
         \"experience\":5-50的整数,\"dueDate\":\"YYYY-MM-DD 或 null\",\"dueAt\":\"带时区的 RFC3339 日期时间或 null\"}}。\
         象限规则：有明确截止且重要=Q1；重要无截止=Q2；琐事紧急=Q3；其他=Q4。\
         经验参考：Q1=30 Q2=25 Q3=15 Q4=5，按工作量微调。没有日期信息则 dueDate、dueAt 为 null。有明确时间时必须保留到 dueAt，沿用当前时间的时区；只给日期时设为当天23:59:59。不要编造日期或时间。",
        today
    );
    let r = AiService::chat(&cfg, &model, &system, text)?;
    let v = AiService::parse_json(&r.content)?;
    let quadrant = v["quadrant"].as_str().unwrap_or("Q2").to_string();
    let growth = v["growthAttribute"].as_str().unwrap_or("mind").to_string();
    Ok(ParsedTask {
        title: v["title"].as_str().unwrap_or(text).to_string(),
        quadrant: if ["Q1", "Q2", "Q3", "Q4"].contains(&quadrant.as_str()) {
            quadrant
        } else {
            "Q2".into()
        },
        growth_attribute: if [
            "physique", "mind", "discipline", "expression", "creativity", "resources",
        ]
        .contains(&growth.as_str())
        {
            growth
        } else {
            "mind".into()
        },
        experience: v["experience"].as_i64().unwrap_or(25).clamp(5,50) as i32,
        due_date: v["dueDate"].as_str().filter(|s|chrono::NaiveDate::parse_from_str(s,"%Y-%m-%d").is_ok()).map(str::to_string),
        due_at: match v["dueAt"].as_str() {
            Some(value) => Some(chrono::DateTime::parse_from_rfc3339(value).map_err(|_|"AI 返回的截止时间无效，请手动填写")?.to_rfc3339()),
            None => None,
        },
    })
}

/// 3) 现实物品 → 游戏名 + 效果 + 属性建议
pub fn name_equipment(cfg: &AiConfig, real_name: &str, real_effect: &str) -> Result<Value, String> {
    let model = cfg.model_light.clone().unwrap_or_else(|| cfg.model.clone());
    let system = "你是暗黑哥特游戏的世界观作者。把一件现实物品翻译成游戏装备。\
        只输出 JSON：{\"gameItemName\":\"哥特风名字\",\"realEffect\":\"一句话现实效果（直接采用用户描述的语意）\",\
        \"bonuses\":[{\"attributeKey\":\"physique|mind|discipline|expression|creativity|resources\",\"bonusValue\":1-10}]}。\
        最多两条加成，总值不超过 15，要贴合这件物品的实际用途。";
    let user = format!("现实物品：{}\n实际用途：{}", real_name, real_effect);
    let r = AiService::chat(&cfg, &model, &system, &user)?;
    AiService::parse_json(&r.content)
}

/// 4) 属性教练：六维数据 → 建议
pub fn coach(cfg: &AiConfig, snapshot: &Value) -> Result<String, String> {
    let model = cfg.model_light.clone().unwrap_or_else(|| cfg.model.clone());
    let system = "你是人生 RPG 的属性教练。根据六维属性数据，用中文给一条 80 字以内的具体建议：\
        指出最弱的维度、说明它为什么重要、推荐一个本周可执行的任务方向。直接输出建议正文，不要标题。";
    AiService::chat(&cfg, &model, &system, &snapshot.to_string()).map(|r| r.content)
}


