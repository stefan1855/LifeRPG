use crate::models::{
    AttributeLog, Character, CharacterAttribute, DashboardSnapshot, RpgExperienceResult,
};
use crate::rpg_engine::RpgEngine;
use rusqlite::{params, Connection, Result};

pub struct CharacterService;

impl CharacterService {
    /// 获取当前主角角色
    pub fn get_default_character(conn: &Connection) -> Result<Character> {
        conn.query_row(
            "SELECT Id, Name, Description, AvatarImagePath, Level, TotalExperience, CreatedAt, LastGrowthAt, UpdatedAt
             FROM Characters
             ORDER BY CreatedAt ASC
             LIMIT 1;",
            [],
            |row| {
                Ok(Character {
                    id: row.get(0)?,
                    name: row.get(1)?,
                    description: row.get(2)?,
                    avatar_image_path: row.get(3)?,
                    level: row.get(4)?,
                    total_experience: row.get(5)?,
                    created_at: row.get(6)?,
                    last_growth_at: row.get(7)?,
                    updated_at: row.get(8)?,
                })
            },
        )
    }

    /// 获取角色的所有属性详情（包含等级进度计算）
    pub fn get_attributes(conn: &Connection, character_id: &str) -> Result<Vec<CharacterAttribute>> {
        let mut stmt = conn.prepare(
            "SELECT Id, CharacterId, AttributeKey, DisplayName, Level, Experience, BaseValue, CreatedAt, UpdatedAt
             FROM CharacterAttributes
             WHERE CharacterId = ?1
             ORDER BY CASE AttributeKey
                WHEN 'physique' THEN 0
                WHEN 'mind' THEN 1
                WHEN 'discipline' THEN 2
                WHEN 'expression' THEN 3
                WHEN 'creativity' THEN 4
                WHEN 'resources' THEN 5
                WHEN 'general' THEN 6
                ELSE 7
             END, DisplayName COLLATE NOCASE ASC;",
        )?;

        let rows = stmt.query_map([character_id], |row| {
            let exp: i32 = row.get(5)?;
            let level_res = RpgEngine::calculate_level(exp);

            Ok(CharacterAttribute {
                id: row.get(0)?,
                character_id: row.get(1)?,
                attribute_key: row.get(2)?,
                display_name: row.get(3)?,
                level: level_res.level,
                experience: exp,
                base_value: row.get(6)?,
                created_at: row.get(7)?,
                updated_at: row.get(8)?,
                exp_into_current_level: Some(level_res.experience_into_current_level),
                exp_to_next_level: Some(level_res.experience_to_next_level),
                exp_span: Some((level_res.experience_required_for_next_level - level_res.experience_required_for_current_level).max(1)),
                progress_percent: Some(level_res.progress_percent),
            })
        })?;

        let mut list = Vec::new();
        for r in rows {
            list.push(r?);
        }
        Ok(list)
    }

    /// 获取最近的属性经验流水日志
    pub fn get_recent_logs(conn: &Connection, limit: usize) -> Result<Vec<AttributeLog>> {
        let mut stmt = conn.prepare(
            "SELECT Id, CharacterId, AttributeKey, ExperienceDelta, SourceType, SourceId, Reason, CreatedAt
             FROM AttributeLogs
             ORDER BY CreatedAt DESC
             LIMIT ?1;",
        )?;

        let rows = stmt.query_map([limit], |row| {
            Ok(AttributeLog {
                id: row.get(0)?,
                character_id: row.get(1)?,
                attribute_key: row.get(2)?,
                experience_delta: row.get(3)?,
                source_type: row.get(4)?,
                source_id: row.get(5)?,
                reason: row.get(6)?,
                created_at: row.get(7)?,
            })
        })?;

        let mut list = Vec::new();
        for r in rows {
            list.push(r?);
        }
        Ok(list)
    }

    /// 获取仪表盘综合状态快照
    pub fn get_dashboard_snapshot(conn: &Connection) -> Result<DashboardSnapshot> {
        let character = Self::get_default_character(conn)?;
        let level_info = RpgEngine::calculate_level(character.total_experience);
        let attributes = Self::get_attributes(conn, &character.id)?;

        let today = chrono::Local::now().format("%Y-%m-%d").to_string();

        let today_completed_count: i32 = conn.query_row(
            "SELECT COUNT(*) FROM ActionTasks WHERE Status = 'Completed' AND CompletedAt LIKE ?1;",
            [format!("{}%", today)],
            |row| row.get(0),
        ).unwrap_or(0);

        let today_total_count: i32 = conn.query_row(
            "SELECT COUNT(*) FROM ActionTasks WHERE (DueAt LIKE ?1 OR CreatedAt LIKE ?1 OR (Status = 'Completed' AND CompletedAt LIKE ?1));",
            [format!("{}%", today)],
            |row| row.get(0),
        ).unwrap_or(0);

        Ok(DashboardSnapshot {
            character,
            level_info,
            attributes,
            today_completed_count,
            today_total_count,
        })
    }

