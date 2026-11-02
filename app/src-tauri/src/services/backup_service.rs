use rusqlite::Connection;
use std::fs;
use std::path::{Path, PathBuf};

use crate::models::BackupInfo;

/// 备份与恢复：把数据库快照 + 图片目录打包成 zip
pub struct BackupService;
#[derive(serde::Serialize)]
#[serde(rename_all="camelCase")]
pub struct RestoreReport {pub protection_path:String,pub previously_missing_images:usize}

impl BackupService {
    /// 创建备份，dest_dir 为空时落到默认备份目录
    pub fn create(conn: &Connection, dest_dir: Option<String>) -> Result<BackupInfo, String> {
        let dir = match dest_dir {
            Some(d) if !d.trim().is_empty() => PathBuf::from(d),
            _ => crate::db::Db::get_backup_dir()?,
        };
        let images = crate::db::Db::get_images_dir()?;
        Self::create_at(conn, &dir, Some(&images))
    }

    pub(crate) fn create_at(conn: &Connection, dir: &Path, images: Option<&Path>) -> Result<BackupInfo, String> {
        fs::create_dir_all(&dir).map_err(|e| format!("无法创建备份目录: {}", e))?;

        let stamp = chrono::Local::now().format("%Y%m%d_%H%M%S_%f").to_string();
        let file_name = format!("liferpg_backup_{}.zip", stamp);
        let zip_path = dir.join(&file_name);

        // 用 VACUUM INTO 拿到一致性快照
        let snapshot = dir.join(format!(".snapshot_{}.db", stamp));
        let _ = fs::remove_file(&snapshot);
        conn.execute(
            &format!("VACUUM INTO '{}'", snapshot.to_string_lossy().replace('\'', "''")),
            [],
        )
        .map_err(|e| format!("数据库快照失败: {}", e))?;

        let pending=dir.join(format!(".{}.partial",file_name));
        let outcome=(||->Result<(),String>{
            let snapshot_conn=Connection::open(&snapshot).map_err(|e|e.to_string())?;
            let references=crate::services::image_cleanup_service::references(&snapshot_conn)?;
            if let Some(images_dir)=images {
                // Read errors and missing references must fail before publishing a backup.
                for entry in fs::read_dir(images_dir).map_err(|e|e.to_string())? {entry.map_err(|e|e.to_string())?;}
                for name in &references {if !images_dir.join(name).is_file(){return Err(format!("备份缺少引用图片：{}",name));}}
            } else if !references.is_empty(){return Err("备份缺少图片目录".into());}
            let file=fs::File::create(&pending).map_err(|e|e.to_string())?;
            let mut zip=zip::ZipWriter::new(file);
            let opts=zip::write::SimpleFileOptions::default().compression_method(zip::CompressionMethod::Deflated);
            zip.start_file("liferpg.db",opts).map_err(|e|e.to_string())?;
            std::io::copy(&mut fs::File::open(&snapshot).map_err(|e|e.to_string())?,&mut zip).map_err(|e|e.to_string())?;
            if let Some(images_dir)=images {
                // Keep unreferenced files until the user explicitly reviews image maintenance.
                for entry in fs::read_dir(images_dir).map_err(|e|e.to_string())? {
                    let entry=entry.map_err(|e|e.to_string())?;let path=entry.path();
                    let m=fs::symlink_metadata(&path).map_err(|e|e.to_string())?;
                    if !m.is_file() {continue;}
                    if m.file_type().is_symlink(){return Err("图片目录包含链接，请先检查".into());}
                    #[cfg(windows)] {use std::os::windows::fs::MetadataExt;if m.file_attributes() & 0x400 != 0{return Err("图片目录包含重解析文件，请先检查".into());}}
                    zip.start_file(format!("images/{}",entry.file_name().to_string_lossy()),opts).map_err(|e|e.to_string())?;
                    std::io::copy(&mut fs::File::open(path).map_err(|e|e.to_string())?,&mut zip).map_err(|e|e.to_string())?;
                }
            }
            zip.finish().and_then(|f|f.sync_all().map_err(zip::result::ZipError::Io)).map_err(|e|e.to_string())?;
            fs::rename(&pending,&zip_path).map_err(|e|e.to_string())?;Ok(())
        })();
        let _=fs::remove_file(&snapshot);
        if outcome.is_err(){let _=fs::remove_file(&pending);}
        outcome?;

        let size = fs::metadata(&zip_path).map(|m| m.len()).unwrap_or(0);
        Ok(BackupInfo {
            file_name,
            file_path: zip_path.to_string_lossy().to_string(),
            size_bytes: size,
            created_at: chrono::Local::now().to_rfc3339(),
        })
    }

