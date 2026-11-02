use crate::db::Db;
use crate::models::{
    ActionTask, ArchiveStats, AttributeLog, BackupInfo, Character, CharacterAttribute,
    CreateRecordInput, CreateTaskInput, DashboardSnapshot, EquipmentItem, EquipmentSnapshot,
    LifeRecord, MapLocationItem, NpcItem, RpgExperienceResult, SaveEquipmentInput, SaveMapInput,
    SaveNpcInput, SaveSkillInput, SkillItem, UpdateRecordInput, UpdateTaskInput,
};
use crate::services::action_task_service::ActionTaskService;
use crate::services::backup_service::BackupService;
use crate::services::character_service::CharacterService;
use crate::services::config_service::{ConfigService, WidgetSettings};
use crate::services::equipment_service::EquipmentService;
use crate::services::export_service::ExportService;
use crate::services::image_service::ImageService;
use crate::services::record_service::RecordService;
use crate::services::skill_service::SkillService;
use crate::services::stats_service::StatsService;
use crate::services::world_service::WorldService;
use tauri::{AppHandle, Manager, WebviewWindow, LogicalSize, Size};
use tauri_plugin_autostart::ManagerExt;
use tauri_plugin_global_shortcut::{GlobalShortcutExt, Shortcut};

async fn run_blocking<T: Send + 'static>(work: impl FnOnce() -> Result<T,String> + Send + 'static) -> Result<T,String> {
    tauri::async_runtime::spawn_blocking(work).await.map_err(|error|format!("后台任务异常: {}",error))?
}


#[tauri::command]
pub fn rpg_get_dashboard() -> Result<DashboardSnapshot, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    CharacterService::get_dashboard_snapshot(&conn).map_err(|e| format!("获取仪表盘失败: {}", e))
}

#[tauri::command]
pub fn rpg_get_character() -> Result<Character, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    CharacterService::get_default_character(&conn).map_err(|e| format!("获取角色失败: {}", e))
}

#[tauri::command]
pub fn rpg_update_character(name: String, description: String) -> Result<Character, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    CharacterService::update_character(&conn, &name, &description)
        .map_err(|e| format!("更新角色失败: {}", e))
}

#[tauri::command]
pub async fn rpg_set_avatar(path: Option<String>) -> Result<Character, String> {
    run_blocking(move || {
        let _operation = crate::operations::read()?;
        let mut conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
        let tx = conn.transaction().map_err(|e| e.to_string())?;
        let character = CharacterService::get_default_character(&tx).map_err(|e| e.to_string())?;
        let image = path.as_deref().map(ImageService::import_avatar).transpose()?.map(|(_, file)| file);
        tx.execute("UPDATE Characters SET AvatarImagePath=?1,UpdatedAt=?2 WHERE Id=?3",rusqlite::params![image,chrono::Local::now().to_rfc3339(),character.id]).map_err(|e|e.to_string())?;
        tx.commit().map_err(|e|e.to_string())?;
        CharacterService::get_default_character(&conn).map_err(|e|e.to_string())
    }).await
}

#[tauri::command]
pub fn rpg_get_attributes() -> Result<Vec<CharacterAttribute>, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    let character = CharacterService::get_default_character(&conn).map_err(|e| format!("获取角色失败: {}", e))?;
    CharacterService::get_attributes(&conn, &character.id).map_err(|e| format!("获取属性失败: {}", e))
}

#[tauri::command]
pub fn rpg_get_logs(limit: Option<usize>) -> Result<Vec<AttributeLog>, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    CharacterService::get_recent_logs(&conn, limit.unwrap_or(50)).map_err(|e| format!("获取日志失败: {}", e))
}

#[tauri::command]
pub fn task_list(status: Option<String>, category: Option<String>) -> Result<Vec<ActionTask>, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    ActionTaskService::list_tasks(&conn, status.as_deref(), category.as_deref())
        .map_err(|e| format!("查询任务列表失败: {}", e))
}

#[tauri::command]
pub fn task_get_categories() -> Result<Vec<String>, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    ActionTaskService::get_categories(&conn).map_err(|e| format!("查询分类列表失败: {}", e))
}

#[tauri::command]
pub fn task_create(input: CreateTaskInput) -> Result<ActionTask, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    ActionTaskService::create_task(&conn, input).map_err(|e| format!("创建任务失败: {}", e))
}

#[tauri::command]
pub fn task_update(input: UpdateTaskInput) -> Result<bool, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    ActionTaskService::update_task(&conn, input).map_err(|e| format!("更新任务失败: {}", e))
}

#[tauri::command]
pub fn task_delete(id: String) -> Result<bool, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    ActionTaskService::delete_task(&conn, &id).map_err(|e| format!("删除任务失败: {}", e))
}