    /// 完成任务并发放 RPG 经验（事务 + 防重复幂等）
    pub fn apply_task_completion(conn: &mut Connection, task_id: &str) -> Result<RpgExperienceResult> {
        let character = Self::get_default_character(conn)?;

        // 1. 读取任务信息
        let (title, exp, growth_attr): (String, i32, String) = conn.query_row(
            "SELECT Title, Experience, GrowthAttribute FROM ActionTasks WHERE Id = ?1;",
            [task_id],
            |row| Ok((row.get(0)?, row.get(1)?, row.get(2)?)),
        )?;

        // 2. 检查幂等性：是否已经发过经验
        let event_exists: i64 = conn.query_row(
            "SELECT COUNT(*) FROM RpgExperienceEvents WHERE SourceType = 'ActionTask' AND SourceId = ?1;",
            [task_id],
            |row| row.get(0),
        )?;

        let gains = RpgEngine::allocate_task_experience(exp, Some(&growth_attr));
        let now = chrono::Local::now().to_rfc3339();

        if event_exists > 0 {
            // 已经发过经验，仅更新任务状态为完成
            conn.execute(
                "UPDATE ActionTasks SET Status = 'Completed', CompletedAt = ?1, UpdatedAt = ?1 WHERE Id = ?2;",
                params![&now, task_id],
            )?;

            return Ok(RpgExperienceResult {
                applied: false,
                is_duplicate: true,
                total_experience_granted: 0,
                gains,
                new_level: character.level,
                leveled_up: false,
            });
        }

        // 3. 开启事务写入经验与日志
        let tx = conn.transaction()?;

        // 更新角色总经验与等级
        let new_total_exp = character.total_experience + exp.max(0);
        let new_level_info = RpgEngine::calculate_level(new_total_exp);
        let leveled_up = new_level_info.level > character.level;

        tx.execute(
            "UPDATE Characters SET Level = ?1, TotalExperience = ?2, LastGrowthAt = ?3, UpdatedAt = ?3 WHERE Id = ?4;",
            params![new_level_info.level, new_total_exp, &now, &character.id],
        )?;

        // 发放属性经验与写入属性日志
        for gain in &gains {
            if gain.experience_delta <= 0 {
                continue;
            }

            let attr_exists: i64 = tx.query_row(
                "SELECT COUNT(*) FROM CharacterAttributes WHERE CharacterId = ?1 AND AttributeKey = ?2;",
                params![&character.id, &gain.attribute_key],
                |row| row.get(0),
            )?;

            if attr_exists == 0 {
                let attr_id = uuid::Uuid::new_v4().to_string();
                let attr_level_info = RpgEngine::calculate_level(gain.experience_delta);
                tx.execute(
                    "INSERT INTO CharacterAttributes (Id, CharacterId, AttributeKey, DisplayName, Level, Experience, BaseValue, CreatedAt, UpdatedAt)
                     VALUES (?1, ?2, ?3, ?4, ?5, ?6, 0, ?7, ?7);",
                    params![&attr_id, &character.id, &gain.attribute_key, &gain.display_name, attr_level_info.level, gain.experience_delta, &now],
                )?;
            } else {
                let current_attr_exp: i32 = tx.query_row(
                    "SELECT Experience FROM CharacterAttributes WHERE CharacterId = ?1 AND AttributeKey = ?2;",
                    params![&character.id, &gain.attribute_key],
                    |row| row.get(0),
                )?;
                let updated_attr_exp = current_attr_exp + gain.experience_delta;
                let attr_level_info = RpgEngine::calculate_level(updated_attr_exp);

                tx.execute(
                    "UPDATE CharacterAttributes SET Level = ?1, Experience = ?2, UpdatedAt = ?3 WHERE CharacterId = ?4 AND AttributeKey = ?5;",
                    params![attr_level_info.level, updated_attr_exp, &now, &character.id, &gain.attribute_key],
                )?;
            }

            // 写入属性日志
            let log_id = uuid::Uuid::new_v4().to_string();
            let reason = format!("完成行动任务：{}", title);
            tx.execute(
                "INSERT INTO AttributeLogs (Id, CharacterId, AttributeKey, ExperienceDelta, SourceType, SourceId, Reason, CreatedAt)
                 VALUES (?1, ?2, ?3, ?4, 'ActionTask', ?5, ?6, ?7);",
                params![&log_id, &character.id, &gain.attribute_key, gain.experience_delta, task_id, &reason, &now],
            )?;
        }

        // 写入经验事件（防重幂等）
        let event_id = uuid::Uuid::new_v4().to_string();
        let event_note = format!("完成行动任务：{}", title);
        tx.execute(
            "INSERT INTO RpgExperienceEvents (Id, SourceType, SourceId, CharacterId, TotalExperience, Note, CreatedAt)
             VALUES (?1, 'ActionTask', ?2, ?3, ?4, ?5, ?6);",
            params![&event_id, task_id, &character.id, exp, &event_note, &now],
        )?;

        // 更新任务自身状态为 Completed
        tx.execute(
            "UPDATE ActionTasks SET Status = 'Completed', CompletedAt = ?1, UpdatedAt = ?1 WHERE Id = ?2;",
            params![&now, task_id],
        )?;

        tx.commit()?;

        Ok(RpgExperienceResult {
            applied: true,
            is_duplicate: false,
            total_experience_granted: exp.max(0),
            gains,
            new_level: new_level_info.level,
            leveled_up,
        })
    }

