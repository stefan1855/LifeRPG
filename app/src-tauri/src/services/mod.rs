pub mod action_task_service;
pub mod ai_service;
pub mod backup_service;
pub mod character_service;
pub mod config_service;
pub mod equipment_service;
pub mod export_service;
pub mod image_service;
pub mod image_cleanup_service;
pub mod notification_service;
pub mod mcp_service;
pub mod record_service;
pub mod skill_service;
pub mod stats_service;
pub mod world_service;

#[cfg(test)]
mod tests {
    use super::action_task_service::ActionTaskService;
    use super::backup_service::BackupService;
    use super::character_service::CharacterService;
    use super::export_service::ExportService;
    use super::record_service::RecordService;
    use super::stats_service::StatsService;
    use crate::db::Db;
    use crate::models::{CreateRecordInput, CreateTaskInput, UpdateRecordInput};
    use rusqlite::Connection;

    fn setup_in_memory_db() -> Connection {
        let conn = Connection::open_in_memory().unwrap();
        Db::ensure_schema(&conn).unwrap();
        conn
    }

    #[test]
    fn test_character_initialization() {
        let conn = setup_in_memory_db();
        let character = CharacterService::get_default_character(&conn).unwrap();
        assert_eq!(character.name, "人生主角");
        assert_eq!(character.level, 1);
        assert_eq!(character.total_experience, 0);

        let attrs = CharacterService::get_attributes(&conn, &character.id).unwrap();
        assert_eq!(attrs.len(), 6);
    }

    #[test]
    fn test_task_creation_and_completion_flow() {
        let mut conn = setup_in_memory_db();

        // 1. 创建心智任务 (50 经验)
        let task = ActionTaskService::create_task(&conn, CreateTaskInput {
            title: "学习 Rust 核心原理".to_string(),
            content: Some("精读生命周期与所有权".to_string()),
            category: Some("软件需求".to_string()),
            quadrant: Some("Q1".to_string()),
            growth_attribute: Some("心智".to_string()),
            experience: Some(50),
            due_at: None,
        }).unwrap();

        assert_eq!(task.growth_attribute, "mind");
        assert_eq!(task.experience, 50);
        assert_eq!(task.status, "Pending");

        // 2. 完成任务，发放经验
        let res = CharacterService::apply_task_completion(&mut conn, &task.id).unwrap();
        assert!(res.applied);
        assert!(!res.is_duplicate);
        assert_eq!(res.total_experience_granted, 50);

        let character = CharacterService::get_default_character(&conn).unwrap();
        assert_eq!(character.total_experience, 50);
        assert_eq!(character.level, 1);

        let attrs = CharacterService::get_attributes(&conn, &character.id).unwrap();
        let mind_attr = attrs.iter().find(|a| a.attribute_key == "mind").unwrap();
        assert_eq!(mind_attr.experience, 50);

        // 3. 重复完成（防重幂等）
        let dup_res = CharacterService::apply_task_completion(&mut conn, &task.id).unwrap();
        assert!(!dup_res.applied);
        assert!(dup_res.is_duplicate);
        assert_eq!(dup_res.total_experience_granted, 0);

        // 经验依然是 50，没有被重复叠加
        let character2 = CharacterService::get_default_character(&conn).unwrap();
        assert_eq!(character2.total_experience, 50);

        // 4. 回滚任务
        let rolled_back = CharacterService::rollback_task_completion(&mut conn, &task.id).unwrap();
        assert!(rolled_back);

        let character3 = CharacterService::get_default_character(&conn).unwrap();
        assert_eq!(character3.total_experience, 0);

        let attrs3 = CharacterService::get_attributes(&conn, &character.id).unwrap();
        let mind_attr3 = attrs3.iter().find(|a| a.attribute_key == "mind").unwrap();
        assert_eq!(mind_attr3.experience, 0);
    }

