use crate::models::{ActionTask, CreateTaskInput, UpdateTaskInput};
use crate::rpg_engine::RpgEngine;
use rusqlite::{params, Connection, Result};

pub struct ActionTaskService;

impl ActionTaskService {
    /// 获取任务列表（支持按状态、分类筛选）
    pub fn list_tasks(
        conn: &Connection,
        status: Option<&str>,
        category: Option<&str>,
    ) -> Result<Vec<ActionTask>> {
        let mut query = String::from(
            "SELECT Id, Title, Content, Category, Status, Quadrant, GrowthAttribute, Experience,
                    DueAt, CompletedAt, CompletionNote, LinkedLifeRecordId, CreatedAt, UpdatedAt
             FROM ActionTasks
             WHERE 1=1",
        );

        let mut param_values: Vec<String> = Vec::new();

        if let Some(s) = status {
            let trimmed = s.trim();
            if !trimmed.is_empty() && trimmed != "all" {
                query.push_str(" AND Status = ?");
                param_values.push(trimmed.to_string());
            }
        }

        if let Some(c) = category {
            let trimmed = c.trim();
            if !trimmed.is_empty() && trimmed != "全部" && trimmed != "all" {
                query.push_str(" AND Category = ?");
                param_values.push(trimmed.to_string());
            }
        }

        query.push_str(" ORDER BY CASE Status WHEN 'Pending' THEN 0 ELSE 1 END, CreatedAt DESC;");

        let mut stmt = conn.prepare(&query)?;
        let rows = stmt.query_map(rusqlite::params_from_iter(param_values.iter()), |row| {
            Ok(ActionTask {
                id: row.get(0)?,
                title: row.get(1)?,
                content: row.get(2)?,
                category: row.get(3)?,
                status: row.get(4)?,
                quadrant: row.get(5)?,
                growth_attribute: row.get(6)?,
                experience: row.get(7)?,
                due_at: row.get(8)?,
                completed_at: row.get(9)?,
                completion_note: row.get(10)?,
                linked_life_record_id: row.get(11)?,
                created_at: row.get(12)?,
                updated_at: row.get(13)?,
            })
        })?;

        let mut tasks = Vec::new();
        for r in rows {
            tasks.push(r?);
        }
        Ok(tasks)
    }

    /// 获取所有存在的分类列表
    pub fn get_categories(conn: &Connection) -> Result<Vec<String>> {
        let mut stmt = conn.prepare("SELECT DISTINCT Category FROM ActionTasks WHERE Category != '' ORDER BY Category ASC;")?;
        let rows = stmt.query_map([], |row| row.get(0))?;
        let mut list = Vec::new();
        for r in rows {
            list.push(r?);
        }
        if list.is_empty() {
            list.push("默认".to_string());
            list.push("软件需求".to_string());
            list.push("日常成长".to_string());
        }
        Ok(list)
    }

    /// 创建行动任务
    pub fn create_task(conn: &Connection, input: CreateTaskInput) -> Result<ActionTask> {
        let id = uuid::Uuid::new_v4().to_string();
        let now = chrono::Local::now().to_rfc3339();

        let title = input.title.trim().to_string();
        let content = input.content.unwrap_or_default().trim().to_string();
        let category = input.category.unwrap_or_else(|| "默认".to_string()).trim().to_string();
        let quadrant = input.quadrant.unwrap_or_else(|| "Q2".to_string()).trim().to_string();

        let (growth_attr, _) = RpgEngine::resolve_attribute(input.growth_attribute.as_deref());

        let experience = match input.experience {
            Some(exp) if exp > 0 => exp,
            _ => RpgEngine::calculate_quadrant_experience(Some(&quadrant)),
        };

        conn.execute(
            "INSERT INTO ActionTasks (
                Id, Title, Content, Category, Status, Quadrant, GrowthAttribute, Experience,
                DueAt, CompletedAt, CompletionNote, LinkedLifeRecordId, CreatedAt, UpdatedAt)
             VALUES (?1, ?2, ?3, ?4, 'Pending', ?5, ?6, ?7, ?8, NULL, '', NULL, ?9, ?9);",
            params![
                &id,
                &title,
                &content,
                &category,
                &quadrant,
                &growth_attr,
                experience,
                input.due_at,
                &now
            ],
        )?;

