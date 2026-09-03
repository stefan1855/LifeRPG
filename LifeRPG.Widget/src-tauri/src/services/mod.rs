pub mod action_task_service;
pub mod character_service;
pub mod config_service;

#[cfg(test)]
mod tests {
    use super::action_task_service::ActionTaskService;
    use super::character_service::CharacterService;
    use crate::db::Db;
    use crate::models::CreateTaskInput;
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
}