    /// 列出备份（默认目录 + 指定目录）
    pub fn list(extra_dir: Option<String>) -> Vec<BackupInfo> {
        let mut dirs = Vec::new();
        if let Ok(d) = crate::db::Db::get_backup_dir() {
            dirs.push(d);
        }
        if let Some(d) = extra_dir {
            if !d.trim().is_empty() {
                dirs.push(PathBuf::from(d));
            }
        }
        let mut out = Vec::new();
        for dir in dirs {
            if let Ok(entries) = fs::read_dir(&dir) {
                for entry in entries.flatten() {
                    let p = entry.path();
                    if p.extension().map(|e| e == "zip").unwrap_or(false) {
                        let meta = entry.metadata().ok();
                        let created = meta
                            .as_ref()
                            .and_then(|m| m.modified().ok())
                            .map(|t| chrono::DateTime::<chrono::Local>::from(t).to_rfc3339())
                            .unwrap_or_default();
                        out.push(BackupInfo {
                            file_name: p
                                .file_name()
                                .map(|n| n.to_string_lossy().to_string())
                                .unwrap_or_default(),
                            file_path: p.to_string_lossy().to_string(),
                            size_bytes: meta.map(|m| m.len()).unwrap_or(0),
                            created_at: created,
                        });
                    }
                }
            }
        }
        out.sort_by(|a, b| b.created_at.cmp(&a.created_at));
        out
    }

    /// 从 zip 恢复：先备份当前状态，再覆盖数据库与图片
    pub fn restore(zip_path: &str) -> Result<RestoreReport, String> {
        let mut conn = crate::db::Db::open()?;
        let images=crate::db::Db::get_images_dir()?;
        let protection=match Self::create(&conn,None) {
            Ok(info)=>RestoreReport{protection_path:info.file_path,previously_missing_images:0},
            Err(error) if error.starts_with("备份缺少引用图片")=>{
                // A missing image is precisely a reason to restore. Preserve the readable
                // current state explicitly, without calling an incomplete ZIP a valid backup.
                let dir=crate::db::Db::get_backup_dir()?.join(format!("protection-{}",uuid::Uuid::new_v4()));
                fs::create_dir_all(dir.join("data")).map_err(|e|e.to_string())?;
                fs::create_dir(dir.join("images")).map_err(|e|e.to_string())?;
                conn.backup(rusqlite::DatabaseName::Main,dir.join("data/liferpg.db"),None).map_err(|e|e.to_string())?;
                let missing:Vec<String>=crate::services::image_cleanup_service::references(&conn)?.into_iter().filter(|n|!images.join(n).is_file()).collect();
                for item in fs::read_dir(&images).map_err(|e|e.to_string())? {
                    let item=item.map_err(|e|e.to_string())?;let meta=fs::symlink_metadata(item.path()).map_err(|e|e.to_string())?;
                    if meta.file_type().is_symlink(){return Err("保护当前状态失败：图片目录含链接".into());}
                    #[cfg(windows)] {use std::os::windows::fs::MetadataExt;if meta.file_attributes()&0x400!=0{return Err("保护当前状态失败：图片含重解析点".into());}}
                    if meta.is_file(){fs::copy(item.path(),dir.join("images").join(item.file_name())).map_err(|e|e.to_string())?;}
                }
                fs::write(dir.join("protection.json"),serde_json::to_vec_pretty(&serde_json::json!({"createdAt":chrono::Local::now().to_rfc3339(),"missingImages":missing,"note":"恢复前可读状态，引用图片存在缺失；不是完整备份"})).map_err(|e|e.to_string())?).map_err(|e|e.to_string())?;
                RestoreReport{protection_path:dir.to_string_lossy().into(),previously_missing_images:missing.len()}
            },
            Err(error)=>return Err(error),
        };
        Self::restore_into(zip_path, &mut conn, &images)?;Ok(protection)
    }

