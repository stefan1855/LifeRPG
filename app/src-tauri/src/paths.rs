//! 便携式路径解析
//!
//! 设计目标：程序放哪，配置就在哪；数据目录由用户首次启动时指定，绝不默认写 C 盘。
//!
//! 布局：
//! ```text
//! <exe 目录>\
//! ├─ LifeRPG.exe
//! ├─ liferpg.config.json     ← 便携配置（含数据目录指针）
//! └─ .runtime\webview2\      ← WebView2 缓存，避免落 C 盘
//! <数据目录>\
//! ├─ data\liferpg.db
//! ├─ images\
//! └─ backups\
//! ```

use std::path::{Path, PathBuf};

/// 程序所在目录（绿色版根目录）
pub fn exe_dir() -> PathBuf {
    std::env::current_exe()
        .ok()
        .and_then(|p| p.parent().map(|d| d.to_path_buf()))
        .unwrap_or_else(|| PathBuf::from("."))
}

/// 便携配置文件路径：始终跟着 exe 走
pub fn portable_config_path() -> PathBuf {
    exe_dir().join("liferpg.config.json")
}

/// WebView2 用户数据目录（缓存/GPU 着色器都在这），默认放在 exe 同级，保持 C 盘干净
pub fn webview_data_dir() -> PathBuf {
    exe_dir().join(".runtime").join("webview2")
}

/// 把用户选的数据目录标准化（去掉尾部斜杠、展开为绝对路径）
pub fn normalize_data_dir(input: &str) -> Result<PathBuf, String> {
    let trimmed = input.trim().trim_matches('"');
    if trimmed.is_empty() {
        return Err("数据目录不能为空".into());
    }
    let p = PathBuf::from(trimmed);
    if !p.is_absolute() {
        return Err(format!("请选择或输入绝对路径（当前填的是相对路径：{}）", trimmed));
    }
    Ok(p)
}

/// 推荐的可写数据目录：程序目录下的 LifeRPG-Data（程序目录必须可写，否则程序本身跑不起来）
pub fn suggested_data_dir() -> PathBuf {
    exe_dir().join("LifeRPG-Data")
}

/// 把 io::Error 的原始错误码翻译成中文短标签
fn err_kind_cn(code: Option<i32>) -> &'static str {
    match code {
        Some(2) => "系统找不到指定的文件",
        Some(3) => "系统找不到指定的路径",
        Some(5) => "拒绝访问",
        Some(19) => "介质受写保护",
        Some(32) => "文件被其他程序占用",
        Some(112) => "磁盘空间不足",
        Some(123) => "文件名或路径格式不正确",
        _ => "系统调用失败",
    }
}

/// 构造「可操作」的目录错误信息，而不是把裸 os error 甩给用户
pub fn describe_dir_error(e: &std::io::Error, target: &Path) -> String {
    let code = e.raw_os_error();
    let mut msg = format!(
        "目标：{}\n原因：{}（os error {}）",
        target.display(),
        err_kind_cn(code),
        code.map(|c| c.to_string()).unwrap_or_else(|| "未知".into())
    );

    if code == Some(5) {
        msg.push_str(
            "\n\n常见原因与处置：\
             \n1) 程序文件带「低完整性」标记（从受限目录拷贝/解压时常见），进程被系统降权，写不进普通目录 → 先双击便携包里的「解除锁定.bat」，或把整个程序文件夹拷到 D:\\ 根目录下再运行；\
             \n2) 目标文件夹受系统保护或被安全软件拦截 → 换一个普通目录，例如程序目录下的 LifeRPG-Data；\
             \n3) 目标是只读介质或网络盘 → 换成本地磁盘目录。",
        );
        if let Some(hint) = low_integrity_hint() {
            msg.push_str("\n\n自动检测：");
            msg.push_str(&hint);
        }
    } else if code == Some(19) || code == Some(2) || code == Some(3) {
        msg.push_str("\n\n请确认该路径所在磁盘存在、且不是只读介质。");
    }

    msg
}