    #[test]
    fn test_record_crud_flow() {
        let conn = setup_in_memory_db();

        let created = RecordService::create(
            &conn,
            CreateRecordInput {
                title: "晨跑 5 公里".into(),
                content: Some("配速 6'10\"".into()),
                category: Some("健康".into()),
                experience: Some(20),
                image_paths: None,
            },
        )
        .unwrap();
        assert_eq!(created.title, "晨跑 5 公里");
        assert_eq!(created.category, "健康");

        let listed = RecordService::list(&conn, None, None, None).unwrap();
        assert_eq!(listed.len(), 1);

        // 关键词过滤命中 / 未命中
        assert_eq!(RecordService::list(&conn, Some("晨跑".into()), None, None).unwrap().len(), 1);
        assert_eq!(RecordService::list(&conn, Some("不存在的词".into()), None, None).unwrap().len(), 0);

        let updated = RecordService::update(
            &conn,
            UpdateRecordInput {
                id: created.id.clone(),
                title: "晨跑 6 公里".into(),
                content: Some("状态不错".into()),
                category: Some("健康".into()),
                experience: Some(30),
                add_image_paths: None,
                remove_image_ids: None,
            },
        )
        .unwrap();
        assert_eq!(updated.title, "晨跑 6 公里");
        assert_eq!(updated.experience, 30);

        assert!(RecordService::delete(&conn, &created.id).unwrap());
        assert_eq!(RecordService::list(&conn, None, None, None).unwrap().len(), 0);
    }

    #[test]
    fn test_archive_stats_aggregation() {
        let mut conn = setup_in_memory_db();

        let task = ActionTaskService::create_task(
            &conn,
            CreateTaskInput {
                title: "写周报".into(),
                content: None,
                category: Some("工作".into()),
                quadrant: Some("Q1".into()),
                growth_attribute: Some("discipline".into()),
                experience: Some(30),
                due_at: None,
            },
        )
        .unwrap();
        CharacterService::apply_task_completion(&mut conn, &task.id).unwrap();

        RecordService::create(
            &conn,
            CreateRecordInput {
                title: "读书 30 分钟".into(),
                content: None,
                category: Some("学习".into()),
                experience: Some(10),
                image_paths: None,
            },
        )
        .unwrap();

        let stats = StatsService::archive_stats(&conn, 30).unwrap();
        assert_eq!(stats.total_tasks, 1);
        assert_eq!(stats.completed_tasks, 1);
        assert_eq!(stats.total_records, 1);
        assert_eq!(stats.total_experience, 40);
        assert_eq!(stats.daily.len(), 30);
        assert!(stats.category_breakdown.iter().any(|c| c.name == "工作"));
    }

    #[test]
    fn test_backup_and_export() {
        let conn = setup_in_memory_db();
        ActionTaskService::create_task(
            &conn,
            CreateTaskInput {
                title: "备份测试任务".into(),
                content: None,
                category: Some("测试".into()),
                quadrant: Some("Q2".into()),
                growth_attribute: Some("mind".into()),
                experience: Some(15),
                due_at: None,
            },
        )
        .unwrap();

        // 备份：落在临时目录，能列出
        let dir = std::env::temp_dir().join(format!("liferpg_test_backup_{}",uuid::Uuid::new_v4()));
        let _ = std::fs::create_dir_all(&dir);
        let info = BackupService::create_at(&conn, &dir, None).unwrap();
        assert!(std::path::Path::new(&info.file_path).exists());
        assert!(info.size_bytes > 0);

        let listed = BackupService::list(Some(dir.to_string_lossy().to_string()));
        assert!(listed.iter().any(|b| b.file_name == info.file_name));

        // 导出 Markdown：文件生成且包含任务标题
        let out = ExportService::export_markdown(&conn, &dir.to_string_lossy()).unwrap();
        let text = std::fs::read_to_string(&out).unwrap();
        assert!(text.contains("备份测试任务"));
        assert!(text.contains("## 行动任务"));

        // Always restore into a separate in-memory database and isolated image folder.
        let mut restored = Connection::open_in_memory().unwrap();
        BackupService::restore_into(&info.file_path, &mut restored, &dir.join("restored-images")).unwrap();
        assert_eq!(restored.query_row("SELECT COUNT(*) FROM ActionTasks", [], |r| r.get::<_, i64>(0)).unwrap(), 1);

    }