    /// 回滚/取消完成任务
    pub fn rollback_task_completion(conn: &mut Connection, task_id: &str) -> Result<bool> {
        let character = Self::get_default_character(conn)?;
        let now = chrono::Local::now().to_rfc3339();

        let event_opt: Option<(i32, String)> = conn.query_row(
            "SELECT TotalExperience, Note FROM RpgExperienceEvents WHERE SourceType = 'ActionTask' AND SourceId = ?1;",
            [task_id],
            |row| Ok((row.get(0)?, row.get(1)?)),
        ).ok();

        let tx = conn.transaction()?;

        if let Some((granted_exp, _)) = event_opt {
            // 扣除角色总经验并重算等级
            let new_total_exp = (character.total_experience - granted_exp).max(0);
            let new_level_info = RpgEngine::calculate_level(new_total_exp);

            tx.execute(
                "UPDATE Characters SET Level = ?1, TotalExperience = ?2, UpdatedAt = ?3 WHERE Id = ?4;",
                params![new_level_info.level, new_total_exp, &now, &character.id],
            )?;

            // 查找对应的属性日志并扣减属性经验
            let attr_logs: Vec<(String, i32)> = {
                let mut stmt = tx.prepare(
                    "SELECT AttributeKey, ExperienceDelta FROM AttributeLogs WHERE SourceType = 'ActionTask' AND SourceId = ?1;",
                )?;
                let rows = stmt.query_map([task_id], |row| Ok((row.get(0)?, row.get(1)?)))?;
                let mut list = Vec::new();
                for r in rows {
                    list.push(r?);
                }
                list
            };

            for (attr_key, delta) in attr_logs {
                let cur_exp: i32 = tx.query_row(
                    "SELECT Experience FROM CharacterAttributes WHERE CharacterId = ?1 AND AttributeKey = ?2;",
                    params![&character.id, &attr_key],
                    |row| row.get(0),
                ).unwrap_or(0);

                let updated_exp = (cur_exp - delta).max(0);
                let attr_lv = RpgEngine::calculate_level(updated_exp).level;

                tx.execute(
                    "UPDATE CharacterAttributes SET Level = ?1, Experience = ?2, UpdatedAt = ?3 WHERE CharacterId = ?4 AND AttributeKey = ?5;",
                    params![attr_lv, updated_exp, &now, &character.id, &attr_key],
                )?;
            }

            // 删除事件与日志
            tx.execute(
                "DELETE FROM AttributeLogs WHERE SourceType = 'ActionTask' AND SourceId = ?1;",
                [task_id],
            )?;
            tx.execute(
                "DELETE FROM RpgExperienceEvents WHERE SourceType = 'ActionTask' AND SourceId = ?1;",
                [task_id],
            )?;
        }

        // 将任务状态重置为 Pending
        tx.execute(
            "UPDATE ActionTasks SET Status = 'Pending', CompletedAt = NULL, UpdatedAt = ?1 WHERE Id = ?2;",
            params![&now, task_id],
        )?;

        tx.commit()?;
        Ok(true)
    }
}
