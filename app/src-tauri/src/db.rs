use rusqlite::{params, Connection, Result};
use std::fs;
use std::path::PathBuf;

use crate::services::config_service::ConfigService;

pub struct Db;

impl Db {
    /// 数据根目录（用户首次启动时选定）；未配置则报错，由前端引导用户设置
    pub fn data_root() -> std::result::Result<PathBuf, String> {
        ConfigService::data_root()?.ok_or_else(|| "尚未设置数据目录".to_string())
    }

    /// 获取 LifeRPG 数据库的主路径
    pub fn get_db_path() -> std::result::Result<PathBuf, String> {
        let root = Self::data_root()?;
        let dir = root.join("data");
        fs::create_dir_all(&dir).map_err(|e| format!("无法创建数据目录: {}", e))?;
        Ok(dir.join("liferpg.db"))
    }

    /// 图片证据的落盘目录
    pub fn get_images_dir() -> std::result::Result<PathBuf, String> {
        let root = Self::data_root()?;
        let dir = root.join("images");
        fs::create_dir_all(&dir).map_err(|e| format!("无法创建图片目录: {}", e))?;
        Ok(dir)
    }

    /// 备份输出目录
    pub fn get_backup_dir() -> std::result::Result<PathBuf, String> {
        let root = Self::data_root()?;
        let dir = root.join("backups");
        fs::create_dir_all(&dir).map_err(|e| format!("无法创建备份目录: {}", e))?;
        Ok(dir)
    }

    /// 打开 SQLite 数据库连接并配置 PRAGMA
    pub fn open() -> std::result::Result<Connection, String> {
        let root = Self::data_root()?;
        Self::open_at(&root)
    }

