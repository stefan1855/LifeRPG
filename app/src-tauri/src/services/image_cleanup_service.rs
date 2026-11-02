use rusqlite::Connection;
use serde::{Deserialize, Serialize};
use std::{collections::HashSet, fs, path::{Path, PathBuf}, time::{SystemTime, UNIX_EPOCH}};

const GRACE_MS: u64 = 24 * 60 * 60 * 1000;
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "camelCase")]
pub struct Candidate { pub name: String, pub bytes: u64, pub modified_ms: u64 }
#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct Scan { pub root: String, pub candidates: Vec<Candidate>, pub bytes: u64, pub recent: usize, pub skipped: usize, pub missing: Vec<String> }
#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Batch { pub id: String, pub created_at: String, pub files: Vec<Candidate> }
pub struct ImageCleanup;
fn basename(value: &str) -> Result<String, String> {
    value.rsplit(['/', '\\']).next().filter(|n| valid_name(n)).map(str::to_string).ok_or_else(|| "图片引用包含无效文件名".into())
}
fn referenced(names:&HashSet<String>,name:&str)->bool { let folded=name.to_lowercase();names.iter().any(|n|n.to_lowercase()==folded) }
fn valid_name(n: &str) -> bool { !n.is_empty() && n != "." && n != ".." && !n.contains(['/', '\\', ':']) }
fn metadata(path: &Path) -> Result<Candidate, String> {
    let m = fs::symlink_metadata(path).map_err(|e| e.to_string())?;
    if !m.is_file() || m.file_type().is_symlink() { return Err("跳过目录或链接".into()); }
    #[cfg(windows)] {
        use std::os::windows::fs::MetadataExt;
        if m.file_attributes() & 0x400 != 0 { return Err("跳过重解析文件".into()); }
    }
    let modified_ms = m.modified().map_err(|e| e.to_string())?.duration_since(UNIX_EPOCH).map_err(|e| e.to_string())?.as_millis() as u64;
    Ok(Candidate { name: path.file_name().ok_or("文件名无效")?.to_string_lossy().into(), bytes: m.len(), modified_ms })
}
pub fn references(conn: &Connection) -> Result<HashSet<String>, String> {
    let mut names = HashSet::new();
    for sql in ["SELECT FilePath FROM LifeRecordImages", "SELECT FilePath FROM ActionTaskImages", "SELECT AvatarImagePath FROM Characters WHERE AvatarImagePath IS NOT NULL AND AvatarImagePath<>''"] {
        let mut stmt = conn.prepare(sql).map_err(|e| e.to_string())?;
        let rows = stmt.query_map([], |r| r.get::<_,String>(0)).map_err(|e| e.to_string())?;
        for row in rows { names.insert(basename(&row.map_err(|e| e.to_string())?)?); }
    }
    Ok(names)
}
fn managed_root(root: &Path) -> Result<PathBuf, String> {
    let root = root.canonicalize().map_err(|e| e.to_string())?;
    // Project policy prohibits deleting files on the system drive.
    #[cfg(windows)] if root.to_string_lossy().to_ascii_lowercase().starts_with(r"\\?\c:\") || root.to_string_lossy().to_ascii_lowercase().starts_with(r"c:\") { return Err("系统盘目录不支持图片清理，请先迁移数据目录".into()); }
    for sub in ["images", ".image-cleanup"] {
        if let Ok(m) = fs::symlink_metadata(root.join(sub)) {
            if m.file_type().is_symlink() { return Err("图片目录或隔离区不能是链接".into()); }
            #[cfg(windows)] { use std::os::windows::fs::MetadataExt; if m.file_attributes() & 0x400 != 0 { return Err("图片目录或隔离区不能是重解析点".into()); } }
        }
    }
    Ok(root)
}
impl ImageCleanup {
    pub fn scan(conn: &Connection, root: &Path) -> Result<Scan,String> {
        let names = references(conn)?; let images = root.join("images");
        let mut scan = Scan { root: root.to_string_lossy().into(), candidates: vec![], bytes: 0, recent: 0, skipped: 0, missing: vec![] };
        for n in &names { if !images.join(n).is_file() { scan.missing.push(n.clone()); } }
        let now = SystemTime::now().duration_since(UNIX_EPOCH).map_err(|e| e.to_string())?.as_millis() as u64;
        let entries=match fs::read_dir(&images) {
            Ok(entries)=>entries,
            Err(error) if error.kind()==std::io::ErrorKind::NotFound=>return Ok(scan),
            Err(error)=>return Err(error.to_string()),
        };
        for item in entries {
            let item = item.map_err(|e| e.to_string())?;
            let file = match metadata(&item.path()) { Ok(f) => f, Err(_) => { scan.skipped += 1; continue; } };
            if referenced(&names,&file.name) { continue; }
            if now.saturating_sub(file.modified_ms) < GRACE_MS { scan.recent += 1; continue; }
            if !matches!(item.path().extension().and_then(|e| e.to_str()).map(str::to_ascii_lowercase).as_deref(), Some("jpg"|"jpeg"|"png"|"webp"|"gif"|"bmp")) { scan.skipped += 1; continue; }
            scan.bytes += file.bytes; scan.candidates.push(file);
        }
        scan.candidates.sort_by(|a,b| a.name.cmp(&b.name)); scan.missing.sort(); Ok(scan)
    }
    pub fn quarantine(conn: &Connection, root: &Path, expected_root: &str, selected: &[Candidate]) -> Result<Batch,String> {
        let _safe = managed_root(root)?;
        if root != Path::new(expected_root) { return Err("数据目录已变化，请重新扫描".into()); }
        if selected.is_empty() { return Err("请先选择图片".into()); }
        let tx = conn.unchecked_transaction().map_err(|e| e.to_string())?;
        // Take a write reservation before checking references.
        tx.execute("UPDATE AppMetadata SET Value=Value WHERE Key='image_cleanup_lock'", []).map_err(|e| e.to_string())?;
        let scan = Self::scan(&tx, root)?;
        let mut unique = HashSet::new();
        for file in selected {
            if !valid_name(&file.name) || !unique.insert(&file.name) || !scan.candidates.contains(file) { return Err("清单已变化或图片已被引用，请重新扫描".into()); }
        }
        let batch = Batch { id: uuid::Uuid::new_v4().to_string(), created_at: chrono::Local::now().to_rfc3339(), files: selected.to_vec() };
        let dir = root.join(".image-cleanup").join(&batch.id);fs::create_dir_all(dir.join("files")).map_err(|e| e.to_string())?;
        // Persist the complete intent first; recovery uses the files actually present after a crash.
        let mut manifest = fs::File::create(dir.join("manifest.json")).map_err(|e| e.to_string())?;
        use std::io::Write;
        manifest.write_all(&serde_json::to_vec(&batch).map_err(|e| e.to_string())?).and_then(|_| manifest.sync_all()).map_err(|e| e.to_string())?;
        for file in selected { fs::rename(root.join("images").join(&file.name), dir.join("files").join(&file.name)).map_err(|e| format!("隔离未全部完成，可在隔离区恢复：{}",e))?; }
        tx.commit().map_err(|e| e.to_string())?; Ok(batch)
    }
    fn batch(root: &Path, id: &str) -> Result<(PathBuf,Batch),String> {
        managed_root(root)?;uuid::Uuid::parse_str(id).map_err(|_| "隔离批次无效")?;
        let dir = root.join(".image-cleanup").join(id);
        for p in [&dir, &dir.join("files")] {
            let m=fs::symlink_metadata(p).map_err(|e| e.to_string())?;
            if m.file_type().is_symlink() { return Err("隔离区包含链接".into()); }
            #[cfg(windows)] {use std::os::windows::fs::MetadataExt;if m.file_attributes() & 0x400 != 0 {return Err("隔离区包含重解析点".into());}}
        }
        let mut batch: Batch = serde_json::from_slice(&fs::read(dir.join("manifest.json")).map_err(|e| e.to_string())?).map_err(|e| e.to_string())?;
        if batch.id != id || batch.files.iter().any(|f| !valid_name(&f.name)) { return Err("隔离清单无效".into()); }
        batch.files.retain(|f| dir.join("files").join(&f.name).is_file()); Ok((dir,batch))
    }
    pub fn batches(root: &Path) -> Result<Vec<Batch>,String> {
        managed_root(root)?;let dir=root.join(".image-cleanup");if !dir.exists(){return Ok(vec![]);}
        let mut batches=vec![];
        for item in fs::read_dir(dir).map_err(|e| e.to_string())? {
            let item=item.map_err(|e| e.to_string())?;
            if let Some(id)=item.file_name().to_str() { if let Ok((_,batch))=Self::batch(root,id) { if !batch.files.is_empty(){ batches.push(batch); } } }
        }
        batches.sort_by(|a,b| b.created_at.cmp(&a.created_at));Ok(batches)
    }
    pub fn restore(root: &Path,id:&str) -> Result<usize,String> {
        let (dir,batch)=Self::batch(root,id)?;
        fs::create_dir_all(root.join("images")).map_err(|e|e.to_string())?;
        // Preflight the whole batch: never overwrite a file, including a dangling link.
        for file in &batch.files { if fs::symlink_metadata(root.join("images").join(&file.name)).is_ok(){return Err("目标已有同名文件，未覆盖；请先检查图片目录".into());} metadata(&dir.join("files").join(&file.name))?; }
        for file in &batch.files {fs::rename(dir.join("files").join(&file.name),root.join("images").join(&file.name)).map_err(|e| e.to_string())?;}
        Ok(batch.files.len())
    }
    pub fn purge(conn:&Connection,root:&Path,id:&str,confirmation:&str) -> Result<usize,String> {
        if confirmation!="永久删除" {return Err("请输入“永久删除”以确认".into());}
        let tx=conn.unchecked_transaction().map_err(|e|e.to_string())?;
        tx.execute("UPDATE AppMetadata SET Value=Value WHERE Key='image_cleanup_lock'",[]).map_err(|e|e.to_string())?;
        let (dir,batch)=Self::batch(root,id)?;let names=references(&tx)?;
        for file in &batch.files { if referenced(&names,&file.name){return Err("隔离图片已重新被引用，请先恢复".into());} if metadata(&dir.join("files").join(&file.name))? != *file {return Err("隔离图片已变化，未删除".into());} }
        for file in &batch.files {fs::remove_file(dir.join("files").join(&file.name)).map_err(|e| e.to_string())?;}
        tx.commit().map_err(|e|e.to_string())?;Ok(batch.files.len())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    fn setup()->(Connection,PathBuf){
        let temp=std::env::temp_dir();assert!(!temp.to_string_lossy().to_ascii_lowercase().starts_with("c:"),"Load scripts/build-env.ps1 first");
        let root=temp.join(format!("image-cleanup-test-{}",uuid::Uuid::new_v4()));fs::create_dir_all(root.join("images")).unwrap();
        let conn=Connection::open_in_memory().unwrap();crate::db::Db::ensure_schema(&conn).unwrap();(conn,root)
    }
    fn old(root:&Path,name:&str){let path=root.join("images").join(name);fs::write(&path,b"test-image").unwrap();fs::File::options().write(true).open(path).unwrap().set_times(fs::FileTimes::new().set_modified(SystemTime::now()-std::time::Duration::from_secs(90000))).unwrap();}
    #[test]
    fn first_scan_without_an_image_directory_is_empty(){
        let (conn,root)=setup();fs::remove_dir(root.join("images")).unwrap();let scan=ImageCleanup::scan(&conn,&root).unwrap();assert!(scan.candidates.is_empty());assert!(scan.missing.is_empty());
        assert_eq!(root.parent(),Some(std::env::temp_dir().as_path()));fs::remove_dir_all(root).unwrap();
    }
    #[test]
    fn protects_all_references_and_recent_files(){
        let (conn,root)=setup();for name in ["record.jpg","task.jpg","avatar.jpg","orphan.jpg","unknown.txt"] {old(&root,name);}
        fs::write(root.join("images/recent.jpg"),b"new").unwrap();
        conn.execute("INSERT INTO LifeRecords(Id,Title,Content,Category,Experience,CreatedAt,UpdatedAt) VALUES('r','record','','test',0,'now','now')",[]).unwrap();
        conn.execute("INSERT INTO LifeRecordImages(Id,LifeRecordId,FileName,FilePath,CreatedAt) VALUES('i','r','record.jpg','D:\\old\\images\\record.jpg','now')",[]).unwrap();
        conn.execute("INSERT INTO ActionTasks(Id,Title,CreatedAt,UpdatedAt) VALUES('t','task','now','now')",[]).unwrap();
        conn.execute("INSERT INTO ActionTaskImages(Id,ActionTaskId,FileName,FilePath,CreatedAt) VALUES('ti','t','task.jpg','task.jpg','now')",[]).unwrap();
        conn.execute("UPDATE Characters SET AvatarImagePath='AVATAR.JPG'",[]).unwrap();
        let scan=ImageCleanup::scan(&conn,&root).unwrap();assert_eq!(scan.candidates.len(),1);assert_eq!(scan.candidates[0].name,"orphan.jpg");assert_eq!(scan.recent,1);assert_eq!(scan.skipped,1);assert!(scan.missing.is_empty());
        assert_eq!(root.parent(),Some(std::env::temp_dir().as_path()));fs::remove_dir_all(root).unwrap();
    }
    #[test]
    fn quarantine_rechecks_references_and_restore_never_overwrites(){
        let (conn,root)=setup();old(&root,"orphan.jpg");let scan=ImageCleanup::scan(&conn,&root).unwrap();
        conn.execute("UPDATE Characters SET AvatarImagePath='orphan.jpg'",[]).unwrap();
        assert!(ImageCleanup::quarantine(&conn,&root,&scan.root,&scan.candidates).is_err());assert!(root.join("images/orphan.jpg").exists());
        conn.execute("UPDATE Characters SET AvatarImagePath=NULL",[]).unwrap();
        let batch=ImageCleanup::quarantine(&conn,&root,&scan.root,&scan.candidates).unwrap();assert!(!root.join("images/orphan.jpg").exists());
        assert_eq!(ImageCleanup::batches(&root).unwrap().len(),1);
        fs::write(root.join("images/orphan.jpg"),b"replacement").unwrap();assert!(ImageCleanup::restore(&root,&batch.id).is_err());assert_eq!(fs::read(root.join("images/orphan.jpg")).unwrap(),b"replacement");
        fs::remove_file(root.join("images/orphan.jpg")).unwrap();assert_eq!(ImageCleanup::restore(&root,&batch.id).unwrap(),1);
        assert!(ImageCleanup::batches(&root).unwrap().is_empty());
        assert_eq!(root.parent(),Some(std::env::temp_dir().as_path()));fs::remove_dir_all(root).unwrap();
    }
    #[test]
    fn permanent_delete_requires_confirmation_and_current_reference_check(){
        let (conn,root)=setup();old(&root,"orphan.jpg");let scan=ImageCleanup::scan(&conn,&root).unwrap();let batch=ImageCleanup::quarantine(&conn,&root,&scan.root,&scan.candidates).unwrap();
        assert!(ImageCleanup::purge(&conn,&root,&batch.id,"yes").is_err());
        conn.execute("UPDATE Characters SET AvatarImagePath='orphan.jpg'",[]).unwrap();assert!(ImageCleanup::purge(&conn,&root,&batch.id,"永久删除").is_err());
        conn.execute("UPDATE Characters SET AvatarImagePath=NULL",[]).unwrap();assert_eq!(ImageCleanup::purge(&conn,&root,&batch.id,"永久删除").unwrap(),1);
        assert!(ImageCleanup::batches(&root).unwrap().is_empty());assert!(ImageCleanup::restore(&root,"../outside").is_err());
        assert_eq!(root.parent(),Some(std::env::temp_dir().as_path()));fs::remove_dir_all(root).unwrap();
    }
}
