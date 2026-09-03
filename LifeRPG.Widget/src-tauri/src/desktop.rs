//! Windows 窗口系统集成管理
//! 保证标准应用窗口行为（任务栏图标正常显示、Alt+Tab 列表正常可见）。

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
