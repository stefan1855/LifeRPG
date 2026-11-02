use rusqlite::{params, Connection};
use uuid::Uuid;

use crate::models::{SaveSkillInput, SkillItem};
use crate::services::character_service::CharacterService;

/// 技能树服务：技能定义 + 角色技能（等级/经验/解锁）
pub struct SkillService;

impl SkillService {
    fn now() -> String {
        chrono::Local::now().to_rfc3339()
    }

    pub fn list(conn: &Connection) -> rusqlite::Result<Vec<SkillItem>> {
        let character = CharacterService::get_default_character(conn)?;
        let mut stmt = conn.prepare(
            "SELECT sd.Id, sd.Name, sd.Category, sd.Description,
                    COALESCE(cs.Level, 0), COALESCE(cs.Experience, 0), COALESCE(cs.IsUnlocked, 0),
                    sd.CreatedAt, sd.UpdatedAt
             FROM SkillDefinitions sd
             LEFT JOIN CharacterSkills cs
               ON cs.SkillDefinitionId = sd.Id AND cs.CharacterId = ?1
             ORDER BY sd.Category, sd.Name",
        )?;
        let rows = stmt.query_map(params![&character.id], |row| {
            Ok(SkillItem {
                id: row.get(0)?,
                name: row.get(1)?,
                category: row.get(2)?,
                description: row.get(3)?,
                level: row.get(4)?,
                experience: row.get(5)?,
                is_unlocked: row.get::<_, i64>(6)? > 0,
                created_at: row.get(7)?,
                updated_at: row.get(8)?,
            })
        })?;
        rows.collect()
    }

    fn ensure_character_skill(conn: &Connection, skill_id: &str) -> Result<String, String> {
        let character = CharacterService::get_default_character(conn).map_err(|e| e.to_string())?;
        let existing: Option<String> = conn
            .query_row(
                "SELECT Id FROM CharacterSkills WHERE CharacterId = ?1 AND SkillDefinitionId = ?2",
                params![&character.id, skill_id],
                |r| r.get(0),
            )
            .ok();
        if let Some(id) = existing {
            return Ok(id);
        }
        let id = Uuid::new_v4().to_string();
        let now = Self::now();
        conn.execute(
            "INSERT INTO CharacterSkills (Id, CharacterId, SkillDefinitionId, Level, Experience, IsUnlocked, CreatedAt, UpdatedAt)
             VALUES (?1, ?2, ?3, 0, 0, 0, ?4, ?4)",
            params![&id, &character.id, skill_id, &now],
        )
        .map_err(|e| e.to_string())?;
        Ok(id)
    }

    pub fn save(conn: &Connection, input: SaveSkillInput) -> Result<String, String> {
        let now = Self::now();
        let desc = input.description.unwrap_or_default();
        match input.id {
            Some(id) if !id.is_empty() => {
                conn.execute(
                    "UPDATE SkillDefinitions SET Name = ?1, Category = ?2, Description = ?3, UpdatedAt = ?4 WHERE Id = ?5",
                    params![&input.name, &input.category, &desc, &now, &id],
                )
                .map_err(|e| e.to_string())?;
                Ok(id)
            }
            _ => {
                let id = Uuid::new_v4().to_string();
                conn.execute(
                    "INSERT INTO SkillDefinitions (Id, Name, Category, Description, CreatedAt, UpdatedAt)
                     VALUES (?1, ?2, ?3, ?4, ?5, ?5)",
                    params![&id, &input.name, &input.category, &desc, &now],
                )
                .map_err(|e| e.to_string())?;
                Self::ensure_character_skill(conn, &id)?;
                Ok(id)
            }
        }
    }

    /// 解锁技能（初始 Lv.1）
    pub fn unlock(conn: &Connection, skill_id: &str) -> Result<(), String> {
        let cs_id = Self::ensure_character_skill(conn, skill_id)?;
        let now = Self::now();
        conn.execute(
            "UPDATE CharacterSkills SET IsUnlocked = 1, Level = CASE WHEN Level < 1 THEN 1 ELSE Level END, UpdatedAt = ?1
             WHERE Id = ?2",
            params![&now, &cs_id],
        )
        .map_err(|e| e.to_string())?;
        Ok(())
    }

    pub fn relock(conn: &Connection, skill_id: &str) -> Result<(), String> {
        let cs_id = Self::ensure_character_skill(conn, skill_id)?;
        let now = Self::now();
        conn.execute(
            "UPDATE CharacterSkills SET IsUnlocked = 0, UpdatedAt = ?1 WHERE Id = ?2",
            params![&now, &cs_id],
        )
        .map_err(|e| e.to_string())?;
        Ok(())
    }

    /// 给技能加经验，每 100 经验升一级
    pub fn add_experience(conn: &Connection, skill_id: &str, amount: i32) -> Result<SkillItem, String> {
        let cs_id = Self::ensure_character_skill(conn, skill_id)?;
        let (level, exp): (i32, i32) = conn
            .query_row(
                "SELECT Level, Experience FROM CharacterSkills WHERE Id = ?1",
                params![&cs_id],
                |r| Ok((r.get(0)?, r.get(1)?)),
            )
            .map_err(|e| e.to_string())?;

        let mut new_exp = exp + amount.max(0);
        let mut new_level = level.max(1);
        while new_exp >= 100 {
            new_exp -= 100;
            new_level += 1;
        }
        let now = Self::now();
        conn.execute(
            "UPDATE CharacterSkills SET Level = ?1, Experience = ?2, IsUnlocked = 1, UpdatedAt = ?3 WHERE Id = ?4",
            params![new_level, new_exp, &now, &cs_id],
        )
        .map_err(|e| e.to_string())?;

        Self::list(conn)
            .map_err(|e| e.to_string())?
            .into_iter()
            .find(|s| s.id == skill_id)
            .ok_or_else(|| "技能不存在".to_string())
    }

    pub fn delete(conn: &Connection, skill_id: &str) -> Result<bool, String> {
        conn.execute(
            "DELETE FROM CharacterSkills WHERE SkillDefinitionId = ?1",
            params![skill_id],
        )
        .map_err(|e| e.to_string())?;
        conn.execute(
            "DELETE FROM SkillUnlockConditions WHERE SkillDefinitionId = ?1",
            params![skill_id],
        )
        .map_err(|e| e.to_string())?;
        let n = conn
            .execute("DELETE FROM SkillDefinitions WHERE Id = ?1", params![skill_id])
            .map_err(|e| e.to_string())?;
        Ok(n > 0)
    }
}
