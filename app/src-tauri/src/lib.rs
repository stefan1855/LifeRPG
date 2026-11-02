pub mod commands;
pub mod db;
pub mod desktop;
pub mod models;
pub mod operations;
pub mod paths;
pub mod rpg_engine;
pub mod services;

use commands::*;
use tauri::Manager;
use tauri_plugin_autostart::ManagerExt;

pub fn run() {
    // 把 WebView2 的缓存/GPU 数据放到 exe 同级，保持 C 盘干净（必须在创建窗口前设置）
    let runtime = paths::webview_data_dir();
    std::fs::create_dir_all(&runtime).expect("程序目录不可写，无法建立运行时缓存；请移动程序到可写目录");
    std::env::set_var("WEBVIEW2_USER_DATA_FOLDER", runtime);

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
            // 只有已经配置过数据目录时才初始化数据库；否则由前端引导用户选择
            if db::Db::data_root().is_ok() {
                match db::Db::open() {
                    Ok(conn) => {
                        let _ = db::Db::ensure_schema(&conn);
                    }
                    Err(e) => eprintln!("[LifeRPG] 数据库初始化失败: {}", e),
                }
            }

            if let Some(window) = app.get_webview_window("main") {
                #[cfg(windows)]
                let _ = desktop::setup_desktop_widget_mode(&window);
            }

            // Apply monitor constraints before the setup page is rendered too.
            if let Some(window) = app.get_webview_window("main") {
                let cfg = services::config_service::ConfigService::load();
                let mode = if cfg.data_dir.is_some() { cfg.view_mode.unwrap_or_else(|| "main".into()) } else { "main".into() };
                if services::config_service::ConfigService::load_checked().is_ok(){window_set_view_mode(window, mode)?;}
                else{fit_window_to_monitor(&window,"main")?;}
            }

            // 兜底：异常退出后系统可能记住最小化/隐藏状态，启动时强制还原并置前，
            // 避免出现「进程在跑但没有界面」的黑洞。
            if let Some(window) = app.get_webview_window("main") {
                let _ = window.show();
                let _ = window.unminimize();
                let _ = window.set_focus();
            }

            // 托盘：关闭窗口后应用常驻，可从这里唤回
            desktop::setup_tray(app)?;

            // 注册保存的快捷键与恢复置顶状态
            let cfg = services::config_service::ConfigService::load();
            if let Some(key) = cfg.hotkey {
                let _ = set_hotkey(app.handle().clone(), key);
            }
            if cfg.is_always_on_top == Some(true) {
                if let Some(window) = app.get_webview_window("main") {
                    let _ = window.set_always_on_top(true);
                }
            }

            std::thread::spawn(move || {
                loop {
                    if let Ok(_operation) = operations::read() {
                        let cfg = services::config_service::ConfigService::load();
                        if cfg.notification_enabled == Some(true) && cfg.data_dir.is_some() {
                            if let Ok(conn) = db::Db::open() {
                                if let Err(error) = services::notification_service::NotificationService::dispatch(&conn, |id,title| services::notification_service::NotificationService::send(id,"LifeRPG · 任务到期",title)) {
                                    eprintln!("[LifeRPG] 到期检查失败: {}",error);
                                }
                            }
                        }
                    }
                    std::thread::sleep(std::time::Duration::from_secs(30));
                }
            });

            // 开机自启：以配置为准同步一次真实状态（防止注册表被外部改动后失配）
            let config_valid=services::config_service::ConfigService::load_checked().is_ok();
            let want = cfg.autostart.unwrap_or(false);
            let handle = app.handle().clone();
            std::thread::spawn(move || {
                let current = handle.autolaunch().is_enabled().unwrap_or(false);
                if config_valid && want != current {
                    let _ = if want {
                        handle.autolaunch().enable()
                    } else {
                        handle.autolaunch().disable()
                    };
                }
            });

            Ok(())
        })
        .on_window_event(|window,event| {
            if matches!(event,tauri::WindowEvent::ScaleFactorChanged { .. }) {
                let app = window.app_handle().clone();
                let label = window.label().to_string();
                // Windows applies its suggested DPI rectangle after dispatching the event.
                std::thread::spawn(move || {
                    std::thread::sleep(std::time::Duration::from_millis(120));
                    if let Some(window) = app.get_webview_window(&label) {
                        let mode = services::config_service::ConfigService::load().view_mode.unwrap_or_else(|| "main".into());
                        if let Err(error) = fit_window_to_monitor(&window,&mode) { eprintln!("[LifeRPG] DPI 窗口适配失败: {}",error); }
                    }
                });
            }
        })
        .invoke_handler(tauri::generate_handler![
            rpg_get_dashboard,
            rpg_get_character,
            rpg_update_character,
            rpg_set_avatar,
            rpg_set_avatar_fit,
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
            config_recover,
            config_save,
            get_autostart,
            set_autostart,
            set_hotkey,
            app_exit,
            window_set_view_mode,
            window_toggle_mini,
            window_toggle_pin,
            record_list,
            record_get,
            record_create,
            record_update,
            record_delete,
            record_categories,
            image_to_data_url,
            image_gallery,
            image_cleanup_scan,image_cleanup_quarantine,image_cleanup_batches,image_cleanup_restore,image_cleanup_purge,
            notification_status,notification_test,notification_receipt,notification_check,
            archive_stats,
            backup_create,
            backup_list,
            backup_restore,
            export_markdown,
            app_data_dir,
            app_state,
            data_dir_init,
            data_dir_migrate,
            data_dir_check,
            equip_list,
            equip_snapshot,
            equip_conflicts,
            equip_repair,
            equip_save,
            equip_toggle,
            equip_delete,
            skill_list,
            skill_save,
            skill_unlock,
            skill_relock,
            skill_add_exp,
            skill_delete,
            npc_list,
            npc_save,
            npc_adjust_relation,
            npc_delete,
            map_list,
            map_save,
            map_toggle,
            map_delete,
            ai_config_get,
            ai_test,
            ai_review,
            ai_parse_task,
            ai_name_equipment,
            ai_coach,
            mcp_get_servers,
            mcp_save_server,
            mcp_delete_server,
            mcp_test_server,
            mcp_call_tool,
        ])
        .run(tauri::generate_context!())
        .expect("运行 LifeRPG HUD 时发生错误");
}
