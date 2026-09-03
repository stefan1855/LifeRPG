use rusqlite::{params, Connection, Result};
use std::fs;
use std::path::PathBuf;

pub struct Db;

impl Db {
    /// 获取 LifeRPG 数据库的主路径
    pub fn get_db_path() -> PathBuf {
        // 1. 优先使用 LifeRPG.Native 标准数据目录
        if let Some(local_app_data) = dirs::data_local_dir() {
            let native_db = local_app_data.join("LifeRPG.Native").join("data").join("liferpg.db");
            if native_db.exists() {
                return native_db;
            }

            // 如果 Native 目录存在但 db 还没创建，依然优先建在 Native 目录下以实现天然共用
            let native_dir = local_app_data.join("LifeRPG.Native").join("data");
            if native_dir.exists() {
                return native_db;
            }

            // 2. 否则使用 LifeRPG.Widget 专用目录
            let widget_dir = local_app_data.join("LifeRPG.Widget").join("data");
            let _ = fs::create_dir_all(&widget_dir);
            return widget_dir.join("liferpg.db");
        }

        // 3. 兜底当前目录
        PathBuf::from("liferpg.db")
    }

    /// 打开 SQLite 数据库连接并配置 PRAGMA
    pub fn open() -> Result<Connection> {
        let db_path = Self::get_db_path();
        if let Some(parent) = db_path.parent() {
            let _ = fs::create_dir_all(parent);
        }

        let conn = Connection::open(&db_path)?;

        conn.execute_batch(
            "PRAGMA foreign_keys = ON;
             PRAGMA journal_mode = WAL;
             PRAGMA busy_timeout = 5000;",
        )?;

        Self::ensure_schema(&conn)?;
        Ok(conn)
    }

    /// 保证 LifeRPG 基础表结构完整存在
    pub fn ensure_schema(conn: &Connection) -> Result<()> {
        conn.execute_batch(
            r#"
            CREATE TABLE IF NOT EXISTS Characters (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                AvatarImagePath TEXT,
                Description TEXT,
                Level INTEGER NOT NULL DEFAULT 1,
                TotalExperience INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                LastGrowthAt TEXT,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CharacterAttributes (
                Id TEXT NOT NULL PRIMARY KEY,
                CharacterId TEXT NOT NULL,
                AttributeKey TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                Level INTEGER NOT NULL DEFAULT 1,
                Experience INTEGER NOT NULL DEFAULT 0,
                BaseValue INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (CharacterId) REFERENCES Characters(Id) ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_CharacterAttributes_Character_Key
            ON CharacterAttributes(CharacterId, AttributeKey);

            CREATE TABLE IF NOT EXISTS AttributeLogs (
                Id TEXT NOT NULL PRIMARY KEY,
                CharacterId TEXT NOT NULL,
                AttributeKey TEXT NOT NULL,
                ExperienceDelta INTEGER NOT NULL,
                SourceType TEXT NOT NULL,
                SourceId TEXT NOT NULL,
                Reason TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (CharacterId) REFERENCES Characters(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS RpgExperienceEvents (
                Id TEXT NOT NULL PRIMARY KEY,
                SourceType TEXT NOT NULL,
                SourceId TEXT NOT NULL,
                CharacterId TEXT NOT NULL,
                TotalExperience INTEGER NOT NULL,
                Note TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (CharacterId) REFERENCES Characters(Id) ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_RpgExperienceEvents_Source
            ON RpgExperienceEvents(SourceType, SourceId);

            CREATE TABLE IF NOT EXISTS ActionTasks (
                Id TEXT NOT NULL PRIMARY KEY,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL DEFAULT '',
                Category TEXT NOT NULL DEFAULT '默认',
                Status TEXT NOT NULL DEFAULT 'Pending',
                Quadrant TEXT NOT NULL DEFAULT 'Q2',
                GrowthAttribute TEXT NOT NULL DEFAULT 'mind',
                Experience INTEGER NOT NULL DEFAULT 40,
                DueAt TEXT,
                CompletedAt TEXT,
                CompletionNote TEXT NOT NULL DEFAULT '',
                LinkedLifeRecordId TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS GrowthAttributes (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                Value INTEGER NOT NULL DEFAULT 0,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS LifeRecords (
                Id TEXT NOT NULL PRIMARY KEY,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL,
                Category TEXT NOT NULL,
                Experience INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            "#,
        )?;

        // 初始化默认角色和六大基础属性（若不存在）
        Self::ensure_default_character(conn)?;

        Ok(())
    }

    /// 确保默认角色存在
    fn ensure_default_character(conn: &Connection) -> Result<()> {
        let count: i64 = conn.query_row(
            "SELECT COUNT(*) FROM Characters LIMIT 1;",
            [],
            |row| row.get(0),
        )?;

        let now = chrono::Local::now().to_rfc3339();

        let character_id = if count == 0 {
            let id = uuid::Uuid::new_v4().to_string();
            conn.execute(
                "INSERT INTO Characters (Id, Name, Description, Level, TotalExperience, CreatedAt, UpdatedAt)
                 VALUES (?1, '人生主角', '这是我的人生RPG角色。', 1, 0, ?2, ?2);",
                params![&id, &now],
            )?;
            id
        } else {
            conn.query_row(
                "SELECT Id FROM Characters ORDER BY CreatedAt ASC LIMIT 1;",
                [],
                |row| row.get::<_, String>(0),
            )?
        };

        // 确保六大属性均已创建
        for (key, display) in crate::rpg_engine::DEFAULT_ATTRIBUTES {
            let attr_exists: i64 = conn.query_row(
                "SELECT COUNT(*) FROM CharacterAttributes WHERE CharacterId = ?1 AND AttributeKey = ?2;",
                params![&character_id, key],
                |row| row.get(0),
            )?;

            if attr_exists == 0 {
                let attr_id = uuid::Uuid::new_v4().to_string();
                conn.execute(
                    "INSERT INTO CharacterAttributes (Id, CharacterId, AttributeKey, DisplayName, Level, Experience, BaseValue, CreatedAt, UpdatedAt)
                     VALUES (?1, ?2, ?3, ?4, 1, 0, 0, ?5, ?5);",
                    params![&attr_id, &character_id, key, display, &now],
                )?;
            }
        }

        Ok(())
    }
}