#[tauri::command]
pub fn task_complete(id: String) -> Result<RpgExperienceResult, String> {
    let _operation=crate::operations::read()?;
    let mut conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    CharacterService::apply_task_completion(&mut conn, &id).map_err(|e| format!("完成任务结算失败: {}", e))
}

#[tauri::command]
pub fn task_uncomplete(id: String) -> Result<bool, String> {
    let _operation=crate::operations::read()?;
    let mut conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    CharacterService::rollback_task_completion(&mut conn, &id).map_err(|e| format!("回滚任务状态失败: {}", e))
}

#[tauri::command]
pub fn config_get() -> Result<WidgetSettings, String> {
    ConfigService::load_checked()
}

#[tauri::command]
pub fn config_save(settings: serde_json::Value) -> Result<(), String> {
    let _operation=crate::operations::write()?;
    ConfigService::save_merged(&settings)?;
    Ok(())
}

#[tauri::command]
pub fn get_autostart(app: AppHandle) -> Result<bool, String> {
    app.autolaunch().is_enabled().map_err(|e| e.to_string())
}

#[tauri::command]
pub fn set_autostart(app: AppHandle, enabled: bool) -> Result<(), String> {
    ConfigService::load_checked()?;
    let _operation=crate::operations::read()?;
    let autolaunch = app.autolaunch();
    if enabled {
        autolaunch.enable().map_err(|e| e.to_string())?;
    } else {
        autolaunch.disable().map_err(|e| e.to_string())?;
    }
    // 同步写回配置，下次启动以配置为准再校准一次
    ConfigService::update(|cfg|{cfg.autostart=Some(enabled);Ok(())})?;
    Ok(())
}

#[tauri::command]
pub fn set_hotkey(app: AppHandle, key: String) -> Result<(), String> {
    ConfigService::load_checked()?;
    let _operation=crate::operations::read()?;
    let shortcut: Shortcut = key.parse().map_err(|_| "无效快捷键格式".to_string())?;
    let manager = app.global_shortcut();
    if manager.is_registered(shortcut) { return Ok(()); }

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

    let cfg = ConfigService::load_checked()?;
    if let Some(old) = cfg.hotkey.as_ref().filter(|old| *old != &key) {
        if let Ok(old_shortcut) = old.parse::<Shortcut>() { let _ = manager.unregister(old_shortcut); }
    }
    ConfigService::update(|cfg|{cfg.hotkey=Some(key);Ok(())})?;

    Ok(())
}

/// 优雅退出应用
#[tauri::command]
pub fn app_exit(app: AppHandle) -> Result<(), String> {
    app.exit(0);
    Ok(())
}

/// Keep the complete native frame inside the current monitor work area after a DPI change.
pub fn fit_window_to_monitor(window: &WebviewWindow, mode: &str) -> Result<(), String> {
    if window.is_maximized().map_err(|e| e.to_string())? { return Ok(()); }
    let Some(monitor) = window.current_monitor().map_err(|e| e.to_string())? else { return Ok(()); };
    let area = monitor.work_area();
    let scale = monitor.scale_factor();
    let margin = (8.0 * scale).round() as u32;
    let inner = window.inner_size().map_err(|e| e.to_string())?;
    let outer = window.outer_size().map_err(|e| e.to_string())?;
    let max_w = area.size.width.saturating_sub(outer.width.saturating_sub(inner.width) + margin * 2).max(1);
    let max_h = area.size.height.saturating_sub(outer.height.saturating_sub(inner.height) + margin * 2).max(1);
    let (min_w,min_h): (f64,f64) = match mode { "widget" => (280.0,300.0), "mini" => (200.0,36.0), _ => (760.0,560.0) };
    window.set_min_size(Some(Size::Logical(LogicalSize::new(min_w.min(max_w as f64/scale),min_h.min(max_h as f64/scale))))).map_err(|e| e.to_string())?;
    if inner.width > max_w || inner.height > max_h {
        window.set_size(Size::Physical(tauri::PhysicalSize::new(inner.width.min(max_w),inner.height.min(max_h)))).map_err(|e| e.to_string())?;
    }
    let outer = window.outer_size().map_err(|e| e.to_string())?;
    let position = window.outer_position().map_err(|e| e.to_string())?;
    let left = area.position.x + margin as i32;
    let top = area.position.y + margin as i32;
    let right = (area.position.x + area.size.width as i32 - margin as i32 - outer.width as i32).max(left);
    let bottom = (area.position.y + area.size.height as i32 - margin as i32 - outer.height as i32).max(top);
    window.set_position(tauri::PhysicalPosition::new(position.x.clamp(left,right),position.y.clamp(top,bottom))).map_err(|e| e.to_string())?;
    Ok(())
}