    /// Validate a candidate root without changing the portable configuration.
    pub fn open_at(root: &std::path::Path) -> std::result::Result<Connection, String> {
        let db_path = root.join("data").join("liferpg.db");
        if let Some(parent) = db_path.parent() {
            fs::create_dir_all(parent).map_err(|e| format!("无法创建数据目录: {}",e))?;
        }

        let conn = Connection::open(&db_path).map_err(|e| format!("打开数据库失败: {}", e))?;

        conn.execute_batch(
            "PRAGMA foreign_keys = ON;
             PRAGMA journal_mode = WAL;
             PRAGMA busy_timeout = 5000;",
        )
        .map_err(|e| format!("数据库配置失败: {}", e))?;

        Self::ensure_schema(&conn).map_err(|e| format!("初始化表结构失败: {}", e))?;
        // 仅在真实启动路径写入起始契约（内存库/测试不写入）
        Self::seed_starter_content(&conn).map_err(|e| format!("写入起始契约失败: {}", e))?;
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

            CREATE TABLE IF NOT EXISTS LifeRecordImages (
                Id TEXT NOT NULL PRIMARY KEY,
                LifeRecordId TEXT NOT NULL,
                FileName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (LifeRecordId) REFERENCES LifeRecords(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_LifeRecordImages_Record
            ON LifeRecordImages(LifeRecordId);

            CREATE TABLE IF NOT EXISTS ActionTaskImages (
                Id TEXT NOT NULL PRIMARY KEY,
                ActionTaskId TEXT NOT NULL,
                FileName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (ActionTaskId) REFERENCES ActionTasks(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_ActionTaskImages_Task
            ON ActionTaskImages(ActionTaskId);

            CREATE INDEX IF NOT EXISTS IX_ActionTasks_Status
            ON ActionTasks(Status, DueAt);

            CREATE INDEX IF NOT EXISTS IX_LifeRecords_Created
            ON LifeRecords(CreatedAt DESC);

            CREATE TABLE IF NOT EXISTS EquipmentDefinitions (
                Id TEXT NOT NULL PRIMARY KEY,
                RealItemName TEXT NOT NULL DEFAULT '',
                GameItemName TEXT NOT NULL DEFAULT '',
                Category TEXT NOT NULL DEFAULT 'chest',
                Status TEXT NOT NULL DEFAULT 'Owned',
                RealEffect TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CharacterEquipment (
                Id TEXT NOT NULL PRIMARY KEY,
                CharacterId TEXT NOT NULL,
                EquipmentDefinitionId TEXT NOT NULL,
                Status TEXT NOT NULL DEFAULT 'Equipped',
                EquippedAt TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (CharacterId) REFERENCES Characters(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_CharacterEquipment_Character
            ON CharacterEquipment(CharacterId);

            CREATE TABLE IF NOT EXISTS EquipmentAttributeBonuses (
                Id TEXT NOT NULL PRIMARY KEY,
                EquipmentDefinitionId TEXT NOT NULL,
                AttributeKey TEXT NOT NULL,
                BonusValue INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS IX_EquipBonus_Def
            ON EquipmentAttributeBonuses(EquipmentDefinitionId);

            CREATE TABLE IF NOT EXISTS SkillDefinitions (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                Category TEXT NOT NULL DEFAULT '通用',
                Description TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CharacterSkills (
                Id TEXT NOT NULL PRIMARY KEY,
                CharacterId TEXT NOT NULL,
                SkillDefinitionId TEXT NOT NULL,
                Level INTEGER NOT NULL DEFAULT 0,
                Experience INTEGER NOT NULL DEFAULT 0,
                IsUnlocked INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (CharacterId) REFERENCES Characters(Id) ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_CharacterSkills_Char_Skill
            ON CharacterSkills(CharacterId, SkillDefinitionId);

            CREATE TABLE IF NOT EXISTS SkillUnlockConditions (
                Id TEXT NOT NULL PRIMARY KEY,
                SkillDefinitionId TEXT NOT NULL,
                ConditionType TEXT NOT NULL,
                ConditionValue TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS NpcDefinitions (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                RealIdentity TEXT NOT NULL DEFAULT '',
                Category TEXT NOT NULL DEFAULT '普通',
                InfluenceType TEXT NOT NULL DEFAULT '中性',
                LinkedDomain TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS NpcRelationships (
                Id TEXT NOT NULL PRIMARY KEY,
                CharacterId TEXT NOT NULL,
                NpcDefinitionId TEXT NOT NULL,
                RelationshipValue INTEGER NOT NULL DEFAULT 0,
                LastInteractionAt TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (CharacterId) REFERENCES Characters(Id) ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_NpcRel_Char_Npc
            ON NpcRelationships(CharacterId, NpcDefinitionId);

            CREATE TABLE IF NOT EXISTS MapLocations (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                LocationType TEXT NOT NULL DEFAULT '区域',
                Description TEXT NOT NULL DEFAULT '',
                IsUnlocked INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            "#,
        )?;

        // 初始化默认角色和六大基础属性（若不存在）
        Self::ensure_default_character(conn)?;
        conn.execute_batch("CREATE TABLE IF NOT EXISTS AppMetadata (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);")?;
        if conn.query_row("SELECT COUNT(*) FROM AppMetadata WHERE Key='record_attribute_v1'", [], |r| r.get::<_,i64>(0))? == 0 {
            let tx = conn.unchecked_transaction()?;
            let amount: i32 = tx.query_row("SELECT COALESCE(SUM(TotalExperience),0) FROM RpgExperienceEvents WHERE SourceType='LifeRecord'", [], |r| r.get(0))?;
            if amount > 0 {
                let character = crate::services::character_service::CharacterService::get_default_character(&tx)?;
                let exp: i32 = tx.query_row("SELECT Experience FROM CharacterAttributes WHERE CharacterId=?1 AND AttributeKey='mind'", [&character.id], |r| r.get(0))?;
                let updated = exp.saturating_add(amount);
                tx.execute("UPDATE CharacterAttributes SET Experience=?1,Level=?2 WHERE CharacterId=?3 AND AttributeKey='mind'", params![updated,crate::rpg_engine::RpgEngine::calculate_level(updated).level,character.id])?;
            }
            tx.execute("INSERT INTO AppMetadata(Key,Value) VALUES ('record_attribute_v1','1')", [])?;
            tx.commit()?;
        }


        let has_fit:bool=conn.prepare("PRAGMA table_info(Characters)")?.query_map([],|r|r.get::<_,String>(1))?.collect::<Result<Vec<_>>>()?.iter().any(|c|c=="AvatarFit");
        if !has_fit{conn.execute_batch("ALTER TABLE Characters ADD COLUMN AvatarFit TEXT NOT NULL DEFAULT 'contain'")?;}
        conn.execute_batch("CREATE VIEW IF NOT EXISTS EffectiveCharacterEquipment AS
          SELECT Id,CharacterId,EquipmentDefinitionId,Status,EquippedAt,CreatedAt,UpdatedAt FROM (
            SELECT ce.*,ROW_NUMBER() OVER(PARTITION BY ce.CharacterId,ed.Category ORDER BY ce.EquippedAt DESC,ce.CreatedAt DESC,ce.Id DESC) AS Position
            FROM CharacterEquipment ce JOIN EquipmentDefinitions ed ON ed.Id=ce.EquipmentDefinitionId WHERE ce.Status='Equipped') WHERE Position=1;")?;
        Ok(())
    }

    /// 首次运行时写入 3 条起始任务与 1 条示例记录
    fn seed_starter_content(conn: &Connection) -> Result<()> {
        conn.execute_batch("CREATE TABLE IF NOT EXISTS AppMetadata (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);")?;
        if conn.query_row("SELECT COUNT(*) FROM AppMetadata WHERE Key='starter_content'", [], |r| r.get::<_, i64>(0))? > 0 { return Ok(()); }
        let tx = conn.unchecked_transaction()?;
        let conn = &*tx;
        conn.execute("INSERT INTO AppMetadata (Key,Value) VALUES ('starter_content','1')", [])?;
        let tasks: i64 = conn.query_row("SELECT COUNT(*) FROM ActionTasks", [], |r| r.get(0))?;
        let records: i64 = conn.query_row("SELECT COUNT(*) FROM LifeRecords", [], |r| r.get(0))?;
        let library: i64 = conn.query_row("SELECT (SELECT COUNT(*) FROM EquipmentDefinitions) + (SELECT COUNT(*) FROM SkillDefinitions) + (SELECT COUNT(*) FROM MapLocations)", [], |r| r.get(0))?;
        if tasks > 0 || records > 0 || library > 0 {
            tx.commit()?;
            return Ok(());
        }

        let now = chrono::Local::now().to_rfc3339();
        let starters = [
            (
                "完成第一次灵魂契约",
                "点一下左侧「行动任务」里的勾选框，看看经验如何上涨",
                "入门",
                "Q2",
                "discipline",
                25,
            ),
            (
                "写一条今天的行为记录",
                "去「行为记录」记录一件今天真正做过的事，可以配图",
                "入门",
                "Q1",
                "mind",
                30,
            ),
            (
                "设定本周最重要的一件事",
                "把它放进「重要不紧急」，这是长期成长的象限",
                "入门",
                "Q2",
                "creativity",
                25,
            ),
        ];

        for (title, content, category, quadrant, attr, exp) in starters {
            conn.execute(
                "INSERT INTO ActionTasks (Id, Title, Content, Category, Status, Quadrant, GrowthAttribute, Experience, CompletionNote, CreatedAt, UpdatedAt)
                 VALUES (?1, ?2, ?3, ?4, 'Pending', ?5, ?6, ?7, '', ?8, ?8);",
                params![
                    uuid::Uuid::new_v4().to_string(),
                    title,
                    content,
                    category,
                    quadrant,
                    attr,
                    exp,
                    &now
                ],
            )?;
        }

        // 技能示例（可删）
        for (name, category, desc) in [
            ("深度工作", "专业", "连续专注 90 分钟不被打断"),
            ("身体训练", "生活", "每周三次力量或有氧训练"),
            ("复盘写作", "专业", "每周写一次复盘，把经验沉淀下来"),
        ] {
            conn.execute(
                "INSERT INTO SkillDefinitions (Id, Name, Category, Description, CreatedAt, UpdatedAt)
                 VALUES (?1, ?2, ?3, ?4, ?5, ?5);",
                params![uuid::Uuid::new_v4().to_string(), name, category, desc, &now],
            )?;
        }

        // 地点示例（可删）
        for (name, ltype, desc) in [
            ("书房", "据点", "主要的修炼场"),
            ("公司", "区域", "每日征战之地"),
            ("健身房", "据点", "体魄的锻造炉"),
        ] {
            conn.execute(
                "INSERT INTO MapLocations (Id, Name, LocationType, Description, IsUnlocked, CreatedAt, UpdatedAt)
                 VALUES (?1, ?2, ?3, ?4, 0, ?5, ?5);",
                params![uuid::Uuid::new_v4().to_string(), name, ltype, desc, &now],
            )?;
        }

        // 装备示例（可删）
        let char_id: String = conn.query_row("SELECT Id FROM Characters LIMIT 1;", [], |r| r.get(0))?;
        for (game_name, real_name, cat, effect, bonuses, auto_equip) in [
            (
                "幽邃降噪兜帽",
                "头戴降噪耳机",
                "head",
                "隔绝凡世喧嚣，锁定深度心流",
                vec![("mind", 12)],
                true,
            ),
            (
                "黑铁重装板甲",
                "人体工学电脑椅",
                "chest",
                "支撑脊柱与体魄，持久征战不疲",
                vec![("physique", 15)],
                true,
            ),
            (
                "淬火符文机械键刃",
                "客制化机械键盘",
                "mainhand",
                "指尖敲击如有火星激射，心意直达代码",
                vec![("expression", 16), ("creativity", 8)],
                true,
            ),
            (
                "精金纪律腕轮",
                "机械腕表",
                "wrist",
                "滴答刻画真实时空，令懒散无所遁形",
                vec![("discipline", 10)],
                false,
            ),
            (
                "暗金折射护目镜",
                "防蓝光护目镜",
                "trinket1",
                "过滤杂散炫光，目力长明",
                vec![("mind", 8)],
                false,
            ),
        ] {
            let eq_id = uuid::Uuid::new_v4().to_string();
            conn.execute(
                "INSERT INTO EquipmentDefinitions (Id, RealItemName, GameItemName, Category, Status, RealEffect, CreatedAt, UpdatedAt)
                 VALUES (?1, ?2, ?3, ?4, 'Owned', ?5, ?6, ?6);",
                params![&eq_id, real_name, game_name, cat, effect, &now],
            )?;
            for (attr, val) in bonuses {
                conn.execute(
                    "INSERT INTO EquipmentAttributeBonuses (Id, EquipmentDefinitionId, AttributeKey, BonusValue)
                     VALUES (?1, ?2, ?3, ?4);",
                    params![uuid::Uuid::new_v4().to_string(), &eq_id, attr, val],
                )?;
            }
            if auto_equip {
                conn.execute(
                    "INSERT INTO CharacterEquipment (Id, CharacterId, EquipmentDefinitionId, Status, EquippedAt, CreatedAt, UpdatedAt)
                     VALUES (?1, ?2, ?3, 'Equipped', ?4, ?4, ?4);",
                    params![uuid::Uuid::new_v4().to_string(), &char_id, &eq_id, &now],
                )?;
            }
        }

        tx.commit()?;
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

#[cfg(test)]
mod regression_tests {
    use super::*;
    #[test]
    fn starter_content_does_not_return_after_user_clears_it() {
        let conn=Connection::open_in_memory().unwrap();
        Db::ensure_schema(&conn).unwrap();Db::seed_starter_content(&conn).unwrap();
        let count:i64=conn.query_row("SELECT COUNT(*) FROM SkillDefinitions",[],|r|r.get(0)).unwrap();
        conn.execute("DELETE FROM ActionTasks",[]).unwrap();conn.execute("DELETE FROM LifeRecords",[]).unwrap();
        Db::seed_starter_content(&conn).unwrap();
        assert_eq!(conn.query_row("SELECT COUNT(*) FROM ActionTasks",[],|r|r.get::<_,i64>(0)).unwrap(),0);
        assert_eq!(conn.query_row("SELECT COUNT(*) FROM SkillDefinitions",[],|r|r.get::<_,i64>(0)).unwrap(),count);
    }
    #[test]
    fn candidate_root_is_validated_before_use() {
        let root=std::env::temp_dir().join(format!("liferpg-candidate-{}",uuid::Uuid::new_v4()));
        std::fs::create_dir_all(root.join("data")).unwrap();
        let path=root.join("data/liferpg.db");
        std::fs::write(&path,b"not a database").unwrap();
        assert!(Db::open_at(&root).is_err());
        assert_eq!(std::fs::read(&path).unwrap(),b"not a database");
        let valid=root.join("valid");
        let connection=Db::open_at(&valid).unwrap();
        assert_eq!(connection.query_row("SELECT COUNT(*) FROM ActionTasks",[],|r|r.get::<_,i64>(0)).unwrap(),3);
    }

}