/// 检测当前 exe 是否带「低完整性」标签（只在出错时调用，避免常规路径开销）
#[cfg(windows)]
fn low_integrity_hint() -> Option<String> {
    use std::os::windows::process::CommandExt;
    const CREATE_NO_WINDOW: u32 = 0x0800_0000;

    let exe = std::env::current_exe().ok()?;
    let out = std::process::Command::new("icacls")
        .arg(&exe)
        .creation_flags(CREATE_NO_WINDOW)
        .output()
        .ok()?;
    let text = String::from_utf8_lossy(&out.stdout);
    if text.contains("Low Mandatory Level") {
        Some(
            "LifeRPG.exe 当前带有「低完整性」标签，进程被系统降权，因此写不进普通目录。\
             处置：双击程序文件夹里的「解除锁定.bat」，或把整个程序文件夹拷到 D:\\ 根目录后重新运行。"
                .to_string(),
        )
    } else {
        None
    }
}

#[cfg(not(windows))]
fn low_integrity_hint() -> Option<String> {
    None
}

/// 在指定数据目录下建立标准子目录，并验证可写
pub fn prepare_data_dir(root: &PathBuf) -> Result<(), String> {
    // 用户可能把某个已存在的「文件」当成了目录
    if root.exists() && !root.is_dir() {
        return Err(format!(
            "所选路径已存在，但它是一个文件而不是文件夹：{}\n请重新选择一个文件夹。",
            root.display()
        ));
    }

    std::fs::create_dir_all(root).map_err(|e| describe_dir_error(&e, root))?;

    for sub in ["data", "images", "backups"] {
        let dir = root.join(sub);
        std::fs::create_dir_all(&dir).map_err(|e| describe_dir_error(&e, &dir))?;
    }
    // 真实写一次，确认不是只读盘
    let probe = root.join("data").join(".write_probe");
    std::fs::write(&probe, b"ok").map_err(|e| describe_dir_error(&e, &probe))?;
    let _ = std::fs::remove_file(&probe);
    Ok(())
}

/// 只做「能否写入」探测，不建标准子目录（供首次启动页预检用）
pub fn probe_writable(root: &Path) -> Result<(), String> {
    if root.exists() && !root.is_dir() {
        return Err(format!("该路径已存在且是文件，不是文件夹：{}", root.display()));
    }
    std::fs::create_dir_all(root).map_err(|e| describe_dir_error(&e, root))?;
    let probe = root.join(".liferpg_write_probe");
    std::fs::write(&probe, b"ok").map_err(|e| describe_dir_error(&e, &probe))?;
    let _ = std::fs::remove_file(&probe);
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_normalize_rejects_relative_and_empty() {
        assert!(normalize_data_dir("").is_err());
        assert!(normalize_data_dir("   ").is_err());
        assert!(normalize_data_dir("relative\\path").is_err());
        assert!(normalize_data_dir("D:\\LifeRPG-Data").is_ok());
    }

    #[test]
    fn test_describe_dir_error_is_actionable() {
        let e = std::io::Error::from_raw_os_error(5);
        let msg = describe_dir_error(&e, Path::new("D:\\F\\liferpg\\data"));
        assert!(msg.contains("拒绝访问"), "应包含中文原因: {}", msg);
        assert!(msg.contains("os error 5"), "应保留原始错误码: {}", msg);
        assert!(msg.contains("解除锁定"), "应给出可操作处置: {}", msg);
    }

    #[test]
    fn test_probe_writable_creates_and_cleans() {
        let dir = std::env::temp_dir().join(format!("liferpg-probe-{}", uuid::Uuid::new_v4()));
        assert!(probe_writable(&dir).is_ok());
        assert!(dir.is_dir());
        assert!(!dir.join(".liferpg_write_probe").exists(), "探针文件应被清理");
        let _ = std::fs::remove_dir_all(&dir);
    }

    #[test]
    fn test_probe_rejects_path_that_is_a_file() {
        let file = std::env::temp_dir().join(format!("liferpg-file-{}.tmp", uuid::Uuid::new_v4()));
        std::fs::write(&file, b"x").unwrap();
        let err = probe_writable(&file).unwrap_err();
        assert!(err.contains("文件"), "应明确提示这是个文件: {}", err);
        let _ = std::fs::remove_file(&file);
    }
}