/// 窗口形态多档切换（大屏指挥中心 main 1460x920 / 桌面挂件 widget 340x600 / 胶囊护符 mini 260x44）
#[tauri::command]
pub fn window_set_view_mode(window: WebviewWindow, mode: String) -> Result<(), String> {
    let _operation=crate::operations::read()?;
    if !["main","widget","mini"].contains(&mode.as_str()) { return Err("无效窗口形态".into()); }
    window.unmaximize().map_err(|e| e.to_string())?;
    let monitor = window.current_monitor().map_err(|e| e.to_string())?;
    let (available_w, available_h) = monitor.map(|m| ((m.work_area().size.width as f64 / m.scale_factor() - 24.0).max(320.0), (m.work_area().size.height as f64 / m.scale_factor() - 24.0).max(300.0))).unwrap_or((1460.0,920.0));
    // 始终保持任务栏图标可见与 Alt+Tab 可见
    let _ = window.set_skip_taskbar(false);
    match mode.as_str() {
        "main" => {
            let _ = window.set_min_size(Some(Size::Logical(LogicalSize::new(760.0_f64.min(available_w), 560.0_f64.min(available_h)))));
            let _ = window.set_size(Size::Logical(LogicalSize::new(1460.0_f64.min(available_w), 920.0_f64.min(available_h))));
            let _ = window.set_resizable(true);
            let _ = window.center();
        }
        "widget" => {
            let _ = window.set_min_size(Some(Size::Logical(LogicalSize::new(280.0, 300.0))));
            let _ = window.set_size(Size::Logical(LogicalSize::new(340.0_f64.min(available_w), 600.0_f64.min(available_h))));
            let _ = window.set_resizable(true);
        }
        "mini" => {
            let _ = window.set_min_size(Some(Size::Logical(LogicalSize::new(200.0, 36.0))));
            let _ = window.set_size(Size::Logical(LogicalSize::new(260.0, 44.0)));
            let _ = window.set_resizable(false);
        }
        _ => {}
    }

    fit_window_to_monitor(&window, &mode)?;

    // 同步持久化配置，并通知前端切换视图状态（例如从系统托盘发起切换时）
    ConfigService::update(|cfg|{cfg.view_mode=Some(mode.clone());Ok(())})?;
    let _ = window.eval(&format!(
        "window.__onHostViewMode && window.__onHostViewMode({:?})",
        mode
    ));

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

/// 真实置顶切换（通过 Tauri API + Win32 HWND_TOPMOST 双保险 + 便携配置持久化）
#[tauri::command]
pub fn window_toggle_pin(window: WebviewWindow, pin: bool) -> Result<(), String> {
    ConfigService::load_checked()?;
    let _operation=crate::operations::read()?;
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

    ConfigService::update(|cfg|{cfg.is_always_on_top=Some(pin);Ok(())})?;

    Ok(())
}

// ============================ 行为记录 ============================

#[tauri::command]
pub fn record_list(
    keyword: Option<String>,
    category: Option<String>,
    limit: Option<i64>,
    offset: Option<i64>,
) -> Result<Vec<LifeRecord>, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    RecordService::list_page(&conn, keyword, category, limit, offset.unwrap_or(0)).map_err(|e| format!("查询记录失败: {}", e))
}

#[tauri::command]
pub fn record_get(id: String) -> Result<LifeRecord, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    RecordService::get(&conn, &id).map_err(|e| format!("读取记录失败: {}", e))
}

#[tauri::command]
pub async fn record_create(input: CreateRecordInput) -> Result<LifeRecord, String> {
    run_blocking(move || {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    RecordService::create(&conn, input)

    }).await
}

#[tauri::command]
pub async fn record_update(input: UpdateRecordInput) -> Result<LifeRecord, String> {
    run_blocking(move || {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    RecordService::update(&conn, input)

    }).await
}

#[tauri::command]
pub fn record_delete(id: String) -> Result<bool, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    RecordService::delete(&conn, &id)
}

#[tauri::command]
pub fn record_categories() -> Result<Vec<String>, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    let mut stmt = conn
        .prepare("SELECT DISTINCT Category FROM LifeRecords ORDER BY Category")
        .map_err(|e| e.to_string())?;
    let rows = stmt
        .query_map([], |r| r.get::<_, String>(0))
        .map_err(|e| e.to_string())?;
    let mut out: Vec<String> = rows.flatten().collect();
    for d in ["日常", "工作", "学习", "健康", "财务"] {
        if !out.iter().any(|c| c == d) {
            out.push(d.to_string());
        }
    }
    Ok(out)
}

