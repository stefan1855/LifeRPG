use rusqlite::{params, Connection};
use uuid::Uuid;

use crate::models::{CreateRecordInput, LifeRecord, LifeRecordImage, UpdateRecordInput};
use crate::services::image_service::ImageService;

/// 行为记录（生活流水）服务
pub struct RecordService;

impl RecordService {
    fn now() -> String {
        chrono::Local::now().to_rfc3339()
    }

    fn load_images(conn: &Connection, record_id: &str) -> rusqlite::Result<Vec<LifeRecordImage>> {
        let mut stmt = conn.prepare(
            "SELECT Id, LifeRecordId, FileName, FilePath, CreatedAt
             FROM LifeRecordImages WHERE LifeRecordId = ?1 ORDER BY CreatedAt ASC",
        )?;
        let rows = stmt.query_map(params![record_id], |row| {
            Ok(LifeRecordImage {
                id: row.get(0)?,
                life_record_id: row.get(1)?,
                file_name: row.get(2)?,
                file_path: row.get(3)?,
                created_at: row.get(4)?,
            })
        })?;
        rows.collect()
    }

    fn row_to_record(conn: &Connection, row: &rusqlite::Row) -> rusqlite::Result<LifeRecord> {
        let id: String = row.get(0)?;
        Ok(LifeRecord {
            id: id.clone(),
            title: row.get(1)?,
            content: row.get(2)?,
            category: row.get(3)?,
            experience: row.get(4)?,
            created_at: row.get(5)?,
            updated_at: row.get(6)?,
            images: Self::load_images(conn, &id)?,
        })
    }

    pub fn list(
        conn: &Connection,
        keyword: Option<String>,
        category: Option<String>,
        limit: Option<i64>,
    ) -> rusqlite::Result<Vec<LifeRecord>> {
        Self::list_page(conn,keyword,category,limit,0)
    }

    pub fn list_page(conn: &Connection, keyword: Option<String>, category: Option<String>, limit: Option<i64>, offset: i64) -> rusqlite::Result<Vec<LifeRecord>> {
        let kw = format!("%{}%", keyword.unwrap_or_default());
        let cat = category.unwrap_or_default();
        let lim = limit.unwrap_or(50).clamp(1, 500);

        let mut stmt = conn.prepare(
            "SELECT Id, Title, Content, Category, Experience, CreatedAt, UpdatedAt
             FROM LifeRecords
             WHERE (Title LIKE ?1 OR Content LIKE ?1)
               AND (?2 = '' OR Category = ?2)
             ORDER BY CreatedAt DESC, Id DESC LIMIT ?3 OFFSET ?4",
        )?;
        let ids: Vec<String> = stmt
            .query_map(params![kw, cat, lim, offset.max(0)], |row| row.get::<_, String>(0))?
            .collect::<rusqlite::Result<_>>()?;

        let mut out = Vec::with_capacity(ids.len());
        let mut stmt2 = conn.prepare(
            "SELECT Id, Title, Content, Category, Experience, CreatedAt, UpdatedAt
             FROM LifeRecords WHERE Id = ?1",
        )?;
        for id in ids {
            let rec = stmt2.query_row(params![id], |row| Self::row_to_record(conn, row))?;
            out.push(rec);
        }
        Ok(out)
    }

    pub fn get(conn: &Connection, id: &str) -> rusqlite::Result<LifeRecord> {
        conn.query_row(
            "SELECT Id, Title, Content, Category, Experience, CreatedAt, UpdatedAt
             FROM LifeRecords WHERE Id = ?1",
            params![id],
            |row| Self::row_to_record(conn, row),
        )
    }

    pub fn create(conn: &Connection, input: CreateRecordInput) -> Result<LifeRecord, String> {
        let tx = rusqlite::Transaction::new_unchecked(conn, rusqlite::TransactionBehavior::Immediate).map_err(|e| e.to_string())?;
        let result = Self::create_inner(&tx, input)?;
        tx.commit().map_err(|e| e.to_string())?;
        Ok(result)
    }

