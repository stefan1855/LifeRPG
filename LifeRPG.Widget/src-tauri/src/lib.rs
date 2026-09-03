pub mod commands;
pub mod db;
pub mod desktop;
pub mod models;
pub mod rpg_engine;
pub mod services;

use commands::*;
use tauri::Manager;

pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_autostart::init(
            tauri_plugin_autostart::MacosLauncher::LaunchAgent,
            None,
        ))
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_global_shortcut::Builder::new().build())
        .plugin(tauri_plugin_single_instance::init(|app, _args, _cwd| {
            if let Some(window) = app.get_webview_window("main") {
                let _ = window.show();
                let _ = window.set_focus();
            }
        }))
        .setup(|app| {
            // 初始化数据库与基础角色数据
            if let Ok(conn) = db::Db::open() {
                let _ = db::Db::ensure_schema(&conn);
            }

            if let Some(window) = app.get_webview_window("main") {
                #[cfg(windows)]
                let _ = desktop::setup_desktop_widget_mode(&window);
            }

            // 注册保存的快捷键
            let cfg = services::config_service::ConfigService::load();
            if let Some(key) = cfg.hotkey {
                let _ = set_hotkey(app.handle().clone(), key);
            }

            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            rpg_get_dashboard,
            rpg_get_character,
            rpg_get_attributes,
            rpg_get_logs,
            task_list,
            task_get_categories,
            task_create,
            task_update,
            task_delete,
            task_complete,
            task_uncomplete,
            config_get,
            config_save,
            get_autostart,
            set_autostart,
            set_hotkey,
            app_exit,
            window_set_view_mode,
            window_toggle_mini,
            window_toggle_pin,
        ])
        .run(tauri::generate_context!())
        .expect("运行 LifeRPG HUD 时发生错误");
}