#[tauri::command]
pub fn image_to_data_url(path: String, thumbnail: Option<bool>) -> Result<String, String> {
    let _operation=crate::operations::read()?;
    if thumbnail.unwrap_or(false) { ImageService::thumbnail(&path) } else { ImageService::to_data_url(&path) }
}

// ============================ 统计 / 备份 / 导出 ============================

#[tauri::command]
pub fn archive_stats(days: Option<i64>) -> Result<ArchiveStats, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    StatsService::archive_stats(&conn, days.unwrap_or(30)).map_err(|e| format!("统计失败: {}", e))
}

#[tauri::command]
pub async fn backup_create(dest_dir: Option<String>) -> Result<BackupInfo, String> {
    run_blocking(move || {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    BackupService::create(&conn, dest_dir)

    }).await
}

#[tauri::command]
pub fn backup_list(dest_dir: Option<String>) -> Result<Vec<BackupInfo>, String> {
    Ok(BackupService::list(dest_dir))
}

#[tauri::command]
pub async fn backup_restore(path: String) -> Result<crate::services::backup_service::RestoreReport, String> {
    run_blocking(move || {
    BackupService::restore(&path)

    }).await
}

#[tauri::command]
pub fn export_markdown(dest_dir: String) -> Result<String, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open().map_err(|e| format!("打开数据库失败: {}", e))?;
    ExportService::export_markdown(&conn, &dest_dir)
}

#[tauri::command]
pub fn app_data_dir() -> Result<String, String> {
    let _operation=crate::operations::read()?;
    crate::db::Db::data_root().map(|p| p.to_string_lossy().to_string())
}

// ============================ 首次启动 / 数据目录 ============================

#[derive(serde::Serialize)]
#[serde(rename_all = "camelCase")]
pub struct AppState {
    /// 已配置的数据目录；None 表示需要走首次引导
    pub data_dir: Option<String>,
    pub exe_dir: String,
    pub config_path: String,
    /// WebView2 缓存目录（用于在设置页展示，证明没落 C 盘）
    pub runtime_dir: String,
    pub is_always_on_top: bool,
    pub config_error: Option<String>,
}

#[tauri::command]
pub fn app_state() -> AppState {
    let cfg = ConfigService::load();
    AppState {
        config_error: ConfigService::load_checked().err(),
        data_dir: cfg.data_dir.clone(),
        exe_dir: crate::paths::exe_dir().to_string_lossy().to_string(),
        config_path: ConfigService::get_config_path().to_string_lossy().to_string(),
        runtime_dir: crate::paths::webview_data_dir().to_string_lossy().to_string(),
        is_always_on_top: cfg.is_always_on_top.unwrap_or(false),
    }
}

/// 数据目录预检结果：让前端在用户点「开始使用」之前就能给出准确判断
#[derive(serde::Serialize)]
#[serde(rename_all = "camelCase")]
pub struct DataDirReport {
    pub ok: bool,
    /// 原始 Windows 错误码（成功时为 None）
    pub code: Option<i32>,
    pub message: String,
    /// 推荐的可写目录（程序目录下的 LifeRPG-Data）
    pub suggested_path: String,
}

/// 预检：不写标准子目录，只验证「能不能在这个位置建目录并写文件」
#[tauri::command]
pub fn data_dir_check(path: String) -> DataDirReport {
    let suggested = crate::paths::suggested_data_dir().to_string_lossy().to_string();

    let root = match crate::paths::normalize_data_dir(&path) {
        Ok(p) => p,
        Err(msg) => {
            return DataDirReport {
                ok: false,
                code: None,
                message: msg,
                suggested_path: suggested,
            }
        }
    };

    match crate::paths::probe_writable(&root) {
        Ok(()) => DataDirReport {
            ok: true,
            code: None,
            message: format!("目录可用：{}", root.display()),
            suggested_path: suggested,
        },
        Err(msg) => DataDirReport {
            ok: false,
            code: Some(5),
            message: msg,
            suggested_path: suggested,
        },
    }
}

/// 首次启动：选定数据目录并初始化数据库
#[tauri::command]
pub fn data_dir_init(path: String) -> Result<AppState, String> {
    let _operation=crate::operations::write()?;
    let root = crate::paths::normalize_data_dir(&path)?;
    crate::paths::prepare_data_dir(&root)?;
    let _candidate = Db::open_at(&root)?;
    ConfigService::set_data_root(&path)?;
    Ok(app_state())
}

