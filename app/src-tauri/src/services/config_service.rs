use serde::{Deserialize, Serialize};
use std::fs;
use std::path::PathBuf;

use crate::paths;

/// 便携配置：跟 exe 放在一起，重装/升级不丢，换机器拷走即可
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase", default)]
pub struct WidgetSettings {
    /// 数据根目录（用户首次启动时选定）；None 表示尚未初始化
    pub data_dir: Option<String>,
    pub opacity: Option<u32>,
    pub nav_expanded: Option<bool>,
    pub hotkey: Option<String>,
    pub notification_enabled: Option<bool>,
    pub is_always_on_top: Option<bool>,
    /// main | widget | mini
    pub view_mode: Option<String>,
    /// 开机自启（以配置为准，启动时同步到系统）
    pub autostart: Option<bool>,
    /// AI 军师：开关 + 网关地址 + 模型
    pub ai_enabled: Option<bool>,
    pub ai_base_url: Option<String>,
    pub ai_model: Option<String>,
    pub ai_model_light: Option<String>,
    pub ai_api_key: Option<String>,
    /// 预留：Obsidian 仓库路径，将来做单向/双向同步只需读这个字段
    pub obsidian_vault: Option<String>,
    /// 外接神域：用户自主配置的 MCP 伺服进程列表
    pub mcp_servers: Option<Vec<crate::models::McpServerConfig>>,
}

impl Default for WidgetSettings {
    fn default() -> Self {
        Self {
            data_dir: None,
            opacity: Some(97),
            nav_expanded: Some(false),
            hotkey: Some("CommandOrControl+Shift+L".to_string()),
            notification_enabled: Some(true),
            is_always_on_top: Some(false),
            view_mode: Some("main".to_string()),
            autostart: Some(false),
            ai_enabled: Some(false),
            ai_base_url: Some("http://127.0.0.1:8317/v1".to_string()),
            ai_model: Some("gemini-3.8-flash-high".to_string()),
            ai_model_light: Some("gemini-3-flash".to_string()),
            ai_api_key: None,
            obsidian_vault: None,
            mcp_servers: Some(Vec::new()),
        }
    }
}

pub struct ConfigService;
static CONFIG_WRITE:std::sync::Mutex<()> = std::sync::Mutex::new(());

impl ConfigService {
    /// 便携配置路径（exe 同级）
    pub fn get_config_path() -> PathBuf {
        paths::portable_config_path()
    }

    pub fn read_at(path: &std::path::Path) -> Result<WidgetSettings,String> {
        match fs::read(path) {
            Ok(bytes) => serde_json::from_slice(&bytes).map_err(|e|format!("配置 JSON 损坏：{}（{}）",e,path.display())),
            Err(e) if e.kind()==std::io::ErrorKind::NotFound && !path.exists() => Ok(WidgetSettings::default()),
            Err(e) => Err(format!("配置无法读取：{}（{}）",e,path.display())),
        }
    }
    pub fn load_checked() -> Result<WidgetSettings,String> { Self::read_at(&Self::get_config_path()) }
    // Non-data startup defaults only. Database access and writes always use load_checked.
    pub fn load() -> WidgetSettings { Self::load_checked().unwrap_or_default() }
    fn write_at(path:&std::path::Path, settings:&WidgetSettings)->Result<(),String> {
        use std::io::Write;
        let json=serde_json::to_vec_pretty(settings).map_err(|e|e.to_string())?;
        let temporary=path.with_extension(format!("{}.tmp",uuid::Uuid::new_v4()));
        let result=(||{
            let mut file=fs::OpenOptions::new().write(true).create_new(true).open(&temporary).map_err(|e|format!("创建配置临时文件失败：{}",e))?;
            file.write_all(&json).and_then(|_|file.sync_all()).map_err(|e|format!("写入配置失败：{}",e))?;
            drop(file);
            fs::rename(&temporary,path).map_err(|e|format!("替换配置失败：{}",e))
        })();
        if result.is_err(){let _=fs::remove_file(&temporary);}
        result
    }
    pub fn update(change:impl FnOnce(&mut WidgetSettings)->Result<(),String>)->Result<WidgetSettings,String>{
        let _guard=CONFIG_WRITE.lock().map_err(|_|"配置锁不可用")?;
        let mut cfg=Self::load_checked()?;change(&mut cfg)?;
        Self::write_at(&Self::get_config_path(),&cfg)?;Ok(cfg)
    }
    pub fn recover(source:Option<String>,data_dir:Option<String>)->Result<WidgetSettings,String>{
        let _guard=CONFIG_WRITE.lock().map_err(|_|"配置锁不可用")?;
        let cfg=if let Some(source)=source {
            let bytes=fs::read(&source).map_err(|e|format!("读取恢复文件失败：{}",e))?;
            serde_json::from_slice::<WidgetSettings>(&bytes).map_err(|e|format!("恢复文件无效：{}",e))?
        }else{WidgetSettings{data_dir,..Default::default()}};
        if let Some(root)=cfg.data_dir.as_ref(){
            let root=paths::normalize_data_dir(root)?;
            crate::db::Db::open_at(&root)?;
        }
        let path=Self::get_config_path();
        if path.exists(){
            let backup=path.with_extension(format!("recovery-{}.json",uuid::Uuid::new_v4()));
            fs::copy(&path,&backup).map_err(|e|format!("无法保留配置原件，已中止恢复：{}",e))?;
        }
        Self::write_at(&path,&cfg)?;Ok(cfg)
    }

