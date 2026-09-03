use serde::{Deserialize, Serialize};
use std::fs;
use std::path::PathBuf;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct WidgetSettings {
    pub opacity: Option<u32>,
    pub hotkey: Option<String>,
    pub notification_enabled: Option<bool>,
    pub is_always_on_top: Option<bool>,
    pub width: Option<f64>,
    pub height: Option<f64>,
    pub x: Option<i32>,
    pub y: Option<i32>,
}

impl Default for WidgetSettings {
    fn default() -> Self {
        Self {
            opacity: Some(95),
            hotkey: Some("ctrl+shift+r".to_string()),
            notification_enabled: Some(true),
            is_always_on_top: Some(false),
            width: Some(340.0),
            height: Some(620.0),
            x: None,
            y: None,
        }
    }
}

pub struct ConfigService;

impl ConfigService {
    fn get_config_path() -> PathBuf {
        if let Some(dir) = dirs::data_local_dir() {
            let widget_dir = dir.join("LifeRPG.Widget");
            let _ = fs::create_dir_all(&widget_dir);
            return widget_dir.join("config.json");
        }
        PathBuf::from("config.json")
    }

    pub fn load() -> WidgetSettings {
        let path = Self::get_config_path();
        if let Ok(content) = fs::read_to_string(&path) {
            if let Ok(settings) = serde_json::from_str::<WidgetSettings>(&content) {
                return settings;
            }
        }
        WidgetSettings::default()
    }

    pub fn save(settings: &WidgetSettings) -> Result<(), String> {
        let path = Self::get_config_path();
        let json = serde_json::to_string_pretty(settings)
            .map_err(|e| format!("序列化配置失败: {}", e))?;
        fs::write(&path, json).map_err(|e| format!("写入配置文件失败: {}", e))?;
        Ok(())
    }
}