/// 迁移数据目录：把 data / images / backups 整体复制到新位置后切换指针
#[tauri::command]
pub async fn data_dir_migrate(path: String) -> Result<AppState, String> {
    run_blocking(move || {
    let _operation=crate::operations::write()?;
    let old_root = Db::data_root()?;
    let new_root = crate::paths::normalize_data_dir(&path)?;
    crate::paths::prepare_data_dir(&new_root)?;
    let old = old_root.canonicalize().map_err(|e| e.to_string())?;
    let new = new_root.canonicalize().map_err(|e| e.to_string())?;
    if old == new { return Ok(app_state()); }
    if new.starts_with(&old) || old.starts_with(&new) { return Err("新旧数据目录不能互相包含".into()); }
    let target = new.join("data").join("liferpg.db");
    if target.exists() { return Err("目标目录已有数据库，请选择空的数据目录".into()); }
    let source = Db::open()?;
    source.backup(rusqlite::DatabaseName::Main, &target, None).map_err(|e| e.to_string())?;
    for sub in ["images", "backups", ".image-cleanup"] { if old.join(sub).exists() { copy_dir(&old.join(sub), &new.join(sub))?; } }
    let check = rusqlite::Connection::open(&target).map_err(|e| e.to_string())?;
    Db::ensure_schema(&check).map_err(|e| e.to_string())?;
    ConfigService::set_data_root(&new.to_string_lossy())?;
    Ok(app_state())

    }).await
}

fn copy_dir(from: &std::path::Path, to: &std::path::Path) -> Result<(), String> {
    std::fs::create_dir_all(to).map_err(|e| format!("创建目录失败: {}", e))?;
    let entries = std::fs::read_dir(from).map_err(|e| format!("读取目录失败: {}", e))?;
    for entry in entries {
        let entry=entry.map_err(|error|format!("读取迁移文件失败: {}",error))?;
        let src = entry.path();
        let dst = to.join(entry.file_name());
        if src.is_dir() {
            copy_dir(&src, &dst)?;
        } else {
            std::fs::copy(&src, &dst).map_err(|e| {
                format!("复制 {} 失败: {}", src.display(), e)
            })?;
        }
    }
    Ok(())
}

// ============================ 装备 ============================

#[tauri::command]
pub fn equip_list() -> Result<Vec<EquipmentItem>, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    EquipmentService::list(&conn).map_err(|e| format!("读取装备失败: {}", e))
}

#[tauri::command]
pub fn equip_snapshot() -> Result<EquipmentSnapshot, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    EquipmentService::snapshot(&conn).map_err(|e| format!("读取装备槽失败: {}", e))
}

#[tauri::command]
pub fn equip_save(input: SaveEquipmentInput) -> Result<String, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    EquipmentService::save(&conn, input)
}

#[tauri::command]
pub fn equip_toggle(id: String) -> Result<bool, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    EquipmentService::toggle(&conn, &id)
}

#[tauri::command]
pub fn equip_delete(id: String) -> Result<bool, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    EquipmentService::delete(&conn, &id)
}

// ============================ 技能 ============================

#[tauri::command]
pub fn skill_list() -> Result<Vec<SkillItem>, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    SkillService::list(&conn).map_err(|e| format!("读取技能失败: {}", e))
}

#[tauri::command]
pub fn skill_save(input: SaveSkillInput) -> Result<String, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    SkillService::save(&conn, input)
}

#[tauri::command]
pub fn skill_unlock(id: String) -> Result<(), String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    SkillService::unlock(&conn, &id)
}

#[tauri::command]
pub fn skill_relock(id: String) -> Result<(), String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    SkillService::relock(&conn, &id)
}

#[tauri::command]
pub fn skill_add_exp(id: String, amount: i32) -> Result<SkillItem, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    SkillService::add_experience(&conn, &id, amount)
}

#[tauri::command]
pub fn skill_delete(id: String) -> Result<bool, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    SkillService::delete(&conn, &id)
}

// ============================ 世界：NPC / 地图 ============================

#[tauri::command]
pub fn npc_list() -> Result<Vec<NpcItem>, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    WorldService::npc_list(&conn).map_err(|e| format!("读取人物失败: {}", e))
}

#[tauri::command]
pub fn npc_save(input: SaveNpcInput) -> Result<String, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    WorldService::npc_save(&conn, input)
}

#[tauri::command]
pub fn npc_adjust_relation(id: String, delta: i32) -> Result<i32, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    WorldService::npc_adjust_relationship(&conn, &id, delta)
}

#[tauri::command]
pub fn npc_delete(id: String) -> Result<bool, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    WorldService::npc_delete(&conn, &id)
}

#[tauri::command]
pub fn map_list() -> Result<Vec<MapLocationItem>, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    WorldService::map_list(&conn).map_err(|e| format!("读取地图失败: {}", e))
}