        Ok(ActionTask {
            id,
            title,
            content,
            category,
            status: "Pending".to_string(),
            quadrant,
            growth_attribute: growth_attr,
            experience,
            due_at: input.due_at,
            completed_at: None,
            completion_note: String::new(),
            linked_life_record_id: None,
            created_at: now.clone(),
            updated_at: now,
        })
    }

    /// 更新行动任务
    pub fn update_task(conn: &Connection, input: UpdateTaskInput) -> Result<bool> {
        let tx = rusqlite::Transaction::new_unchecked(conn, rusqlite::TransactionBehavior::Immediate)?;
        let conn = &tx;
        let now = chrono::Local::now().to_rfc3339();
        let title = input.title.trim().to_string();
        let content = input.content.unwrap_or_default().trim().to_string();
        let category = input.category.unwrap_or_else(|| "默认".to_string()).trim().to_string();
        let quadrant = input.quadrant.unwrap_or_else(|| "Q2".to_string()).trim().to_string();

        let (growth_attr, _) = RpgEngine::resolve_attribute(input.growth_attribute.as_deref());

        let experience = match input.experience {
            Some(exp) if exp > 0 => exp,
            _ => RpgEngine::calculate_quadrant_experience(Some(&quadrant)),
        };

        let (status, old_exp, old_attr): (String,i32,String) = conn.query_row("SELECT Status,Experience,GrowthAttribute FROM ActionTasks WHERE Id=?1",[&input.id],|r|Ok((r.get(0)?,r.get(1)?,r.get(2)?)))?;
        if status == "Completed" && (experience != old_exp || growth_attr != old_attr) {
            return Err(rusqlite::Error::InvalidParameterName("已完成任务的经验和成长属性已锁定，请先撤销完成".into()));
        }
        let affected = conn.execute(
            "UPDATE ActionTasks
             SET Title = ?1, Content = ?2, Category = ?3, Quadrant = ?4,
                 GrowthAttribute = ?5, Experience = ?6, DueAt = ?7, UpdatedAt = ?8
             WHERE Id = ?9;",
            params![
                &title,
                &content,
                &category,
                &quadrant,
                &growth_attr,
                experience,
                input.due_at,
                &now,
                &input.id
            ],
        )?;

        tx.commit()?;
        Ok(affected > 0)
    }

    /// 删除行动任务
    pub fn delete_task(conn: &Connection, id: &str) -> Result<bool> {
        let tx = conn.unchecked_transaction()?;
        crate::services::character_service::CharacterService::rollback_task_completion_in_transaction(&tx, id)?;
        let affected = tx.execute("DELETE FROM ActionTasks WHERE Id = ?1;", [id])?;
        tx.commit()?;
        Ok(affected > 0)
    }
}

#[cfg(test)]
mod quick_add_regression {
    use super::*;
    use crate::{db::Db, services::character_service::CharacterService};

    #[test]
    fn quick_add_payload_settles_mind_once_and_rolls_back() {
        let mut conn = Connection::open_in_memory().unwrap();
        Db::ensure_schema(&conn).unwrap();
        let input: CreateTaskInput = serde_json::from_str(
            r#"{"title":"快速任务回归","quadrant":"Q2","growthAttribute":"mind","experience":25}"#,
        ).unwrap();
        let task = ActionTaskService::create_task(&conn, input).unwrap();
        assert_eq!((task.quadrant.as_str(), task.growth_attribute.as_str(), task.experience), ("Q2", "mind", 25));
        let character = CharacterService::get_default_character(&conn).unwrap();
        let mind_before: i32 = conn.query_row("SELECT Experience FROM CharacterAttributes WHERE CharacterId=?1 AND AttributeKey='mind'", [&character.id], |row| row.get(0)).unwrap();

        let result = CharacterService::apply_task_completion(&mut conn, &task.id).unwrap();
        assert!(result.applied);
        assert_eq!(result.total_experience_granted, 25);
        assert_eq!(result.gains[0].attribute_key, "mind");
        assert!(CharacterService::apply_task_completion(&mut conn, &task.id).unwrap().is_duplicate);
        assert_eq!(CharacterService::get_default_character(&conn).unwrap().total_experience, character.total_experience + 25);
        let mind_after: i32 = conn.query_row("SELECT Experience FROM CharacterAttributes WHERE CharacterId=?1 AND AttributeKey='mind'", [&character.id], |row| row.get(0)).unwrap();
        assert_eq!(mind_after, mind_before + 25);
        let incorrect_logs: i32 = conn.query_row("SELECT COUNT(*) FROM AttributeLogs WHERE SourceId=?1 AND AttributeKey<>'mind'", [&task.id], |row| row.get(0)).unwrap();
        assert_eq!(incorrect_logs, 0);

        CharacterService::rollback_task_completion(&mut conn, &task.id).unwrap();
        assert_eq!(CharacterService::get_default_character(&conn).unwrap().total_experience, character.total_experience);
        let mind_restored: i32 = conn.query_row("SELECT Experience FROM CharacterAttributes WHERE CharacterId=?1 AND AttributeKey='mind'", [&character.id], |row| row.get(0)).unwrap();
        assert_eq!(mind_restored, mind_before);
    }
}
