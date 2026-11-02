//! Windows 窗口系统集成管理
//! 保证标准应用窗口行为（任务栏图标正常显示、Alt+Tab 列表正常可见）+ 系统托盘常驻。

use tauri::menu::{Menu, MenuItem};
use tauri::tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent};
use tauri::{App, Manager};

/// 托盘：左键唤回主窗口，右键菜单可切形态 / 退出
pub fn setup_tray(app: &App) -> Result<(), Box<dyn std::error::Error>> {
    let show = MenuItem::with_id(app, "show", "显示主窗口", true, None::<&str>)?;
    let widget = MenuItem::with_id(app, "widget", "切到挂件", true, None::<&str>)?;
    let mini = MenuItem::with_id(app, "mini", "切到胶囊", true, None::<&str>)?;
    let quit = MenuItem::with_id(app, "quit", "退出 LifeRPG", true, None::<&str>)?;
    let menu = Menu::with_items(app, &[&show, &widget, &mini, &quit])?;

    let icon = app
        .default_window_icon()
        .cloned()
        .ok_or("no window icon available for tray")?;

    TrayIconBuilder::with_id("main-tray")
        .icon(icon)
        .tooltip("LifeRPG · 绿火毁灭术")
        .menu(&menu)
        .show_menu_on_left_click(false)
        .on_menu_event(|app, event| match event.id.as_ref() {
            "show" => {
                if let Some(w) = app.get_webview_window("main") {
                    let _ = w.show();
                    let _ = w.unminimize();
                    let _ = w.set_focus();
                    let _ = crate::commands::window_set_view_mode(w, "main".to_string());
                }
            }
            "widget" => {
                if let Some(w) = app.get_webview_window("main") {
                    let _ = w.show();
                    let _ = crate::commands::window_set_view_mode(w, "widget".to_string());
                }
            }
            "mini" => {
                if let Some(w) = app.get_webview_window("main") {
                    let _ = w.show();
                    let _ = crate::commands::window_set_view_mode(w, "mini".to_string());
                }
            }
            "quit" => app.exit(0),
            _ => {}
        })
        .on_tray_icon_event(|tray, event| {
            if let TrayIconEvent::Click {
                button: MouseButton::Left,
                button_state: MouseButtonState::Up,
                ..
            } = event
            {
                let app = tray.app_handle();
                if let Some(w) = app.get_webview_window("main") {
                    let visible = w.is_visible().unwrap_or(false);
                    if visible {
                        let _ = w.set_focus();
                    } else {
                        let _ = w.show();
                        let _ = w.set_focus();
                    }
                }
            }
        })
        .build(app)?;

    Ok(())
}

#[cfg(windows)]
pub fn setup_desktop_widget_mode(win: &tauri::WebviewWindow) -> Result<(), String> {
    use windows::Win32::Foundation::HWND;
    use windows::Win32::UI::WindowsAndMessaging::{
        GetWindowLongPtrW, SetWindowLongPtrW, GWL_EXSTYLE, WS_EX_APPWINDOW, WS_EX_TOOLWINDOW,
    };

    let hwnd_raw = win.hwnd().map_err(|e| e.to_string())?;
    let hwnd = HWND(hwnd_raw.0 as _);

    unsafe {
        // 保证标准应用样式（WS_EX_APPWINDOW），确保任务栏图标与 Alt+Tab 切换列表正常存在
        let ex_style = GetWindowLongPtrW(hwnd, GWL_EXSTYLE);
        let new_ex_style = (ex_style | WS_EX_APPWINDOW.0 as isize) & !(WS_EX_TOOLWINDOW.0 as isize);
        SetWindowLongPtrW(hwnd, GWL_EXSTYLE, new_ex_style);
    }

    Ok(())
}

#[cfg(not(windows))]
pub fn setup_desktop_widget_mode(_win: &tauri::WebviewWindow) -> Result<(), String> {
    Ok(())
}