    fn create_inner(conn: &Connection, input: CreateRecordInput) -> Result<LifeRecord, String> {
        let id = Uuid::new_v4().to_string();
        let now = Self::now();
        let content = input.content.unwrap_or_default();
        let category = input
            .category
            .filter(|c| !c.trim().is_empty())
            .unwrap_or_else(|| "日常".to_string());
        let experience = input.experience.unwrap_or(0);

        conn.execute(
            "INSERT INTO LifeRecords (Id, Title, Content, Category, Experience, CreatedAt, UpdatedAt)
             VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?6)",
            params![&id, &input.title, &content, &category, experience, &now],
        )
        .map_err(|e| e.to_string())?;

        if let Some(paths) = input.image_paths {
            for src in paths {
                Self::attach_image(conn, &id, &src)?;
            }
        }

        crate::services::character_service::CharacterService::sync_record_experience(
            conn,
            &id,
            &input.title,
            experience,
        ).map_err(|e| e.to_string())?;

        Self::get(conn, &id).map_err(|e| e.to_string())
    }

    pub fn update(conn: &Connection, input: UpdateRecordInput) -> Result<LifeRecord, String> {
        let tx = rusqlite::Transaction::new_unchecked(conn, rusqlite::TransactionBehavior::Immediate).map_err(|e| e.to_string())?;
        let result = Self::update_inner(&tx, input)?;
        tx.commit().map_err(|e| e.to_string())?;
        Ok(result)
    }

    fn update_inner(conn: &Connection, input: UpdateRecordInput) -> Result<LifeRecord, String> {
        let now = Self::now();
        let current = Self::get(conn, &input.id).map_err(|e| e.to_string())?;
        let content = input.content.unwrap_or(current.content);
        let category = input
            .category
            .filter(|c| !c.trim().is_empty())
            .unwrap_or(current.category);
        let experience = input.experience.unwrap_or(current.experience);

        conn.execute(
            "UPDATE LifeRecords SET Title = ?1, Content = ?2, Category = ?3, Experience = ?4, UpdatedAt = ?5
             WHERE Id = ?6",
            params![&input.title, &content, &category, experience, &now, &input.id],
        )
        .map_err(|e| e.to_string())?;

        if let Some(ids) = input.remove_image_ids {
            for img_id in ids {
                Self::remove_image(conn, &img_id)?;
            }
        }
        if let Some(paths) = input.add_image_paths {
            for src in paths {
                Self::attach_image(conn, &input.id, &src)?;
            }
        }

        crate::services::character_service::CharacterService::sync_record_experience(
            conn,
            &input.id,
            &input.title,
            experience,
        ).map_err(|e| e.to_string())?;

        Self::get(conn, &input.id).map_err(|e| e.to_string())
    }

    pub fn delete(conn: &Connection, id: &str) -> Result<bool, String> {
        let tx = rusqlite::Transaction::new_unchecked(conn, rusqlite::TransactionBehavior::Immediate).map_err(|e| e.to_string())?;
        let conn = &*tx;
        // 经验与记录在同一事务中结算
        crate::services::character_service::CharacterService::sync_record_experience(
            conn, id, "", 0,
        ).map_err(|e| e.to_string())?;
        let affected = conn
            .execute("DELETE FROM LifeRecords WHERE Id = ?1", params![id])
            .map_err(|e| e.to_string())?;
        tx.commit().map_err(|e| e.to_string())?;
        Ok(affected > 0)
    }

    pub fn attach_image(conn: &Connection, record_id: &str, source: &str) -> Result<(), String> {
        let (file_name, file_path) = ImageService::import(source)?;
        let now = Self::now();
        conn.execute(
            "INSERT INTO LifeRecordImages (Id, LifeRecordId, FileName, FilePath, CreatedAt)
             VALUES (?1, ?2, ?3, ?4, ?5)",
            params![Uuid::new_v4().to_string(), record_id, file_name, file_path, now],
        )
        .map_err(|e| e.to_string())?;
        Ok(())
    }

    pub fn remove_image(conn: &Connection, image_id: &str) -> Result<(), String> {
        conn.execute("DELETE FROM LifeRecordImages WHERE Id = ?1", params![image_id])
            .map_err(|e| e.to_string())?;
        Ok(())
    }
}