#[tauri::command]
pub fn map_save(input: SaveMapInput) -> Result<String, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    WorldService::map_save(&conn, input)
}

#[tauri::command]
pub fn map_toggle(id: String) -> Result<bool, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    WorldService::map_toggle(&conn, &id)
}

#[tauri::command]
pub fn map_delete(id: String) -> Result<bool, String> {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    WorldService::map_delete(&conn, &id)
}

// ============================ AI 军师 ============================

use crate::services::ai_service::{self, AiConfig, ParsedTask};
use serde_json::{json, Value};

/// AI 设置读取（前端展示用）
#[tauri::command]
pub fn ai_config_get() -> AiConfig {
    AiConfig::load()
}

/// 连接测试：走一遍完整调用链
#[tauri::command]
pub async fn ai_test() -> Result<String, String> {
    run_blocking(move || {
    let cfg = AiConfig::load();
    let model = cfg.model_light.clone().unwrap_or_else(|| cfg.model.clone());
    ai_service::AiService::chat(&cfg, &model, "你是连通性测试。", "回复两个字：在线")
        .map(|r| r.content)

    }).await
}

/// 1) AI 复盘：聚合任务/记录/属性数据后交给军师
#[tauri::command]
pub async fn ai_review(days: Option<i64>) -> Result<String, String> {
    run_blocking(move || {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    let n = days.unwrap_or(7).clamp(1,365);
    let stats = StatsService::archive_stats(&conn, n).map_err(|e| format!("统计失败: {}", e))?;
    let mut records_stmt=conn.prepare("SELECT Title,Category,Experience,Content FROM LifeRecords WHERE julianday(CreatedAt)>=julianday('now',?1) ORDER BY julianday(CreatedAt) DESC LIMIT 10").map_err(|e|e.to_string())?;
    let records=records_stmt.query_map([format!("-{} days",n)],|r|Ok((r.get::<_,String>(0)?,r.get::<_,String>(1)?,r.get::<_,i32>(2)?,r.get::<_,String>(3)?))).map_err(|e|e.to_string())?.collect::<rusqlite::Result<Vec<_>>>().map_err(|e|e.to_string())?;

    let record_titles: Vec<Value> = records
        .iter()
        .map(|r| json!({ "title": r.0, "category": r.1, "exp": r.2, "content": r.3.chars().take(2000).collect::<String>() }))
        .collect();

    let completed_titles: Vec<Value> = {
        let mut stmt = conn
            .prepare("SELECT Title, Quadrant, Experience FROM ActionTasks WHERE Status='Completed' AND julianday(CompletedAt)>=julianday('now',?1) ORDER BY julianday(CompletedAt) DESC LIMIT 15")
            .map_err(|e| e.to_string())?;
        let rows = stmt
            .query_map([format!("-{} days",n)], |row| {
                Ok(json!({ "title": row.get::<_, String>(0)?, "quadrant": row.get::<_, String>(1)?, "exp": row.get::<_, i32>(2)? }))
            })
            .map_err(|e| e.to_string())?;
        rows.flatten().collect()
    };

    let attrs_for_review: Vec<Value> = {
        let mut stmt = conn
            .prepare("SELECT DisplayName, Level, Experience FROM CharacterAttributes ORDER BY AttributeKey")
            .map_err(|e| e.to_string())?;
        let rows = stmt
            .query_map([], |row| {
                Ok(json!({ "name": row.get::<_, String>(0)?, "level": row.get::<_, i32>(1)?, "exp": row.get::<_, i32>(2)? }))
            })
            .map_err(|e| e.to_string())?;
        rows.flatten().collect()
    };

    let summary = json!({
        "周期天数": n,
        "累计任务完成数": stats.completed_tasks,
        "累计待办数": stats.pending_tasks,
        "累计逾期数": stats.overdue_tasks,
        "总经验": stats.total_experience,
        "连续活跃天数": stats.streak_days,
        "六维属性": attrs_for_review,
        "分类分布": stats.category_breakdown,
        "最近完成任务": completed_titles,
        "最近行为记录": record_titles,
    });
    drop(_operation);
    ai_service::review(&AiConfig::load(), &summary)

    }).await
}

/// 2) 自然语言 → 结构化任务
#[tauri::command]
pub async fn ai_parse_task(text: String) -> Result<ParsedTask, String> {
    run_blocking(move || {
    let today = chrono::Local::now().to_rfc3339();
    ai_service::parse_task(&AiConfig::load(), &text, &today)

    }).await
}

/// 3) 现实物品 → 游戏装备命名
#[tauri::command]
pub async fn ai_name_equipment(real_name: String, real_effect: String) -> Result<Value, String> {
    run_blocking(move || {
    ai_service::name_equipment(&AiConfig::load(), &real_name, &real_effect)

    }).await
}