    #[test]
    fn test_equipment_equip_and_bonus() {
        let conn = setup_in_memory_db();

        let id = super::equipment_service::EquipmentService::save(
            &conn,
            crate::models::SaveEquipmentInput {
                id: None,
                real_item_name: "降噪耳机".into(),
                game_item_name: "深渊静默之环".into(),
                category: "head".into(),
                real_effect: Some("隔绝噪音，专注 2 小时".into()),
                bonuses: Some(vec![crate::models::EquipmentBonus {
                    attribute_key: "mind".into(),
                    bonus_value: 8,
                }]),
            },
        )
        .unwrap();

        // 未装备时槽位为空
        let snap = super::equipment_service::EquipmentService::snapshot(&conn).unwrap();
        assert_eq!(snap.slots.len(), 12);
        assert!(snap.slots.iter().all(|s| s.item.is_none()));

        // 装备后槽位被填充，加成合计正确
        assert!(super::equipment_service::EquipmentService::toggle(&conn, &id).unwrap());
        let snap2 = super::equipment_service::EquipmentService::snapshot(&conn).unwrap();
        let head = snap2.slots.iter().find(|s| s.key == "head").unwrap();
        assert!(head.item.is_some());
        assert_eq!(snap2.total_bonuses.len(), 1);
        assert_eq!(snap2.total_bonuses[0].bonus_value, 8);

        // 再点一次卸下
        assert!(!super::equipment_service::EquipmentService::toggle(&conn, &id).unwrap());
        let snap3 = super::equipment_service::EquipmentService::snapshot(&conn).unwrap();
        assert!(snap3.slots.iter().all(|s| s.item.is_none()));
    }

    #[test]
    fn test_skill_unlock_and_level_up() {
        let conn = setup_in_memory_db();

        let id = super::skill_service::SkillService::save(
            &conn,
            crate::models::SaveSkillInput {
                id: None,
                name: "深度工作".into(),
                category: "专业".into(),
                description: Some("连续专注 90 分钟".into()),
            },
        )
        .unwrap();

        let list = super::skill_service::SkillService::list(&conn).unwrap();
        assert_eq!(list.len(), 1);
        assert!(!list[0].is_unlocked);

        super::skill_service::SkillService::unlock(&conn, &id).unwrap();
        let after = super::skill_service::SkillService::list(&conn).unwrap();
        assert!(after[0].is_unlocked);
        assert_eq!(after[0].level, 1);

        // 加 120 经验 → 升 1 级，余 20
        let updated = super::skill_service::SkillService::add_experience(&conn, &id, 120).unwrap();
        assert_eq!(updated.level, 2);
        assert_eq!(updated.experience, 20);
    }

    #[test]
    fn test_npc_relation_and_map_unlock() {
        let conn = setup_in_memory_db();

        let npc_id = super::world_service::WorldService::npc_save(
            &conn,
            crate::models::SaveNpcInput {
                id: None,
                name: "老周".into(),
                real_identity: Some("直属上级".into()),
                category: Some("同事".into()),
                influence_type: Some("助力".into()),
                linked_domain: Some("工作".into()),
            },
        )
        .unwrap();

        assert_eq!(
            super::world_service::WorldService::npc_adjust_relationship(&conn, &npc_id, 30).unwrap(),
            30
        );
        assert_eq!(
            super::world_service::WorldService::npc_adjust_relationship(&conn, &npc_id, -10).unwrap(),
            20
        );
        // 上限截断
        assert_eq!(
            super::world_service::WorldService::npc_adjust_relationship(&conn, &npc_id, 500).unwrap(),
            100
        );

        let npcs = super::world_service::WorldService::npc_list(&conn).unwrap();
        assert_eq!(npcs.len(), 1);
        assert_eq!(npcs[0].relationship_value, 100);

        let place_id = super::world_service::WorldService::map_save(
            &conn,
            crate::models::SaveMapInput {
                id: None,
                name: "书房".into(),
                location_type: Some("据点".into()),
                description: Some("主要修炼场".into()),
            },
        )
        .unwrap();
        let places = super::world_service::WorldService::map_list(&conn).unwrap();
        assert!(!places[0].is_unlocked);
        assert!(super::world_service::WorldService::map_toggle(&conn, &place_id).unwrap());
        let places2 = super::world_service::WorldService::map_list(&conn).unwrap();
        assert!(places2[0].is_unlocked);
    }

