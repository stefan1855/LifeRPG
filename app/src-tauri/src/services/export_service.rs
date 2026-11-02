use rusqlite::Connection;
use std::fs;

/// Markdown 导出服务：把任务与行为记录导出成可读的复盘文档
pub struct ExportService;

impl ExportService {
    pub fn export_markdown(conn: &Connection, dest_dir: &str) -> Result<String, String> {
        let dir = std::path::Path::new(dest_dir);
        fs::create_dir_all(dir).map_err(|e| format!("无法创建导出目录: {}", e))?;

        let stamp = chrono::Local::now().format("%Y%m%d_%H%M%S").to_string();
        let file = dir.join(format!("LifeRPG_导出_{}.md", stamp));

        let mut md = String::new();
        md.push_str("# LifeRPG 复盘导出\n\n");
        md.push_str(&format!(
            "> 导出时间：{}\n\n",
            chrono::Local::now().format("%Y-%m-%d %H:%M")
        ));

        // 角色概览
        if let Ok((name, level, total)) = conn.query_row(
            "SELECT Name, Level, TotalExperience FROM Characters LIMIT 1",
            [],
            |r| Ok((r.get::<_, String>(0)?, r.get::<_, i32>(1)?, r.get::<_, i32>(2)?)),
        ) {
            md.push_str("## 角色\n\n");
            md.push_str(&format!("- 名称：{}\n- 等级：Lv.{}\n- 总经验：{}\n\n", name, level, total));
        }

        // 属性
        md.push_str("## 六大属性\n\n");
        md.push_str("| 属性 | 等级 | 经验 |\n|---|---|---|\n");
        if let Ok(mut stmt) = conn.prepare(
            "SELECT DisplayName, Level, Experience FROM CharacterAttributes ORDER BY AttributeKey",
        ) {
            if let Ok(rows) = stmt.query_map([], |r| {
                Ok((
                    r.get::<_, String>(0)?,
                    r.get::<_, i32>(1)?,
                    r.get::<_, i32>(2)?,
                ))
            }) {
                for row in rows.flatten() {
                    md.push_str(&format!("| {} | {} | {} |\n", row.0, row.1, row.2));
                }
            }
        }
        md.push_str("\n");

        // 任务
        md.push_str("## 行动任务\n\n");
        if let Ok(mut stmt) = conn.prepare(
            "SELECT Title, Status, Quadrant, GrowthAttribute, Experience, DueAt, CompletedAt
             FROM ActionTasks ORDER BY CreatedAt DESC",
        ) {
            if let Ok(rows) = stmt.query_map([], |r| {
                Ok((
                    r.get::<_, String>(0)?,
                    r.get::<_, String>(1)?,
                    r.get::<_, String>(2)?,
                    r.get::<_, String>(3)?,
                    r.get::<_, i32>(4)?,
                    r.get::<_, Option<String>>(5)?,
                    r.get::<_, Option<String>>(6)?,
                ))
            }) {
                for (title, status, quadrant, attr, exp, due, done) in rows.flatten() {
                    let mark = if status == "Completed" { "x" } else { " " };
                    md.push_str(&format!(
                        "- [{}] **{}** · {} · {} · +{} EXP{}{}\n",
                        mark,
                        title,
                        quadrant,
                        attr,
                        exp,
                        due.map(|d| format!(" · 截止 {}", &d[..10.min(d.len())]))
                            .unwrap_or_default(),
                        done.map(|d| format!(" · 完成 {}", &d[..10.min(d.len())]))
                            .unwrap_or_default()
                    ));
                }
            }
        }
        md.push_str("\n");

        // 行为记录
        md.push_str("## 行为记录\n\n");
        if let Ok(mut stmt) =
            conn.prepare("SELECT Title, Content, Category, Experience, CreatedAt FROM LifeRecords ORDER BY CreatedAt DESC")
        {
            if let Ok(rows) = stmt.query_map([], |r| {
                Ok((
                    r.get::<_, String>(0)?,
                    r.get::<_, String>(1)?,
                    r.get::<_, String>(2)?,
                    r.get::<_, i32>(3)?,
                    r.get::<_, String>(4)?,
                ))
            }) {
                for (title, content, category, exp, created) in rows.flatten() {
                    md.push_str(&format!(
                        "### {} · {}\n\n- 分类：{}　经验：+{}\n\n{}\n\n",
                        title,
                        &created[..10.min(created.len())],
                        category,
                        exp,
                        content
                    ));
                }
            }
        }

        fs::write(&file, md).map_err(|e| format!("写入导出文件失败: {}", e))?;
        Ok(file.to_string_lossy().to_string())
    }
}