    pub(crate) fn restore_into(zip_path: &str, destination: &mut Connection, images: &Path) -> Result<(), String> {
        let mut archive = zip::ZipArchive::new(fs::File::open(zip_path).map_err(|e| e.to_string())?).map_err(|e| e.to_string())?;
        // Validate the complete archive before writing any managed files.
        let mut names = std::collections::HashSet::new();
        let mut total = 0u64;
        for i in 0..archive.len() {
            let entry = archive.by_index(i).map_err(|e| e.to_string())?;
            let name = entry.name();
            let valid = name == "liferpg.db" || name.strip_prefix("images/").map(|n| !n.is_empty() && !n.contains(['/', '\\', ':']) && n != "." && n != "..").unwrap_or(false);
            total = total.checked_add(entry.size()).ok_or("备份过大")?;
            if !valid || !names.insert(name.to_string()) || total > 4 * 1024 * 1024 * 1024 { return Err("备份包含非法、重复或过大的条目".into()); }
        }
        if !names.contains("liferpg.db") { return Err("备份缺少 liferpg.db".into()); }
        fs::create_dir_all(images).map_err(|e| e.to_string())?;
        let stage = images.join(format!(".restore-{}", uuid::Uuid::new_v4()));
        fs::create_dir(&stage).map_err(|e| e.to_string())?;
        let outcome = (|| -> Result<(), String> {
            let db = stage.join("liferpg.db");
            {
                let mut entry = archive.by_name("liferpg.db").map_err(|e| e.to_string())?;
                let mut file = fs::File::create(&db).map_err(|e| e.to_string())?;
                std::io::copy(&mut entry, &mut file).map_err(|e| e.to_string())?;
            }
            let restored = Connection::open(&db).map_err(|e| e.to_string())?;
            let check: String = restored.query_row("PRAGMA integrity_check", [], |r| r.get(0)).map_err(|e| e.to_string())?;
            if check != "ok" { return Err("备份数据库校验失败".into()); }
            for table in ["Characters", "CharacterAttributes", "AttributeLogs", "RpgExperienceEvents", "ActionTasks", "GrowthAttributes", "LifeRecords", "LifeRecordImages", "ActionTaskImages", "EquipmentDefinitions", "CharacterEquipment", "EquipmentAttributeBonuses", "SkillDefinitions", "CharacterSkills", "SkillUnlockConditions", "NpcDefinitions", "NpcRelationships", "MapLocations"] {
                restored.prepare(&format!("SELECT * FROM {} LIMIT 0", table)).map_err(|e| format!("备份结构不完整：{}", e))?;
            }
            let broken_foreign_key = restored.prepare("PRAGMA foreign_key_check").map_err(|e| e.to_string())?.exists([]).map_err(|e| e.to_string())?;
            if broken_foreign_key { return Err("备份数据库存在断裂的数据关联".into()); }
            let mut mapping=std::collections::HashMap::new();
            for old in crate::services::image_cleanup_service::references(&restored)? {
                let key=format!("images/{}",old);
                let key=if archive.file_names().any(|n|n==key){key}else{
                    let matches:Vec<String>=archive.file_names().filter(|n|n.to_lowercase()==key.to_lowercase()).map(str::to_string).collect();
                    if matches.len()!=1{return Err(format!("备份缺少图片或文件名不明确：{}",old));}matches[0].clone()
                };
                let mut entry=archive.by_name(&key).map_err(|_|format!("备份缺少图片：{}",old))?;
                let new=format!("{}.jpg",uuid::Uuid::new_v4());
                let mut file=fs::File::create(images.join(&new)).map_err(|e|e.to_string())?;
                std::io::copy(&mut entry,&mut file).map_err(|e|e.to_string())?;mapping.insert(old,new);
            }
            for table in ["LifeRecordImages","ActionTaskImages"] {
                let mut stmt=restored.prepare(&format!("SELECT Id,FilePath FROM {}",table)).map_err(|e|e.to_string())?;
                let rows=stmt.query_map([],|r|Ok((r.get::<_,String>(0)?,r.get::<_,String>(1)?))).map_err(|e|e.to_string())?.collect::<rusqlite::Result<Vec<_>>>().map_err(|e|e.to_string())?;
                for (id,path) in rows {let name=path.rsplit(['/', '\\']).next().ok_or("图片引用无效")?;let new=mapping.get(name).ok_or("图片映射缺失")?;restored.execute(&format!("UPDATE {} SET FileName=?1,FilePath=?1 WHERE Id=?2",table),rusqlite::params![new,id]).map_err(|e|e.to_string())?;}
            }
            let mut stmt=restored.prepare("SELECT Id,AvatarImagePath FROM Characters WHERE AvatarImagePath IS NOT NULL AND AvatarImagePath<>''").map_err(|e|e.to_string())?;
            let avatars=stmt.query_map([],|r|Ok((r.get::<_,String>(0)?,r.get::<_,String>(1)?))).map_err(|e|e.to_string())?.collect::<rusqlite::Result<Vec<_>>>().map_err(|e|e.to_string())?;
            for (id,path) in avatars {let name=path.rsplit(['/', '\\']).next().ok_or("头像引用无效")?;restored.execute("UPDATE Characters SET AvatarImagePath=?1 WHERE Id=?2",rusqlite::params![mapping.get(name).ok_or("头像映射缺失")?,id]).map_err(|e|e.to_string())?;}
            // SQLite's backup transaction replaces the database atomically and preserves WAL correctness.
            let backup = rusqlite::backup::Backup::new(&restored,destination).map_err(|e| e.to_string())?;
            backup.run_to_completion(128,std::time::Duration::from_millis(10),None).map_err(|e| e.to_string())?;
            Ok(())
        })();
        let _ = fs::remove_dir_all(&stage);
        outcome
    }
}