    #[test]
    fn test_mcp_config_and_tool_aggregation() {
        use crate::models::McpServerConfig;
        use crate::services::mcp_service::McpService;
        use std::collections::HashMap;

        let server = McpServerConfig {
            id: "test-mcp".into(),
            name: "MockMCP".into(),
            command: "cmd.exe".into(),
            args: vec!["/c".into(), "echo invalid response".into()],
            env: HashMap::new(),
            enabled: true,
        };

        // 验证结构体正常序列化与默认值
        let json_val = serde_json::to_string(&server).unwrap();
        assert!(json_val.contains("test-mcp"));

        // 验证聚合函数在服务器不可用时不 panic，返回空列表优雅降级
        let all_tools = McpService::list_all_enabled_tools(&[server]);
        assert_eq!(all_tools.len(), 0);

        // 验证带空格与引号的参数切分器
        let split = McpService::split_args(r#""D:\My Projects\mcp server\server.py" --port 8080 'hello world'"#);
        assert_eq!(
            split,
            vec![
                r"D:\My Projects\mcp server\server.py".to_string(),
                "--port".to_string(),
                "8080".to_string(),
                "hello world".to_string()
            ]
        );
    }

    #[test]
    fn test_config_save_preserves_existing_fields() {
        use super::config_service::{ConfigService, WidgetSettings};
        use crate::models::McpServerConfig;
        use std::collections::HashMap;

        let initial = WidgetSettings {
            data_dir: Some("D:\\LifeRPG-Data".into()),
            opacity: Some(97),
            nav_expanded: Some(false),
            hotkey: Some("CommandOrControl+Shift+L".into()),
            notification_enabled: Some(true),
            is_always_on_top: Some(true),
            view_mode: Some("widget".into()),
            autostart: Some(false),
            ai_enabled: Some(true),
            ai_base_url: Some("http://127.0.0.1:8317/v1".into()),
            ai_model: Some("deepseek-v4.1-flash".into()),
            ai_model_light: Some("glm-5.3-flash".into()),
            ai_api_key: Some("sk-secret-key".into()),
            obsidian_vault: Some("D:\\F\\Obsidian".into()),
            mcp_servers: Some(vec![McpServerConfig {
                id: "mcp-1".into(),
                name: "腾讯会议".into(),
                command: "python".into(),
                args: vec!["server.py".into()],
                env: HashMap::new(),
                enabled: true,
            }]),
        };

        // 模拟前端只传 3 个窗口字段（保存设置）
        let patch = serde_json::json!({
            "hotkey": "CommandOrControl+Alt+L",
            "notificationEnabled": false,
            "opacity": 85
        });

        let merged = ConfigService::merge_with_patch(initial, &patch);
        assert_eq!(merged.data_dir.as_deref(), Some("D:\\LifeRPG-Data"), "dataDir 绝不能丢失");
        assert_eq!(merged.ai_api_key.as_deref(), Some("sk-secret-key"), "aiApiKey 绝不能丢失");
        assert_eq!(merged.mcp_servers.as_ref().map(|v| v.len()), Some(1), "mcpServers 绝不能被清空");
        assert_eq!(merged.opacity, Some(85));
        assert_eq!(merged.notification_enabled, Some(false));
        assert_eq!(merged.hotkey.as_deref(), Some("CommandOrControl+Alt+L"));
    }

    #[test]
    fn test_record_experience_grants_and_updates_character_exp() {
        let conn = setup_in_memory_db();
        let rec = RecordService::create(
            &conn,
            CreateRecordInput {
                title: "完成深度架构评审".into(),
                content: Some("拉网排查 9 项缺陷".into()),
                category: Some("研发".into()),
                experience: Some(45),
                image_paths: None,
            },
        )
        .unwrap();

        let c1 = CharacterService::get_default_character(&conn).unwrap();
        assert_eq!(c1.total_experience, 45);

        // 修改记录经验为 120 -> 角色总经验应同步变为 120，等级升至 Lv.2
        RecordService::update(
            &conn,
            UpdateRecordInput {
                id: rec.id.clone(),
                title: "完成深度架构评审（含修复）".into(),
                content: None,
                category: None,
                experience: Some(120),
                add_image_paths: None,
                remove_image_ids: None,
            },
        )
        .unwrap();

        let c2 = CharacterService::get_default_character(&conn).unwrap();
        assert_eq!(c2.total_experience, 120);
        assert_eq!(c2.level, 2);

        // 删除记录 -> 经验应回滚归零，等级回到 Lv.1
        RecordService::delete(&conn, &rec.id).unwrap();
        let c3 = CharacterService::get_default_character(&conn).unwrap();
        assert_eq!(c3.total_experience, 0);
        assert_eq!(c3.level, 1);
    }

    #[test]
    fn test_equipment_bonus_reflected_in_character_attributes() {
        let conn = setup_in_memory_db();
        let eq_id = super::equipment_service::EquipmentService::save(
            &conn,
            crate::models::SaveEquipmentInput {
                id: None,
                real_item_name: "人体工学椅".into(),
                game_item_name: "黑铁重装王座".into(),
                category: "chest".into(),
                real_effect: Some("久坐不疲".into()),
                bonuses: Some(vec![crate::models::EquipmentBonus {
                    attribute_key: "physique".into(),
                    bonus_value: 15,
                }]),
            },
        )
        .unwrap();

        // 穿戴装备
        assert!(super::equipment_service::EquipmentService::toggle(&conn, &eq_id).unwrap());

        let c = CharacterService::get_default_character(&conn).unwrap();
        let attrs = CharacterService::get_attributes(&conn, &c.id).unwrap();
        let physique = attrs.iter().find(|a| a.attribute_key == "physique").unwrap();
        assert_eq!(physique.base_value, 15, "穿戴后体魄属性应包含 +15 装备加成");

        // 卸下装备 -> 加成归零
        assert!(!super::equipment_service::EquipmentService::toggle(&conn, &eq_id).unwrap());
        let attrs2 = CharacterService::get_attributes(&conn, &c.id).unwrap();
        let physique2 = attrs2.iter().find(|a| a.attribute_key == "physique").unwrap();
        assert_eq!(physique2.base_value, 0);
    }

    #[test]
    fn test_character_profile_update() {
        let conn = setup_in_memory_db();
        let updated = CharacterService::update_character(&conn, "柳贯一", "宁在直中取，不向曲中求").unwrap();
        assert_eq!(updated.name, "柳贯一");
        assert_eq!(updated.description.as_deref(), Some("宁在直中取，不向曲中求"));
    }
    #[test]
    fn record_failure_rolls_back_all_database_changes() {
        let conn = setup_in_memory_db();
        let result = RecordService::create(&conn, CreateRecordInput {
            title: "failed import".into(), content: None, category: None,
            experience: Some(50), image_paths: Some(vec!["missing-image-for-regression.jpg".into()]),
        });
        assert!(result.is_err());
        assert_eq!(conn.query_row("SELECT COUNT(*) FROM LifeRecords", [], |r| r.get::<_,i64>(0)).unwrap(),0);
        assert_eq!(CharacterService::get_default_character(&conn).unwrap().total_experience,0);
    }

    #[test]
    fn record_attribute_migration_and_event_date_are_stable() {
        let conn = setup_in_memory_db();
        let rec = RecordService::create(&conn, CreateRecordInput { title:"old record".into(),content:None,category:None,experience:Some(45),image_paths:None }).unwrap();
        assert_eq!(conn.query_row("SELECT Experience FROM CharacterAttributes WHERE AttributeKey='mind'",[],|r|r.get::<_,i32>(0)).unwrap(),45);
        conn.execute("UPDATE CharacterAttributes SET Experience=0 WHERE AttributeKey='mind'",[]).unwrap();
        conn.execute("DELETE FROM AppMetadata WHERE Key='record_attribute_v1'",[]).unwrap();
        Db::ensure_schema(&conn).unwrap();
        Db::ensure_schema(&conn).unwrap();
        assert_eq!(conn.query_row("SELECT Experience FROM CharacterAttributes WHERE AttributeKey='mind'",[],|r|r.get::<_,i32>(0)).unwrap(),45);
        conn.execute("UPDATE LifeRecords SET CreatedAt='2025-01-02T12:00:00+08:00' WHERE Id=?1",[&rec.id]).unwrap();
        RecordService::update(&conn,UpdateRecordInput { id:rec.id.clone(),title:"edited".into(),content:None,category:None,experience:Some(80),add_image_paths:None,remove_image_ids:None }).unwrap();
        assert_eq!(conn.query_row("SELECT CreatedAt FROM RpgExperienceEvents WHERE SourceId=?1",[&rec.id],|r|r.get::<_,String>(0)).unwrap(),"2025-01-02T12:00:00+08:00");
        assert_eq!(conn.query_row("SELECT Experience FROM CharacterAttributes WHERE AttributeKey='mind'",[],|r|r.get::<_,i32>(0)).unwrap(),80);
        RecordService::delete(&conn,&rec.id).unwrap();
        assert_eq!(conn.query_row("SELECT Experience FROM CharacterAttributes WHERE AttributeKey='mind'",[],|r|r.get::<_,i32>(0)).unwrap(),0);
    }

    #[test]
    fn restore_rejects_traversal_and_corruption_without_changing_destination() {
        use std::io::Write;
        let dir = std::env::temp_dir().join(format!("liferpg-restore-test-{}",uuid::Uuid::new_v4()));
        std::fs::create_dir_all(&dir).unwrap();
        for (file_name,entry_name,bytes) in [("traversal.zip","images/../../escape.txt",b"bad".as_slice()),("corrupt.zip","liferpg.db",b"not a database".as_slice())] {
            let path = dir.join(file_name);
            let mut zip = zip::ZipWriter::new(std::fs::File::create(&path).unwrap());
            zip.start_file(entry_name,zip::write::SimpleFileOptions::default()).unwrap();
            zip.write_all(bytes).unwrap();zip.finish().unwrap();
            let mut conn = setup_in_memory_db();
            assert!(BackupService::restore_into(path.to_str().unwrap(),&mut conn,&dir.join("images")).is_err());
            assert_eq!(CharacterService::get_default_character(&conn).unwrap().total_experience,0);
            assert!(!dir.join("escape.txt").exists());
        }
    }

    #[test]
    fn record_pages_do_not_overlap() {
        let conn = setup_in_memory_db();
        for n in 0..4 { RecordService::create(&conn,CreateRecordInput { title:format!("record {}",n),content:None,category:None,experience:None,image_paths:None }).unwrap(); }
        let first=RecordService::list_page(&conn,None,None,Some(2),0).unwrap();
        let second=RecordService::list_page(&conn,None,None,Some(2),2).unwrap();
        assert_eq!(first.len(),2);assert_eq!(second.len(),2);
        assert!(first.iter().all(|a| second.iter().all(|b| a.id != b.id)));
    }

    #[test]
    fn deleting_completed_task_rolls_back_character_and_attributes() {
        let mut conn=setup_in_memory_db();
        let task=ActionTaskService::create_task(&conn,CreateTaskInput { title:"delete completed".into(),content:None,category:None,quadrant:Some("Q2".into()),growth_attribute:Some("mind".into()),experience:Some(50),due_at:None }).unwrap();
        CharacterService::apply_task_completion(&mut conn,&task.id).unwrap();
        ActionTaskService::delete_task(&conn,&task.id).unwrap();
        assert_eq!(CharacterService::get_default_character(&conn).unwrap().total_experience,0);
        assert_eq!(conn.query_row("SELECT Experience FROM CharacterAttributes WHERE AttributeKey='mind'",[],|r|r.get::<_,i32>(0)).unwrap(),0);
        assert_eq!(conn.query_row("SELECT COUNT(*) FROM RpgExperienceEvents",[],|r|r.get::<_,i64>(0)).unwrap(),0);
    }

    #[test]
    fn deadlines_with_different_offsets_compare_as_instants() {
        let conn=setup_in_memory_db();
        let overdue:i32=conn.query_row("SELECT julianday('2026-10-30T15:59:59.000Z') < julianday('2026-10-30T20:00:00+08:00')",[],|r|r.get(0)).unwrap();
        assert_eq!(overdue,0);
    }

    #[test]
    fn restore_rejects_incomplete_schema_and_broken_relations() {
        let dir=std::env::temp_dir().join(format!("liferpg-restore-integrity-{}",uuid::Uuid::new_v4()));
        std::fs::create_dir_all(&dir).unwrap();
        let incomplete=Connection::open_in_memory().unwrap();
        incomplete.execute_batch("CREATE TABLE Characters(Id TEXT); CREATE TABLE ActionTasks(Id TEXT); CREATE TABLE LifeRecords(Id TEXT); CREATE TABLE LifeRecordImages(Id TEXT, FilePath TEXT);").unwrap();
        fn raw_archive(conn:&Connection,dir:&std::path::Path)->String {
            use std::io::Write;
            let id=uuid::Uuid::new_v4();let db=dir.join(format!("{}.db",id));conn.backup(rusqlite::DatabaseName::Main,&db,None).unwrap();
            let path=dir.join(format!("{}.zip",id));let mut zip=zip::ZipWriter::new(std::fs::File::create(&path).unwrap());zip.start_file("liferpg.db",zip::write::SimpleFileOptions::default()).unwrap();zip.write_all(&std::fs::read(db).unwrap()).unwrap();zip.finish().unwrap();path.to_string_lossy().into()
        }
        assert!(BackupService::create_at(&incomplete,&dir,None).is_err());
        let archive=raw_archive(&incomplete,&dir);
        let mut destination=setup_in_memory_db();
        assert!(BackupService::restore_into(&archive,&mut destination,&dir.join("images")).is_err());
        assert_eq!(CharacterService::get_default_character(&destination).unwrap().total_experience,0);
        let broken=setup_in_memory_db();
        broken.execute_batch("PRAGMA foreign_keys=OFF;").unwrap();
        broken.execute("INSERT INTO LifeRecordImages(Id,LifeRecordId,FileName,FilePath,CreatedAt) VALUES ('bad','missing','a.jpg','a.jpg','2026-01-01')",[]).unwrap();
        assert!(BackupService::create_at(&broken,&dir,None).is_err());
        let archive=raw_archive(&broken,&dir);
        let error=BackupService::restore_into(&archive,&mut destination,&dir.join("images")).unwrap_err();
        assert!(error.contains("断裂"),"{}",error);
        assert_eq!(CharacterService::get_default_character(&destination).unwrap().total_experience,0);
    }

    #[test]
    fn backup_missing_reference_never_publishes_zip_and_case_names_restore(){
        let dir=std::env::temp_dir().join(format!("liferpg-backup-reference-{}",uuid::Uuid::new_v4()));
        assert!(!dir.to_string_lossy().to_ascii_lowercase().starts_with("c:"),"Load build-env.ps1 first");
        let images=dir.join("images");std::fs::create_dir_all(&images).unwrap();let conn=setup_in_memory_db();
        let record=RecordService::create(&conn,CreateRecordInput{title:"case".into(),content:None,category:None,experience:Some(0),image_paths:None}).unwrap();
        conn.execute("INSERT INTO LifeRecordImages(Id,LifeRecordId,FileName,FilePath,CreatedAt) VALUES('image',?1,'PHOTO.jpg','PHOTO.jpg','2026-01-01')",[record.id]).unwrap();
        std::fs::write(images.join("photo.jpg"),b"image-bytes").unwrap();
        let archive=BackupService::create_at(&conn,&dir,Some(&images)).unwrap();
        std::fs::remove_file(images.join("photo.jpg")).unwrap();assert!(BackupService::create_at(&conn,&dir,Some(&images)).is_err());
        assert_eq!(std::fs::read_dir(&dir).unwrap().filter_map(Result::ok).filter(|e|e.path().extension().map(|x|x=="zip").unwrap_or(false)).count(),1);
        let mut destination=setup_in_memory_db();let restored_images=dir.join("restored-images");BackupService::restore_into(&archive.file_path,&mut destination,&restored_images).unwrap();
        let path:String=destination.query_row("SELECT FilePath FROM LifeRecordImages",[],|r|r.get(0)).unwrap();assert_eq!(std::fs::read(restored_images.join(path)).unwrap(),b"image-bytes");
        assert_eq!(dir.parent(),Some(std::env::temp_dir().as_path()));std::fs::remove_dir_all(dir).unwrap();
    }

}
