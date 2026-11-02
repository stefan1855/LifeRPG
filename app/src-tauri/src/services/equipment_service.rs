use rusqlite::{params, Connection};
use uuid::Uuid;

use crate::models::{
    EquipmentBonus, EquipmentItem, EquipmentSlot, EquipmentSnapshot, SaveEquipmentInput,
};
use crate::services::character_service::CharacterService;

/// 12 个装备槽位（顺序即界面顺序）
pub const SLOTS: &[(&str, &str)] = &[
    ("head", "头部"),
    ("shoulder", "肩部"),
    ("chest", "胸甲"),
    ("hands", "手套"),
    ("waist", "腰带"),
    ("legs", "腿部"),
    ("feet", "战靴"),
    ("wrist", "护腕"),
    ("mainhand", "主手"),
    ("offhand", "副手"),
    ("trinket1", "饰品一"),
    ("trinket2", "饰品二"),
];

pub struct EquipmentService;

impl EquipmentService {
    fn now() -> String {
        chrono::Local::now().to_rfc3339()
    }

    fn load_bonuses(conn: &Connection, def_id: &str) -> rusqlite::Result<Vec<EquipmentBonus>> {
        let mut stmt = conn.prepare(
            "SELECT AttributeKey, BonusValue FROM EquipmentAttributeBonuses WHERE EquipmentDefinitionId = ?1",
        )?;
        let rows = stmt.query_map(params![def_id], |row| {
            Ok(EquipmentBonus {
                attribute_key: row.get(0)?,
                bonus_value: row.get(1)?,
            })
        })?;
        rows.collect()
    }

    /// 列出全部装备（含是否已装备）
    pub fn list(conn: &Connection) -> rusqlite::Result<Vec<EquipmentItem>> {
        let character = CharacterService::get_default_character(conn)?;
        let mut stmt = conn.prepare(
            "SELECT Id, RealItemName, GameItemName, Category, Status, RealEffect, CreatedAt, UpdatedAt
             FROM EquipmentDefinitions ORDER BY Category, CreatedAt",
        )?;
        let base = stmt
            .query_map([], |row| {
                Ok(EquipmentItem {
                    id: row.get(0)?,
                    real_item_name: row.get(1)?,
                    game_item_name: row.get(2)?,
                    category: row.get(3)?,
                    status: row.get(4)?,
                    real_effect: row.get(5)?,
                    created_at: row.get(6)?,
                    updated_at: row.get(7)?,
                    bonuses: Vec::new(),
                    equipped: false,
                })
            })?
            .collect::<rusqlite::Result<Vec<_>>>()?;

        let mut out = Vec::with_capacity(base.len());
        for mut item in base {
            item.bonuses = Self::load_bonuses(conn, &item.id)?;
            item.equipped = conn
                .query_row(
                    "SELECT COUNT(*) FROM EffectiveCharacterEquipment
                     WHERE CharacterId = ?1 AND EquipmentDefinitionId = ?2 AND Status = 'Equipped'",
                    params![&character.id, &item.id],
                    |r| r.get::<_, i64>(0),
                )
                .unwrap_or(0)
                > 0;
            out.push(item);
        }
        Ok(out)
    }

    /// 12 槽快照 + 属性加成合计
    pub fn snapshot(conn: &Connection) -> rusqlite::Result<EquipmentSnapshot> {
        let items = Self::list(conn)?;
        let mut slots = Vec::with_capacity(SLOTS.len());
        for (key, label) in SLOTS {
            let item = items
                .iter()
                .find(|i| i.equipped && i.category == *key)
                .cloned();
            slots.push(EquipmentSlot {
                key: key.to_string(),
                label: label.to_string(),
                item,
            });
        }

        // 汇总加成
        let mut totals: Vec<EquipmentBonus> = Vec::new();
        for slot in &slots {
            if let Some(item) = &slot.item {
                for b in &item.bonuses {
                    if let Some(existing) = totals.iter_mut().find(|t| t.attribute_key == b.attribute_key) {
                        existing.bonus_value += b.bonus_value;
                    } else {
                        totals.push(b.clone());
                    }
                }
            }
        }

        Ok(EquipmentSnapshot {
            slots,
            total_bonuses: totals,
        })
    }

