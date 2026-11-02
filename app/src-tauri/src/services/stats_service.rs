use rusqlite::Connection;

use crate::models::{ArchiveStats, CategoryStat, DailyActivity};

/// 归档与统计服务
pub struct StatsService;

impl StatsService {
    pub fn archive_stats(conn: &Connection, days: i64) -> rusqlite::Result<ArchiveStats> {
        let total_tasks: i32 =
            conn.query_row("SELECT COUNT(*) FROM ActionTasks", [], |r| r.get(0))?;
        let completed_tasks: i32 = conn.query_row(
            "SELECT COUNT(*) FROM ActionTasks WHERE Status = 'Completed'",
            [],
            |r| r.get(0),
        )?;
        let pending_tasks: i32 = conn.query_row(
            "SELECT COUNT(*) FROM ActionTasks WHERE Status <> 'Completed'",
            [],
            |r| r.get(0),
        )?;
        let overdue_tasks: i32 = conn.query_row(
            "SELECT COUNT(*) FROM ActionTasks
             WHERE Status <> 'Completed' AND DueAt IS NOT NULL AND julianday(DueAt) < julianday(?1)",
            rusqlite::params![chrono::Local::now().to_rfc3339()],
            |r| r.get(0),
        )?;
        let total_records: i32 =
            conn.query_row("SELECT COUNT(*) FROM LifeRecords", [], |r| r.get(0))?;
        let total_images: i32 =
            conn.query_row("SELECT COUNT(*) FROM LifeRecordImages", [], |r| r.get(0))?;
        let total_experience: i32 =
            conn.query_row("SELECT TotalExperience FROM Characters LIMIT 1", [], |r| r.get(0))?;

        // 每日活跃度
        let mut daily = Vec::new();
        let mut streak_days = 0;
        let today = chrono::Local::now().date_naive();
        for offset in (0..days.clamp(1, 3660)).rev() {
            let day = today - chrono::Duration::days(offset);
            let prefix = day.format("%Y-%m-%d").to_string();
            let like = format!("{}%", prefix);
            let task_count: i32 = conn.query_row(
                "SELECT COUNT(*) FROM ActionTasks WHERE date(CompletedAt, 'localtime') = substr(?1,1,10)",
                rusqlite::params![&like],
                |r| r.get(0),
            )?;
            let record_count: i32 = conn.query_row(
                "SELECT COUNT(*) FROM LifeRecords WHERE date(CreatedAt, 'localtime') = substr(?1,1,10)",
                rusqlite::params![&like],
                |r| r.get(0),
            )?;
            let experience: i32 = conn
                .query_row(
                    "SELECT COALESCE(SUM(TotalExperience), 0) FROM RpgExperienceEvents WHERE date(CreatedAt, 'localtime') = substr(?1,1,10)",
                    rusqlite::params![&like],
                    |r| r.get(0),
                )
                .unwrap_or(0);
            if offset == 0 || task_count + record_count > 0 {
                // 连续天数从今天往前算
            }
            daily.push(DailyActivity {
                date: prefix,
                task_count,
                record_count,
                experience,
            });
        }
        let mut stmt=conn.prepare("SELECT DISTINCT date(CreatedAt,'localtime') FROM LifeRecords UNION SELECT DISTINCT date(CompletedAt,'localtime') FROM ActionTasks WHERE Status='Completed'")?;
        let active=stmt.query_map([],|r|r.get::<_,Option<String>>(0))?.collect::<rusqlite::Result<Vec<_>>>()?.into_iter().flatten().collect::<std::collections::HashSet<_>>();
        let mut cursor=today;
        if !active.contains(&cursor.to_string()){cursor-=chrono::Duration::days(1);}
        while active.contains(&cursor.to_string()){streak_days+=1;cursor-=chrono::Duration::days(1);}

        // 分类分布（以任务分类为主）
        let mut stmt = conn.prepare(
            "SELECT Category, COUNT(*), COALESCE(SUM(Experience), 0)
             FROM ActionTasks GROUP BY Category ORDER BY COUNT(*) DESC",
        )?;
        let category_breakdown = stmt
            .query_map([], |row| {
                Ok(CategoryStat {
                    name: row.get(0)?,
                    count: row.get(1)?,
                    experience: row.get(2)?,
                })
            })?
            .collect::<rusqlite::Result<Vec<_>>>()?;

        Ok(ArchiveStats {
            total_tasks,
            completed_tasks,
            pending_tasks,
            overdue_tasks,
            total_records,
            total_images,
            total_experience,
            streak_days,
            daily,
            category_breakdown,
        })
    }
}
#[cfg(test)]
mod rc5_stats_tests{
 use super::*;
 #[test]
 fn streak_is_not_limited_to_chart_window(){
  let conn=Connection::open_in_memory().unwrap();crate::db::Db::ensure_schema(&conn).unwrap();
  for day in 0..60 {let date=(chrono::Local::now()-chrono::Duration::days(day)).to_rfc3339();conn.execute("INSERT INTO LifeRecords(Id,Title,Content,Category,Experience,CreatedAt,UpdatedAt)VALUES(?1,'active','','test',0,?2,?2)",rusqlite::params![day.to_string(),date]).unwrap();}
  let s=StatsService::archive_stats(&conn,30).unwrap();assert_eq!(s.streak_days,60);assert_eq!(s.daily.len(),30);
 }
}
