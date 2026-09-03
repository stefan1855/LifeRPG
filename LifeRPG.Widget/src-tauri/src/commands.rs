use crate::db::Db;
use crate::models::{
    ActionTask, AttributeLog, Character, CharacterAttribute, CreateTaskInput, DashboardSnapshot,
    RpgExperienceResult, UpdateTaskInput,
};
use crate::services::action_task_service::ActionTaskService;
use crate::services::character_service::CharacterService;
use crate::services::config_service::{ConfigService, WidgetSettings};
use tauri::{AppHandle, Manager, WebviewWindow, LogicalSize, Size};
use tauri_plugin_autostart::ManagerExt;
use tauri_plugin_global_shortcut::{GlobalShortcutExt, Shortcut};

#[tauri::command]
pub fn rpg_get_dashboard() -> Result<DashboardSnapshot, String> {
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    CharacterService::get_dashboard_snapshot(&conn).map_err(|e| format!("获取仪表盘失败: {}", e))
}

#[tauri::command]
pub fn rpg_get_character() -> Result<Character, String> {
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    CharacterService::get_default_character(&conn).map_err(|e| format!("获取角色失败: {}", e))
}

#[tauri::command]
pub fn rpg_get_attributes() -> Result<Vec<CharacterAttribute>, String> {
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    let character = CharacterService::get_default_character(&conn).map_err(|e| format!("获取角色失败: {}", e))?;
    CharacterService::get_attributes(&conn, &character.id).map_err(|e| format!("获取属性失败: {}", e))
}

#[tauri::command]
pub fn rpg_get_logs(limit: Option<usize>) -> Result<Vec<AttributeLog>, String> {
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    CharacterService::get_recent_logs(&conn, limit.unwrap_or(50)).map_err(|e| format!("获取日志失败: {}", e))
}

#[tauri::command]
pub fn task_list(status: Option<String>, category: Option<String>) -> Result<Vec<ActionTask>, String> {
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    ActionTaskService::list_tasks(&conn, status.as_deref(), category.as_deref())
        .map_err(|e| format!("查询任务列表失败: {}", e))
}

#[tauri::command]
pub fn task_get_categories() -> Result<Vec<String>, String> {
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    ActionTaskService::get_categories(&conn).map_err(|e| format!("查询分类列表失败: {}", e))
}

#[tauri::command]
pub fn task_create(input: CreateTaskInput) -> Result<ActionTask, String> {
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    ActionTaskService::create_task(&conn, input).map_err(|e| format!("创建任务失败: {}", e))
}

#[tauri::command]
pub fn task_update(input: UpdateTaskInput) -> Result<bool, String> {
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    ActionTaskService::update_task(&conn, input).map_err(|e| format!("更新任务失败: {}", e))
}

#[tauri::command]
pub fn task_delete(id: String) -> Result<bool, String> {
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    ActionTaskService::delete_task(&conn, &id).map_err(|e| format!("删除任务失败: {}", e))
}

#[tauri::command]
pub fn task_complete(id: String) -> Result<RpgExperienceResult, String> {
    let mut conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    CharacterService::apply_task_completion(&mut conn, &id).map_err(|e| format!("完成任务结算失败: {}", e))
}

#[tauri::command]
pub fn task_uncomplete(id: String) -> Result<bool, String> {
    let mut conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    CharacterService::rollback_task_completion(&mut conn, &id).map_err(|e| format!("回滚任务状态失败: {}", e))
}

#[tauri::command]
pub fn config_get() -> Result<WidgetSettings, String> {
    Ok(ConfigService::load())
}

#[tauri::command]
pub fn config_save(settings: WidgetSettings) -> Result<(), String> {
    ConfigService::save(&settings)
}

#[tauri::command]
pub fn get_autostart(app: AppHandle) -> Result<bool, String> {
    app.autolaunch().is_enabled().map_err(|e| e.to_string())
}