/// 4) 属性教练：六维快照 → 一条建议
#[tauri::command]
pub async fn ai_coach() -> Result<String, String> {
    run_blocking(move || {
    let _operation=crate::operations::read()?;
    let conn = Db::open()?;
    let snap = CharacterService::get_dashboard_snapshot(&conn).map_err(|e| e.to_string())?;
    let attrs: Vec<Value> = snap
        .attributes
        .iter()
        .map(|a| json!({ "key": a.attribute_key, "name": a.display_name, "level": a.level, "exp": a.experience }))
        .collect();
    let payload = json!({
        "等级": snap.level_info.level,
        "总经验": snap.level_info.total_experience,
        "今日完成": snap.today_completed_count,
        "今日总数": snap.today_total_count,
        "六维": attrs,
    });
    drop(_operation);
    ai_service::coach(&AiConfig::load(), &payload)

    }).await
}

// ============================ 外接神域 (MCP) 契约 ============================

/// 获取所有保存的 MCP 伺服配置
#[tauri::command]
pub fn mcp_get_servers() -> Result<Vec<crate::models::McpServerConfig>, String> {
    let cfg = ConfigService::load();
    Ok(cfg.mcp_servers.unwrap_or_default())
}

/// 保存或更新单个 MCP 伺服配置
#[tauri::command]
pub fn mcp_save_server(server: crate::models::McpServerConfig) -> Result<Vec<crate::models::McpServerConfig>, String> {
    let _operation=crate::operations::read()?;
    let cfg=ConfigService::update(|cfg|{
        let list=cfg.mcp_servers.get_or_insert_with(Vec::new);
        if let Some(pos)=list.iter().position(|s|s.id==server.id){list[pos]=server;}else{list.push(server);}
        Ok(())
    })?;
    Ok(cfg.mcp_servers.unwrap_or_default())
}

/// 删除指定的 MCP 伺服配置
#[tauri::command]
pub fn mcp_delete_server(id: String) -> Result<Vec<crate::models::McpServerConfig>, String> {
    let _operation=crate::operations::read()?;
    let cfg=ConfigService::update(|cfg|{if let Some(list)=&mut cfg.mcp_servers{list.retain(|s|s.id!=id);}Ok(())})?;
    Ok(cfg.mcp_servers.unwrap_or_default())
}

/// 测试指定 MCP 的握手并探测可用工具清单
#[tauri::command]
pub async fn mcp_test_server(server: crate::models::McpServerConfig) -> Result<Vec<crate::models::McpToolDefinition>, String> {
    run_blocking(move || {
    crate::services::mcp_service::McpService::list_tools_for_server(&server)

    }).await
}

/// 执行指定的 MCP 工具调用
#[tauri::command]
pub async fn mcp_call_tool(
    server_id: String,
    tool_name: String,
    arguments: serde_json::Value,
) -> Result<crate::models::McpToolCallResult, String> {
    run_blocking(move || {
    let cfg = ConfigService::load();
    let list = cfg.mcp_servers.unwrap_or_default();
    let s = list.iter().find(|x| x.id == server_id).ok_or_else(|| format!("未找到 ID 为 [{}] 的 MCP 服务", server_id))?;
    crate::services::mcp_service::McpService::call_tool(s, &tool_name, &arguments)

    }).await
}