    pub fn save(conn: &Connection, input: SaveEquipmentInput) -> Result<String, String> {
        if !SLOTS.iter().any(|(key,_)|*key==input.category){return Err("无效装备槽位".into());}
        let tx=rusqlite::Transaction::new_unchecked(conn,rusqlite::TransactionBehavior::Immediate).map_err(|e|e.to_string())?;
        let conn=&tx;
        let now = Self::now();
        let real_effect = input.real_effect.unwrap_or_default();

        let id = match input.id {
            Some(id) if !id.is_empty() => {
                conn.execute(
                    "UPDATE EquipmentDefinitions
                     SET RealItemName = ?1, GameItemName = ?2, Category = ?3, RealEffect = ?4, UpdatedAt = ?5
                     WHERE Id = ?6",
                    params![
                        &input.real_item_name,
                        &input.game_item_name,
                        &input.category,
                        &real_effect,
                        &now,
                        &id
                    ],
                )
                .map_err(|e| e.to_string())?;
                id
            }
            _ => {
                let new_id = Uuid::new_v4().to_string();
                conn.execute(
                    "INSERT INTO EquipmentDefinitions
                     (Id, RealItemName, GameItemName, Category, Status, RealEffect, CreatedAt, UpdatedAt)
                     VALUES (?1, ?2, ?3, ?4, 'Owned', ?5, ?6, ?6)",
                    params![
                        &new_id,
                        &input.real_item_name,
                        &input.game_item_name,
                        &input.category,
                        &real_effect,
                        &now
                    ],
                )
                .map_err(|e| e.to_string())?;
                new_id
            }
        };

        // 重建加成
        conn.execute(
            "DELETE FROM EquipmentAttributeBonuses WHERE EquipmentDefinitionId = ?1",
            params![&id],
        )
        .map_err(|e| e.to_string())?;
        if let Some(bonuses) = input.bonuses {
            for b in bonuses {
                if b.bonus_value == 0 {
                    continue;
                }
                conn.execute(
                    "INSERT INTO EquipmentAttributeBonuses (Id, EquipmentDefinitionId, AttributeKey, BonusValue)
                     VALUES (?1, ?2, ?3, ?4)",
                    params![Uuid::new_v4().to_string(), &id, &b.attribute_key, b.bonus_value],
                )
                .map_err(|e| e.to_string())?;
            }
        }
        let character=CharacterService::get_default_character(conn).map_err(|e|e.to_string())?;
        let equipped:bool=conn.query_row("SELECT EXISTS(SELECT 1 FROM CharacterEquipment WHERE CharacterId=?1 AND EquipmentDefinitionId=?2 AND Status='Equipped')",params![character.id,id],|r|r.get(0)).map_err(|e|e.to_string())?;
        if equipped {conn.execute("DELETE FROM CharacterEquipment WHERE CharacterId=?1 AND EquipmentDefinitionId<>?2 AND EquipmentDefinitionId IN (SELECT Id FROM EquipmentDefinitions WHERE Category=?3)",params![character.id,id,input.category]).map_err(|e|e.to_string())?;}
        tx.commit().map_err(|e|e.to_string())?;
        Ok(id)
    }

    /// 装备 / 卸下切换（同槽位互斥）
    pub fn toggle(conn: &Connection, def_id: &str) -> Result<bool, String> {
        let tx=rusqlite::Transaction::new_unchecked(conn,rusqlite::TransactionBehavior::Immediate).map_err(|e|e.to_string())?;
        let conn=&tx;
        let character = CharacterService::get_default_character(conn).map_err(|e| e.to_string())?;
        let now = Self::now();

        let category: String = conn
            .query_row(
                "SELECT Category FROM EquipmentDefinitions WHERE Id = ?1",
                params![def_id],
                |r| r.get(0),
            )
            .map_err(|_| "装备不存在".to_string())?;

        let already: i64 = conn
            .query_row(
                "SELECT COUNT(*) FROM CharacterEquipment
                 WHERE CharacterId = ?1 AND EquipmentDefinitionId = ?2 AND Status = 'Equipped'",
                params![&character.id, def_id],
                |r| r.get(0),
            )
            .unwrap_or(0);

        if already > 0 {
            conn.execute(
                "DELETE FROM CharacterEquipment WHERE CharacterId = ?1 AND EquipmentDefinitionId = ?2",
                params![&character.id, def_id],
            )
            .map_err(|e| e.to_string())?;
            tx.commit().map_err(|e|e.to_string())?;
            return Ok(false);
        }

        // 先把同槽位的其他装备卸下
        let same_slot: Vec<String> = {
            let mut stmt = conn
                .prepare(
                    "SELECT ce.Id FROM CharacterEquipment ce
                     JOIN EquipmentDefinitions ed ON ed.Id = ce.EquipmentDefinitionId
                     WHERE ce.CharacterId = ?1 AND ce.Status = 'Equipped' AND ed.Category = ?2",
                )
                .map_err(|e| e.to_string())?;
            let rows = stmt
                .query_map(params![&character.id, &category], |r| r.get::<_, String>(0))
                .map_err(|e| e.to_string())?;
            let collected: Vec<String> = rows.flatten().collect();
            collected
        };
        for row_id in same_slot {
            conn.execute("DELETE FROM CharacterEquipment WHERE Id = ?1", params![row_id])
                .map_err(|e| e.to_string())?;
        }

        conn.execute(
            "INSERT INTO CharacterEquipment (Id, CharacterId, EquipmentDefinitionId, Status, EquippedAt, CreatedAt, UpdatedAt)
             VALUES (?1, ?2, ?3, 'Equipped', ?4, ?4, ?4)",
            params![Uuid::new_v4().to_string(), &character.id, def_id, &now],
        )
        .map_err(|e| e.to_string())?;
        tx.commit().map_err(|e|e.to_string())?;
        Ok(true)
    }

