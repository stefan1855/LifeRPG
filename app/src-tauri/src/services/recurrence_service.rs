use chrono::{DateTime, Datelike, Duration, FixedOffset, Months};
use rusqlite::{params, Connection};

pub struct RecurrenceService;
impl RecurrenceService {
    pub fn schema(conn: &Connection) -> rusqlite::Result<()> {
        let columns: Vec<String> = conn.prepare("PRAGMA table_info(ActionTasks)")?
            .query_map([], |r| r.get(1))?.collect::<rusqlite::Result<_>>()?;
        for (name, sql) in [
            ("RepeatRule", "ALTER TABLE ActionTasks ADD COLUMN RepeatRule TEXT NOT NULL DEFAULT 'none'"),
            ("RepeatAnchorAt", "ALTER TABLE ActionTasks ADD COLUMN RepeatAnchorAt TEXT"),
            ("RepeatFromTaskId", "ALTER TABLE ActionTasks ADD COLUMN RepeatFromTaskId TEXT"),
        ] { if !columns.iter().any(|c| c == name) { conn.execute_batch(sql)?; } }
        conn.execute_batch("CREATE UNIQUE INDEX IF NOT EXISTS IX_Task_RepeatFrom ON ActionTasks(RepeatFromTaskId) WHERE RepeatFromTaskId IS NOT NULL;
            CREATE TABLE IF NOT EXISTS TaskMeetings(TaskId TEXT PRIMARY KEY REFERENCES ActionTasks(Id) ON DELETE CASCADE, State TEXT NOT NULL, Payload TEXT NOT NULL DEFAULT '{}', Error TEXT, UpdatedAt TEXT NOT NULL);")
    }

    pub fn validate(rule: &str, due: Option<&str>) -> rusqlite::Result<()> {
        if !["none", "daily", "weekly", "monthly"].contains(&rule) {
            return Err(rusqlite::Error::InvalidParameterName("不支持的循环规则".into()));
        }
        if rule != "none" && due.and_then(|v| DateTime::parse_from_rfc3339(v).ok()).is_none() {
            return Err(rusqlite::Error::InvalidParameterName("循环任务必须设置有效的首次提醒时间".into()));
        }
        Ok(())
    }

    fn index(anchor: DateTime<FixedOffset>, at: DateTime<FixedOffset>, rule: &str) -> i64 {
        let at = at.with_timezone(anchor.offset());
        match rule {
            "monthly" => ((at.year() - anchor.year()) as i64 * 12 + at.month() as i64 - anchor.month() as i64).max(0),
            "weekly" => (at.date_naive() - anchor.date_naive()).num_days().max(0) / 7,
            _ => (at.date_naive() - anchor.date_naive()).num_days().max(0),
        }
    }
    fn at(anchor: DateTime<FixedOffset>, rule: &str, index: i64) -> Option<DateTime<FixedOffset>> {
        if rule == "monthly" {
            anchor.checked_add_months(Months::new(u32::try_from(index).ok()?))
        } else {
            anchor.checked_add_signed(Duration::try_days(index.checked_mul(if rule == "weekly" { 7 } else { 1 })?)?)
        }
    }
    pub fn latest(anchor: DateTime<FixedOffset>, rule: &str, now: DateTime<FixedOffset>) -> Option<DateTime<FixedOffset>> {
        if now < anchor { return None; }
        if rule == "none" { return Some(anchor); }
        let mut i = Self::index(anchor, now, rule);
        let mut date = Self::at(anchor, rule, i)?;
        if date > now { i -= 1; date = Self::at(anchor, rule, i)?; }
        Some(date)
    }
    pub fn next(anchor: DateTime<FixedOffset>, rule: &str, after: DateTime<FixedOffset>) -> Option<DateTime<FixedOffset>> {
        if rule == "none" { return None; }
        let mut i = Self::index(anchor, after, rule);
        let mut date = Self::at(anchor, rule, i)?;
        if date <= after { i += 1; date = Self::at(anchor, rule, i)?; }
        Some(date)
    }

    // Runs inside the same transaction as settlement. One successor per completed task.
    pub fn on_complete(conn: &Connection, id: &str, now: &str) -> rusqlite::Result<()> {
        let (rule, due, anchor): (String, Option<String>, Option<String>) = conn.query_row(
            "SELECT RepeatRule,DueAt,RepeatAnchorAt FROM ActionTasks WHERE Id=?1", [id], |r| Ok((r.get(0)?,r.get(1)?,r.get(2)?)))?;
        if rule == "none" { return Ok(()); }
        let invalid = || rusqlite::Error::InvalidParameterName("循环任务日期无效，请先修正提醒时间".into());
        let due = DateTime::parse_from_rfc3339(due.as_deref().ok_or_else(invalid)?).map_err(|_| invalid())?;
        let anchor = DateTime::parse_from_rfc3339(anchor.as_deref().unwrap_or(&due.to_rfc3339())).map_err(|_| invalid())?;
        let now_date = DateTime::parse_from_rfc3339(now).map_err(|_| invalid())?;
        let next = Self::next(anchor, &rule, due.max(now_date)).ok_or_else(invalid)?.to_rfc3339();
        conn.execute("INSERT OR IGNORE INTO ActionTasks(Id,Title,Content,Category,Status,Quadrant,GrowthAttribute,Experience,DueAt,CompletedAt,CompletionNote,LinkedLifeRecordId,CreatedAt,UpdatedAt,RepeatRule,RepeatAnchorAt,RepeatFromTaskId)
            SELECT ?1,Title,Content,Category,'Pending',Quadrant,GrowthAttribute,Experience,?2,NULL,'',NULL,?3,?3,RepeatRule,COALESCE(RepeatAnchorAt,DueAt),Id FROM ActionTasks WHERE Id=?4",
            params![uuid::Uuid::new_v4().to_string(),next,now,id])?;
        Ok(())
    }

    pub fn before_rollback(conn: &Connection, id: &str) -> rusqlite::Result<()> {
        use rusqlite::OptionalExtension;
        let child: Option<(String,String,String,String)> = conn.query_row(
            "SELECT Id,Status,CreatedAt,UpdatedAt FROM ActionTasks WHERE RepeatFromTaskId=?1", [id],
            |r| Ok((r.get(0)?,r.get(1)?,r.get(2)?,r.get(3)?))).optional()?;
        if let Some((child,status,created,updated)) = child {
            let meeting: bool = conn.query_row("SELECT EXISTS(SELECT 1 FROM TaskMeetings WHERE TaskId=?1)", [&child], |r| r.get(0))?;
            if status != "Pending" || created != updated || meeting {
                return Err(rusqlite::Error::InvalidParameterName("下一次循环任务已被修改、完成或关联会议，请先处理该任务，再撤销本次完成".into()));
            }
            conn.execute("DELETE FROM ActionTasks WHERE Id=?1", [&child])?;
        }
        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    fn dt(s: &str) -> DateTime<FixedOffset> { DateTime::parse_from_rfc3339(s).unwrap() }
    #[test]
    fn calendar_month_end_keeps_original_day_and_timezone() {
        let anchor=dt("2024-01-31T09:30:00+08:00");
        assert_eq!(RecurrenceService::next(anchor,"monthly",anchor).unwrap(),dt("2024-02-29T09:30:00+08:00"));
        assert_eq!(RecurrenceService::next(anchor,"monthly",dt("2024-02-29T09:30:00+08:00")).unwrap(),dt("2024-03-31T09:30:00+08:00"));
        assert_eq!(RecurrenceService::latest(anchor,"daily",dt("2024-02-03T08:00:00+08:00")).unwrap(),dt("2024-02-02T09:30:00+08:00"));
        assert_eq!(RecurrenceService::next(anchor,"weekly",anchor).unwrap(),dt("2024-02-07T09:30:00+08:00"));
    }
    #[test]
    fn invalid_rules_and_missing_date_are_rejected() {
        assert!(RecurrenceService::validate("daily",None).is_err());
        assert!(RecurrenceService::validate("cron",Some("nonsense")).is_err());
        assert!(RecurrenceService::validate("none",None).is_ok());
    }
    #[test]
    fn settlement_undo_and_redo_keep_single_successor() {
        use crate::{db::Db, models::CreateTaskInput,services::{action_task_service::ActionTaskService,character_service::CharacterService}};
        let mut c=Connection::open_in_memory().unwrap();Db::ensure_schema(&c).unwrap();
        let t=ActionTaskService::create_task(&c,serde_json::from_value::<CreateTaskInput>(serde_json::json!({"title":"daily","dueAt":"2020-01-01T09:00:00+08:00","repeatRule":"daily","growthAttribute":"mind","experience":25})).unwrap()).unwrap();
        CharacterService::apply_task_completion(&mut c,&t.id).unwrap();
        CharacterService::apply_task_completion(&mut c,&t.id).unwrap();
        assert_eq!(c.query_row("SELECT COUNT(*) FROM ActionTasks WHERE RepeatFromTaskId=?1",[&t.id],|r|r.get::<_,i32>(0)).unwrap(),1);
        CharacterService::rollback_task_completion(&c,&t.id).unwrap();
        assert_eq!(ActionTaskService::list_tasks(&c,None,None).unwrap().len(),1);
        CharacterService::apply_task_completion(&mut c,&t.id).unwrap();
        c.execute("UPDATE ActionTasks SET UpdatedAt='changed' WHERE RepeatFromTaskId=?1",[&t.id]).unwrap();
        assert!(CharacterService::rollback_task_completion(&c,&t.id).is_err());
        assert_eq!(c.query_row("SELECT COUNT(*) FROM RpgExperienceEvents WHERE SourceId=?1",[&t.id],|r|r.get::<_,i32>(0)).unwrap(),1);
    }
}