// ============================ 图片维护与系统通知 ============================
#[tauri::command]
pub async fn image_cleanup_scan() -> Result<crate::services::image_cleanup_service::Scan,String> {
    run_blocking(move || {
    let _operation=crate::operations::read()?;
    let root=Db::data_root()?;let conn=Db::open()?;
    crate::services::image_cleanup_service::ImageCleanup::scan(&conn,&root)

    }).await
}
#[tauri::command]
pub async fn image_cleanup_quarantine(root:String,files:Vec<crate::services::image_cleanup_service::Candidate>)->Result<crate::services::image_cleanup_service::Batch,String>{
    run_blocking(move || {
    let _operation=crate::operations::write()?;
    let current=Db::data_root()?;let conn=Db::open()?;
    crate::services::image_cleanup_service::ImageCleanup::quarantine(&conn,&current,&root,&files)

    }).await
}
#[tauri::command]
pub async fn image_cleanup_batches()->Result<Vec<crate::services::image_cleanup_service::Batch>,String>{
    run_blocking(move || {
    let _operation=crate::operations::read()?;
    crate::services::image_cleanup_service::ImageCleanup::batches(&Db::data_root()?)

    }).await
}
#[tauri::command]
pub async fn image_cleanup_restore(id:String)->Result<usize,String>{
    run_blocking(move || {
    let _operation=crate::operations::write()?;
    crate::services::image_cleanup_service::ImageCleanup::restore(&Db::data_root()?,&id)

    }).await
}
#[tauri::command]
pub async fn image_cleanup_purge(id:String,confirmation:String)->Result<usize,String>{
    run_blocking(move || {
    let _operation=crate::operations::write()?;
    crate::services::image_cleanup_service::ImageCleanup::purge(&Db::open()?,&Db::data_root()?,&id,&confirmation)

    }).await
}
#[tauri::command]
pub async fn notification_status()->Result<crate::services::notification_service::NotificationStatus,String>{
    run_blocking(move || {
    let _operation=crate::operations::read()?;
    crate::services::notification_service::NotificationService::status(&Db::open()?,ConfigService::load().notification_enabled==Some(true))

    }).await
}
#[tauri::command]
pub async fn notification_test()->Result<serde_json::Value,String>{
    run_blocking(move || {
    let tag=uuid::Uuid::new_v4().to_string();
    crate::services::notification_service::NotificationService::send(&tag,"LifeRPG · 通知测试","这是一条测试通知。到期提醒将在应用运行时检查。")?;
    Ok(json!({"tag":tag,"submitted":true}))

    }).await
}
#[tauri::command]
pub async fn notification_receipt(tag:String)->Result<bool,String>{
    run_blocking(move || {crate::services::notification_service::NotificationService::receipt(&tag)
    }).await
}
#[tauri::command]
pub async fn notification_check()->Result<crate::services::notification_service::Dispatch,String>{
    run_blocking(move || {
    let _operation=crate::operations::read()?;
    if ConfigService::load().notification_enabled!=Some(true){return Ok(Default::default());}
    crate::services::notification_service::NotificationService::dispatch(&Db::open()?,|id,title|crate::services::notification_service::NotificationService::send(id,"LifeRPG · 任务到期",title))

    }).await
}

#[tauri::command]
pub fn config_recover(source:Option<String>,data_dir:Option<String>)->Result<WidgetSettings,String>{
    let _operation=crate::operations::write()?;
    ConfigService::recover(source,data_dir)
}

#[tauri::command]
pub fn equip_conflicts()->Result<Vec<serde_json::Value>,String>{let _guard=crate::operations::read()?;EquipmentService::conflicts(&Db::open()?) }
#[tauri::command]
pub async fn equip_repair()->Result<serde_json::Value,String>{run_blocking(||{
    let _guard=crate::operations::write()?;let conn=Db::open()?;
    let backup=BackupService::create(&conn,None)?;
    let removed=EquipmentService::repair(&conn)?;
    Ok(serde_json::json!({"removed":removed,"backup":backup}))
}).await}

#[tauri::command]
pub fn image_gallery(limit:Option<usize>,offset:Option<usize>)->Result<serde_json::Value,String>{
 let _guard=crate::operations::read()?;let conn=Db::open()?;let limit=limit.unwrap_or(50).clamp(1,200);let offset=offset.unwrap_or(0);
 let mut stmt=conn.prepare("SELECT i.Id,i.FilePath,i.FileName,i.CreatedAt,r.Id,r.Title FROM LifeRecordImages i JOIN LifeRecords r ON r.Id=i.LifeRecordId ORDER BY i.CreatedAt DESC,i.Id DESC LIMIT ?1 OFFSET ?2").map_err(|e|e.to_string())?;
 let mut items=stmt.query_map(rusqlite::params![limit+1,offset],|r|Ok(serde_json::json!({"id":r.get::<_,String>(0)?,"filePath":r.get::<_,String>(1)?,"fileName":r.get::<_,String>(2)?,"createdAt":r.get::<_,String>(3)?,"recordId":r.get::<_,String>(4)?,"recordTitle":r.get::<_,String>(5)?}))).map_err(|e|e.to_string())?.collect::<rusqlite::Result<Vec<_>>>().map_err(|e|e.to_string())?;
 let has_more=items.len()>limit;items.truncate(limit);Ok(serde_json::json!({"items":items,"hasMore":has_more}))
}

#[tauri::command]
pub fn rpg_set_avatar_fit(mode:String)->Result<(),String>{
 if mode!="contain"&&mode!="cover"{return Err("无效头像显示模式".into());}
 let _guard=crate::operations::read()?;let conn=Db::open()?;
 let c=CharacterService::get_default_character(&conn).map_err(|e|e.to_string())?;
 conn.execute("UPDATE Characters SET AvatarFit=?1 WHERE Id=?2",rusqlite::params![mode,c.id]).map_err(|e|e.to_string())?;Ok(())
}
