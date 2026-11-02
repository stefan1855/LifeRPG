use rusqlite::{Connection,params};
use serde::Serialize;

#[derive(Debug,Default,Serialize)]
#[serde(rename_all="camelCase")]
pub struct Dispatch {pub submitted:usize,pub failed:usize,pub last_error:Option<String>}
#[derive(Debug,Serialize)]
#[serde(rename_all="camelCase")]
pub struct NotificationStatus {pub enabled:bool,pub channel:String,pub available:bool,pub error:Option<String>,pub last_submitted:Option<String>,pub last_error:Option<String>}
pub struct NotificationService;
impl NotificationService {
    pub fn schema(conn:&Connection)->Result<(),String>{
        conn.execute_batch("CREATE TABLE IF NOT EXISTS DueNotifications(TaskId TEXT NOT NULL,DueAt TEXT NOT NULL,PRIMARY KEY(TaskId,DueAt));
            CREATE TABLE IF NOT EXISTS NotificationAttempts(TaskId TEXT NOT NULL,DueAt TEXT NOT NULL,LastAttemptAt TEXT NOT NULL,AttemptCount INTEGER NOT NULL DEFAULT 0,LastError TEXT,PRIMARY KEY(TaskId,DueAt));").map_err(|e|e.to_string())
    }
    pub fn dispatch<F:FnMut(&str,&str)->Result<(),String>>(conn:&Connection,mut send:F)->Result<Dispatch,String>{
        Self::schema(conn)?;
        let tx=conn.unchecked_transaction().map_err(|e|e.to_string())?;
        tx.execute("UPDATE AppMetadata SET Value=Value WHERE Key='notification_lock'",[]).map_err(|e|e.to_string())?;
        let tasks:Vec<(String,String,String)>={
            let mut stmt=tx.prepare("SELECT t.Id,t.Title,t.DueAt FROM ActionTasks t
                LEFT JOIN NotificationAttempts a ON a.TaskId=t.Id AND a.DueAt=t.DueAt
                WHERE t.Status='Pending' AND julianday(t.DueAt)<=julianday('now')
                AND NOT EXISTS(SELECT 1 FROM DueNotifications n WHERE n.TaskId=t.Id AND n.DueAt=t.DueAt)
                AND (a.LastAttemptAt IS NULL OR julianday(a.LastAttemptAt)<=julianday('now','-60 seconds'))
                ORDER BY julianday(t.DueAt),t.Id LIMIT 3").map_err(|e|e.to_string())?;
            let rows=stmt.query_map([],|r|Ok((r.get(0)?,r.get(1)?,r.get(2)?))).map_err(|e|e.to_string())?;
            rows.collect::<rusqlite::Result<_>>().map_err(|e|e.to_string())?
        };
        let mut result=Dispatch::default();
        for (id,title,due) in tasks {
            let error=send(&id,&title).err();
            tx.execute("INSERT INTO NotificationAttempts(TaskId,DueAt,LastAttemptAt,AttemptCount,LastError) VALUES(?1,?2,?3,1,?4)
                ON CONFLICT(TaskId,DueAt) DO UPDATE SET LastAttemptAt=excluded.LastAttemptAt,AttemptCount=AttemptCount+1,LastError=excluded.LastError",
                params![id,due,chrono::Local::now().to_rfc3339(),error]).map_err(|e|e.to_string())?;
            if let Some(error)=error {result.failed+=1;result.last_error=Some(error);} else {
                tx.execute("INSERT OR IGNORE INTO DueNotifications(TaskId,DueAt) VALUES(?1,?2)",params![id,due]).map_err(|e|e.to_string())?;result.submitted+=1;
            }
        }
        tx.commit().map_err(|e|e.to_string())?;Ok(result)
    }
    pub fn status(conn:&Connection,enabled:bool)->Result<NotificationStatus,String>{
        Self::schema(conn)?;
        let available=native::available();
        Ok(NotificationStatus{enabled,channel:native::channel().into(),available:available.is_ok(),error:available.err(),
            last_submitted:conn.query_row("SELECT a.LastAttemptAt FROM NotificationAttempts a JOIN DueNotifications n ON n.TaskId=a.TaskId AND n.DueAt=a.DueAt ORDER BY julianday(a.LastAttemptAt) DESC LIMIT 1",[],|r|r.get(0)).ok(),
            last_error:conn.query_row("SELECT LastError FROM NotificationAttempts WHERE LastError IS NOT NULL ORDER BY julianday(LastAttemptAt) DESC LIMIT 1",[],|r|r.get(0)).ok()})
    }
    pub fn send(tag:&str,title:&str,body:&str)->Result<(),String>{native::send(tag,title,body)}
    pub fn receipt(tag:&str)->Result<bool,String>{native::receipt(tag)}
}
#[cfg(windows)]
mod native {
    use windows::{core::HSTRING,Data::Xml::Dom::XmlDocument,UI::Notifications::{ToastNotification,ToastNotificationManager,NotificationSetting},Win32::System::WinRT::{RoInitialize,RoUninitialize,RO_INIT_MULTITHREADED}};
    // A portable application has no installed AUMID. Use the registered portable channel
    // recommended by the underlying Windows toast library; no shortcut/registry install.
    const ID:&str=r"{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}\WindowsPowerShell\v1.0\powershell.exe";
    struct Runtime(bool);
    impl Runtime {fn new()->Self{Self(unsafe{RoInitialize(RO_INIT_MULTITHREADED)}.is_ok())}}
    impl Drop for Runtime{fn drop(&mut self){if self.0 {unsafe{RoUninitialize()}}}}
    pub fn channel()->&'static str{"Windows 便携通知"}
    pub fn available()->Result<(),String>{
        let _runtime=Runtime::new();
        let notifier=ToastNotificationManager::CreateToastNotifierWithId(&HSTRING::from(ID)).map_err(|e|e.to_string())?;
        let setting=notifier.Setting().map_err(|e|e.to_string())?;
        if setting!=NotificationSetting::Enabled {return Err(format!("Windows 通知通道未启用（{}），请检查系统通知设置",setting.0));}Ok(())
    }
    fn escape(text:&str)->String{text.chars().filter(|c|*c>=' '||*c=='\n'||*c=='\t').take(400).collect::<String>().replace('&',"&amp;").replace('<',"&lt;").replace('>',"&gt;").replace('"',"&quot;").replace('\'',"&apos;")}
    pub fn send(tag:&str,title:&str,body:&str)->Result<(),String>{
        let _runtime=Runtime::new();available()?;
        let xml=XmlDocument::new().map_err(|e|e.to_string())?;
        xml.LoadXml(&HSTRING::from(format!("<toast><visual><binding template=\"ToastGeneric\"><text>{}</text><text>{}</text></binding></visual><audio silent=\"true\"/></toast>",escape(title),escape(body)))).map_err(|e|e.to_string())?;
        let toast=ToastNotification::CreateToastNotification(&xml).map_err(|e|e.to_string())?;
        toast.SetTag(&HSTRING::from(tag.chars().take(16).collect::<String>())).map_err(|e|e.to_string())?;
        toast.SetGroup(&HSTRING::from("LifeRPG")).map_err(|e|e.to_string())?;
        ToastNotificationManager::CreateToastNotifierWithId(&HSTRING::from(ID)).and_then(|n|n.Show(&toast)).map_err(|e|e.to_string())
    }
    pub fn receipt(tag:&str)->Result<bool,String>{
        let _runtime=Runtime::new();let tag=HSTRING::from(tag.chars().take(16).collect::<String>());
        let history=ToastNotificationManager::History().and_then(|h|h.GetHistoryWithId(&HSTRING::from(ID))).map_err(|e|e.to_string())?;
        for toast in history {if toast.Group().map_err(|e|e.to_string())?==HSTRING::from("LifeRPG") && toast.Tag().map_err(|e|e.to_string())?==tag {return Ok(true);}}Ok(false)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    fn setup()->Connection{let c=Connection::open_in_memory().unwrap();crate::db::Db::ensure_schema(&c).unwrap();c}
    fn task(c:&Connection,id:&str,due:&str,status:&str){c.execute("INSERT INTO ActionTasks(Id,Title,Status,DueAt,CreatedAt,UpdatedAt) VALUES(?1,?1,?2,?3,'now','now')",params![id,status,due]).unwrap();}
    #[test]
    fn overdue_only_and_success_dedup_survives_recheck_and_changed_deadline(){
        let c=setup();task(&c,"due","2020-01-01T10:00:00+08:00","Pending");task(&c,"future","2099-01-01T00:00:00Z","Pending");task(&c,"done","2020-01-01T00:00:00Z","Completed");task(&c,"invalid","nonsense","Pending");
        let result=NotificationService::dispatch(&c,|id,_|{assert_eq!(id,"due");Ok(())}).unwrap();assert_eq!(result.submitted,1);
        assert_eq!(NotificationService::dispatch(&c,|_,_|panic!("duplicate notification")).unwrap().submitted,0);
        c.execute("UPDATE ActionTasks SET DueAt='2020-01-02T10:00:00+08:00' WHERE Id='due'",[]).unwrap();assert_eq!(NotificationService::dispatch(&c,|_,_|Ok(())).unwrap().submitted,1);
    }
    #[test]
    fn submission_failure_is_not_marked_delivered_and_retries_after_cooldown(){
        let c=setup();task(&c,"due","2020-01-01T00:00:00Z","Pending");
        let result=NotificationService::dispatch(&c,|_,_|Err("blocked".into())).unwrap();assert_eq!(result.failed,1);
        assert_eq!(c.query_row("SELECT COUNT(*) FROM DueNotifications",[],|r|r.get::<_,i64>(0)).unwrap(),0);
        assert_eq!(NotificationService::dispatch(&c,|_,_|panic!("too soon")).unwrap().submitted,0);
        c.execute("UPDATE NotificationAttempts SET LastAttemptAt='2020-01-01T00:00:00Z'",[]).unwrap();assert_eq!(NotificationService::dispatch(&c,|_,_|Ok(())).unwrap().submitted,1);
    }
}
#[cfg(not(windows))]
mod native {
    pub fn channel()->&'static str{"不可用"}
    pub fn available()->Result<(),String>{Err("此版本仅支持 Windows 系统通知".into())}
    pub fn send(_tag:&str,_title:&str,_body:&str)->Result<(),String>{available()}
    pub fn receipt(_tag:&str)->Result<bool,String>{Ok(false)}
}