#[tauri::command]
pub fn set_autostart(app: AppHandle, enabled: bool) -> Result<(), String> {
    let autolaunch = app.autolaunch();
    if enabled {
        autolaunch.enable().map_err(|e| e.to_string())
    } else {
        autolaunch.disable().map_err(|e| e.to_string())
    }
}

#[tauri::command]
pub fn set_hotkey(app: AppHandle, key: String) -> Result<(), String> {
    let shortcut: Shortcut = key.parse().map_err(|_| "无效快捷键格式".to_string())?;
    let manager = app.global_shortcut();
    let _ = manager.unregister_all();

    manager.on_shortcut(shortcut, move |app_handle, _sc, event| {
        if event.state() == tauri_plugin_global_shortcut::ShortcutState::Pressed {
            if let Some(window) = app_handle.get_webview_window("main") {
                if let Ok(visible) = window.is_visible() {
                    if visible {
                        let _ = window.hide();
                    } else {
                        let _ = window.show();
                        let _ = window.set_focus();
                    }
                }
            }
        }
    }).map_err(|e| format!("注册全局快捷键失败: {}", e))?;

    let mut cfg = ConfigService::load();
    cfg.hotkey = Some(key);
    let _ = ConfigService::save(&cfg);

    Ok(())
}

/// 优雅退出应用
#[tauri::command]
pub fn app_exit(app: AppHandle) -> Result<(), String> {
    app.exit(0);
    Ok(())
}

/// 窗口形态多档切换（大屏指挥中心 main 1460x920 / 桌面挂件 widget 340x600 / 胶囊护符 mini 260x44）
#[tauri::command]
pub fn window_set_view_mode(window: WebviewWindow, mode: String) -> Result<(), String> {
    // 始终保持任务栏图标可见与 Alt+Tab 可见
    let _ = window.set_skip_taskbar(false);
    match mode.as_str() {
        "main" => {
            let _ = window.set_min_size(Some(Size::Logical(LogicalSize::new(1100.0, 700.0))));
            let _ = window.set_size(Size::Logical(LogicalSize::new(1460.0, 920.0)));
            let _ = window.set_resizable(true);
            let _ = window.center();
        }
        "widget" => {
            let _ = window.set_min_size(Some(Size::Logical(LogicalSize::new(280.0, 300.0))));
            let _ = window.set_size(Size::Logical(LogicalSize::new(340.0, 600.0)));
            let _ = window.set_resizable(true);
        }
        "mini" => {
            let _ = window.set_min_size(Some(Size::Logical(LogicalSize::new(200.0, 36.0))));
            let _ = window.set_size(Size::Logical(LogicalSize::new(260.0, 44.0)));
            let _ = window.set_resizable(false);
        }
        _ => {}
    }
    Ok(())
}

/// 物理缩放窗口尺寸（兼容旧接口）
#[tauri::command]
pub fn window_toggle_mini(window: WebviewWindow, mini: bool) -> Result<(), String> {
    if mini {
        window_set_view_mode(window, "mini".to_string())
    } else {
        window_set_view_mode(window, "widget".to_string())
    }
}

/// 真实置顶切换（通过 Tauri API + Win32 HWND_TOPMOST 双保险）
#[tauri::command]
pub fn window_toggle_pin(window: WebviewWindow, pin: bool) -> Result<(), String> {
    window.set_always_on_top(pin).map_err(|e| e.to_string())?;

    #[cfg(windows)]
    {
        use windows::Win32::Foundation::HWND;
        use windows::Win32::UI::WindowsAndMessaging::{
            SetWindowPos, HWND_NOTOPMOST, HWND_TOPMOST, SWP_NOMOVE, SWP_NOSIZE,
        };
        if let Ok(hwnd_raw) = window.hwnd() {
            let hwnd = HWND(hwnd_raw.0 as _);
            unsafe {
                let insert_after = if pin { HWND_TOPMOST } else { HWND_NOTOPMOST };
                let _ = SetWindowPos(hwnd, insert_after, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
        }
    }

    Ok(())
}