    pub fn delete(conn: &Connection, def_id: &str) -> Result<bool, String> {
        conn.execute(
            "DELETE FROM CharacterEquipment WHERE EquipmentDefinitionId = ?1",
            params![def_id],
        )
        .map_err(|e| e.to_string())?;
        conn.execute(
            "DELETE FROM EquipmentAttributeBonuses WHERE EquipmentDefinitionId = ?1",
            params![def_id],
        )
        .map_err(|e| e.to_string())?;
        let n = conn
            .execute("DELETE FROM EquipmentDefinitions WHERE Id = ?1", params![def_id])
            .map_err(|e| e.to_string())?;
        Ok(n > 0)
    }
}

impl EquipmentService {
    pub fn conflicts(conn:&Connection)->Result<Vec<serde_json::Value>,String>{
        let mut stmt=conn.prepare("SELECT ce.Id,ed.GameItemName,ed.Category,ce.EquippedAt,NOT EXISTS(SELECT 1 FROM EffectiveCharacterEquipment e WHERE e.Id=ce.Id) FROM CharacterEquipment ce JOIN EquipmentDefinitions ed ON ed.Id=ce.EquipmentDefinitionId WHERE ce.Status='Equipped' AND EXISTS(SELECT 1 FROM CharacterEquipment c2 JOIN EquipmentDefinitions d2 ON d2.Id=c2.EquipmentDefinitionId WHERE c2.CharacterId=ce.CharacterId AND c2.Status='Equipped' AND d2.Category=ed.Category AND c2.Id<>ce.Id) ORDER BY ed.Category,ce.EquippedAt DESC,ce.Id DESC").map_err(|e|e.to_string())?;
        let rows=stmt.query_map([],|r|Ok(serde_json::json!({"id":r.get::<_,String>(0)?,"name":r.get::<_,String>(1)?,"slot":r.get::<_,String>(2)?,"equippedAt":r.get::<_,String>(3)?,"remove":r.get::<_,bool>(4)?}))).map_err(|e|e.to_string())?;
        rows.collect::<rusqlite::Result<Vec<_>>>().map_err(|e|e.to_string())
    }
    pub fn repair(conn:&Connection)->Result<usize,String>{
        conn.execute("DELETE FROM CharacterEquipment WHERE Status='Equipped' AND Id NOT IN (SELECT Id FROM EffectiveCharacterEquipment)",[]).map_err(|e|e.to_string())
    }
}
#[cfg(test)]
mod rc5_equipment_tests {
 use super::*;
 #[test]
 fn moving_equipped_item_replaces_target_and_effective_bonuses_match(){
  let conn=Connection::open_in_memory().unwrap();crate::db::Db::ensure_schema(&conn).unwrap();
  let input=|id:Option<String>,name:&str,slot:&str,bonus:i32|SaveEquipmentInput{id,real_item_name:name.into(),game_item_name:name.into(),category:slot.into(),real_effect:None,bonuses:Some(vec![EquipmentBonus{attribute_key:"mind".into(),bonus_value:bonus}])};
  let head=EquipmentService::save(&conn,input(None,"head","head",11)).unwrap();let chest=EquipmentService::save(&conn,input(None,"chest","chest",22)).unwrap();
  EquipmentService::toggle(&conn,&head).unwrap();EquipmentService::toggle(&conn,&chest).unwrap();
  EquipmentService::save(&conn,input(Some(chest.clone()),"chest","head",22)).unwrap();
  assert_eq!(EquipmentService::list(&conn).unwrap().iter().filter(|i|i.equipped).count(),1);
  assert_eq!(EquipmentService::snapshot(&conn).unwrap().total_bonuses[0].bonus_value,22);
  let c=CharacterService::get_default_character(&conn).unwrap();assert_eq!(CharacterService::get_attributes(&conn,&c.id).unwrap().iter().find(|a|a.attribute_key=="mind").unwrap().base_value,22);
  conn.execute("INSERT INTO CharacterEquipment(Id,CharacterId,EquipmentDefinitionId,Status,EquippedAt,CreatedAt,UpdatedAt)VALUES('duplicate',?1,?2,'Equipped','2000','2000','2000')",params![c.id,head]).unwrap();
  assert_eq!(EquipmentService::conflicts(&conn).unwrap().len(),2);
  assert_eq!(EquipmentService::snapshot(&conn).unwrap().total_bonuses[0].bonus_value,22);
  assert_eq!(EquipmentService::repair(&conn).unwrap(),1);
 }
}