    /// 合并式更新：把前端传来的局部 JSON 合并到现有配置中，绝不因局部保存冲掉 dataDir / aiApiKey / mcpServers
    pub fn merge_with_patch(mut current: WidgetSettings, patch: &serde_json::Value) -> WidgetSettings {
        if let Some(obj) = patch.as_object() {
            if let Some(v)=obj.get("navExpanded").and_then(|v|v.as_bool()){current.nav_expanded=Some(v);}
            if let Some(v) = obj.get("dataDir").and_then(|v| v.as_str()) {
                if !v.trim().is_empty() {
                    current.data_dir = Some(v.to_string());
                }
            }
            if let Some(v) = obj.get("opacity").and_then(|v| v.as_u64()) {
                current.opacity = Some((v as u32).clamp(60, 100));
            }
            if let Some(v) = obj.get("hotkey").and_then(|v| v.as_str()) {
                if !v.trim().is_empty() {
                    current.hotkey = Some(v.to_string());
                }
            }
            if let Some(v) = obj.get("notificationEnabled").and_then(|v| v.as_bool()) {
                current.notification_enabled = Some(v);
            }
            if let Some(v) = obj.get("isAlwaysOnTop").and_then(|v| v.as_bool()) {
                current.is_always_on_top = Some(v);
            }
            if let Some(v) = obj.get("viewMode").and_then(|v| v.as_str()) {
                if !v.trim().is_empty() {
                    current.view_mode = Some(v.to_string());
                }
            }
            if let Some(v) = obj.get("autostart").and_then(|v| v.as_bool()) {
                current.autostart = Some(v);
            }
            if let Some(v) = obj.get("aiEnabled").and_then(|v| v.as_bool()) {
                current.ai_enabled = Some(v);
            }
            if let Some(v) = obj.get("aiBaseUrl").and_then(|v| v.as_str()) {
                if !v.trim().is_empty() {
                    current.ai_base_url = Some(v.to_string());
                }
            }
            if let Some(v) = obj.get("aiModel").and_then(|v| v.as_str()) {
                if !v.trim().is_empty() {
                    current.ai_model = Some(v.to_string());
                }
            }
            if let Some(v) = obj.get("aiModelLight").and_then(|v| v.as_str()) {
                current.ai_model_light = if v.trim().is_empty() {
                    None
                } else {
                    Some(v.to_string())
                };
            }
            if let Some(v) = obj.get("aiApiKey").and_then(|v| v.as_str()) {
                if !v.trim().is_empty() {
                    current.ai_api_key = Some(v.to_string());
                }
            }
            if let Some(v) = obj.get("obsidianVault").and_then(|v| v.as_str()) {
                current.obsidian_vault = if v.trim().is_empty() {
                    None
                } else {
                    Some(v.to_string())
                };
            }
            if let Some(arr) = obj.get("mcpServers") {
                if let Ok(servers) = serde_json::from_value::<Vec<crate::models::McpServerConfig>>(arr.clone()) {
                    current.mcp_servers = Some(servers);
                }
            }
        }
        current
    }

    pub fn save_merged(patch: &serde_json::Value) -> Result<WidgetSettings, String> {
        Self::update(|cfg| { *cfg=Self::merge_with_patch(cfg.clone(),patch);Ok(()) })
    }

    /// 已配置的数据根目录（None = 还没初始化，需要走首次引导）
    pub fn data_root() -> Result<Option<PathBuf>,String> {
        Ok(Self::load_checked()?.data_dir.filter(|d|!d.trim().is_empty()).map(PathBuf::from))
    }

    /// 写入数据目录并落盘配置
    pub fn set_data_root(path: &str) -> Result<(), String> {
        let root = paths::normalize_data_dir(path)?;
        paths::prepare_data_dir(&root)?;
        Self::update(|cfg|{cfg.data_dir=Some(root.to_string_lossy().to_string());Ok(())})?;
        Ok(())
    }
}
#[cfg(test)]
mod recovery_tests {
    use super::*;
    #[test]
    fn missing_corrupt_and_unreadable_config_are_distinct() {
        let root=std::env::temp_dir().join(format!("liferpg-config-{}",uuid::Uuid::new_v4()));
        fs::create_dir_all(&root).unwrap();let file=root.join("config.json");
        assert!(ConfigService::read_at(&file).unwrap().data_dir.is_none());
        fs::write(&file,b"{broken").unwrap();assert!(ConfigService::read_at(&file).is_err());
        assert_eq!(fs::read(&file).unwrap(),b"{broken");
        assert!(ConfigService::read_at(&root).is_err());
        fs::write(&file,b"{\"dataDir\":\"D:/isolated\"}").unwrap();
        assert_eq!(ConfigService::read_at(&file).unwrap().data_dir.as_deref(),Some("D:/isolated"));
        assert_eq!(root.parent(),Some(std::env::temp_dir().as_path()));fs::remove_dir_all(root).unwrap();
    }
}
