use rusqlite::{params, Connection};
use uuid::Uuid;

use crate::models::{MapLocationItem, NpcItem, SaveMapInput, SaveNpcInput};
use crate::services::character_service::CharacterService;

/// 世界服务：NPC 关系 + 地图解锁
pub struct WorldService;

impl WorldService {
    fn now() -> String {
        chrono::Local::now().to_rfc3339()
    }

    // ---------------- NPC ----------------

    pub fn npc_list(conn: &Connection) -> rusqlite::Result<Vec<NpcItem>> {
        let character = CharacterService::get_default_character(conn)?;
        let mut stmt = conn.prepare(
            "SELECT nd.Id, nd.Name, COALESCE(nd.RealIdentity,''), COALESCE(nd.Category,''),
                    COALESCE(nd.InfluenceType,''), COALESCE(nd.LinkedDomain,''),
                    COALESCE(nr.RelationshipValue, 0), nr.LastInteractionAt,
                    nd.CreatedAt, nd.UpdatedAt
             FROM NpcDefinitions nd
             LEFT JOIN NpcRelationships nr
               ON nr.NpcDefinitionId = nd.Id AND nr.CharacterId = ?1
             ORDER BY COALESCE(nr.RelationshipValue, 0) DESC, nd.Name",
        )?;
        let rows = stmt.query_map(params![&character.id], |row| {
            Ok(NpcItem {
                id: row.get(0)?,
                name: row.get(1)?,
                real_identity: row.get(2)?,
                category: row.get(3)?,
                influence_type: row.get(4)?,
                linked_domain: row.get(5)?,
                relationship_value: row.get(6)?,
                last_interaction_at: row.get(7)?,
                created_at: row.get(8)?,
                updated_at: row.get(9)?,
            })
        })?;
        rows.collect()
    }

    pub fn npc_save(conn: &Connection, input: SaveNpcInput) -> Result<String, String> {
        let now = Self::now();
        let real_identity = input.real_identity.unwrap_or_default();
        let category = input.category.unwrap_or_else(|| "普通".into());
        let influence = input.influence_type.unwrap_or_else(|| "中性".into());
        let domain = input.linked_domain.unwrap_or_default();

        match input.id {
            Some(id) if !id.is_empty() => {
                conn.execute(
                    "UPDATE NpcDefinitions SET Name=?1, RealIdentity=?2, Category=?3, InfluenceType=?4, LinkedDomain=?5, UpdatedAt=?6 WHERE Id=?7",
                    params![&input.name, &real_identity, &category, &influence, &domain, &now, &id],
                )
                .map_err(|e| e.to_string())?;
                Ok(id)
            }
            _ => {
                let id = Uuid::new_v4().to_string();
                conn.execute(
                    "INSERT INTO NpcDefinitions (Id, Name, RealIdentity, Category, InfluenceType, LinkedDomain, CreatedAt, UpdatedAt)
                     VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?7)",
                    params![&id, &input.name, &real_identity, &category, &influence, &domain, &now],
                )
                .map_err(|e| e.to_string())?;
                Ok(id)
            }
        }
    }

    /// 调整好感度（可正可负，范围 -100 ~ 100）
    pub fn npc_adjust_relationship(conn: &Connection, npc_id: &str, delta: i32) -> Result<i32, String> {
        let character = CharacterService::get_default_character(conn).map_err(|e| e.to_string())?;
        let now = Self::now();

        let existing: Option<(String, i32)> = conn
            .query_row(
                "SELECT Id, RelationshipValue FROM NpcRelationships WHERE CharacterId=?1 AND NpcDefinitionId=?2",
                params![&character.id, npc_id],
                |r| Ok((r.get(0)?, r.get(1)?)),
            )
            .ok();

        let new_value = match existing {
            Some((row_id, current)) => {
                let v = (current + delta).clamp(-100, 100);
                conn.execute(
                    "UPDATE NpcRelationships SET RelationshipValue=?1, LastInteractionAt=?2 WHERE Id=?3",
                    params![v, &now, &row_id],
                )
                .map_err(|e| e.to_string())?;
                v
            }
            None => {
                let v = delta.clamp(-100, 100);
                conn.execute(
                    "INSERT INTO NpcRelationships (Id, CharacterId, NpcDefinitionId, RelationshipValue, LastInteractionAt, CreatedAt, UpdatedAt)
                     VALUES (?1, ?2, ?3, ?4, ?5, ?5, ?5)",
                    params![Uuid::new_v4().to_string(), &character.id, npc_id, v, &now],
                )
                .map_err(|e| e.to_string())?;
                v
            }
        };
        Ok(new_value)
    }

    pub fn npc_delete(conn: &Connection, npc_id: &str) -> Result<bool, String> {
        conn.execute(
            "DELETE FROM NpcRelationships WHERE NpcDefinitionId = ?1",
            params![npc_id],
        )
        .map_err(|e| e.to_string())?;
        let n = conn
            .execute("DELETE FROM NpcDefinitions WHERE Id = ?1", params![npc_id])
            .map_err(|e| e.to_string())?;
        Ok(n > 0)
    }

    // ---------------- 地图 ----------------

    pub fn map_list(conn: &Connection) -> rusqlite::Result<Vec<MapLocationItem>> {
        let mut stmt = conn.prepare(
            "SELECT Id, Name, COALESCE(LocationType,''), COALESCE(Description,''), IsUnlocked, CreatedAt, UpdatedAt
             FROM MapLocations ORDER BY IsUnlocked DESC, Name",
        )?;
        let rows = stmt.query_map([], |row| {
            Ok(MapLocationItem {
                id: row.get(0)?,
                name: row.get(1)?,
                location_type: row.get(2)?,
                description: row.get(3)?,
                is_unlocked: row.get::<_, i64>(4)? > 0,
                created_at: row.get(5)?,
                updated_at: row.get(6)?,
            })
        })?;
        rows.collect()
    }

    pub fn map_save(conn: &Connection, input: SaveMapInput) -> Result<String, String> {
        let now = Self::now();
        let location_type = input.location_type.unwrap_or_else(|| "区域".into());
        let description = input.description.unwrap_or_default();
        match input.id {
            Some(id) if !id.is_empty() => {
                conn.execute(
                    "UPDATE MapLocations SET Name=?1, LocationType=?2, Description=?3, UpdatedAt=?4 WHERE Id=?5",
                    params![&input.name, &location_type, &description, &now, &id],
                )
                .map_err(|e| e.to_string())?;
                Ok(id)
            }
            _ => {
                let id = Uuid::new_v4().to_string();
                conn.execute(
                    "INSERT INTO MapLocations (Id, Name, LocationType, Description, IsUnlocked, CreatedAt, UpdatedAt)
                     VALUES (?1, ?2, ?3, ?4, 0, ?5, ?5)",
                    params![&id, &input.name, &location_type, &description, &now],
                )
                .map_err(|e| e.to_string())?;
                Ok(id)
            }
        }
    }

    pub fn map_toggle(conn: &Connection, location_id: &str) -> Result<bool, String> {
        let current: i64 = conn
            .query_row(
                "SELECT IsUnlocked FROM MapLocations WHERE Id = ?1",
                params![location_id],
                |r| r.get(0),
            )
            .map_err(|_| "地点不存在".to_string())?;
        let next = if current > 0 { 0 } else { 1 };
        let now = Self::now();
        conn.execute(
            "UPDATE MapLocations SET IsUnlocked = ?1, UpdatedAt = ?2 WHERE Id = ?3",
            params![next, &now, location_id],
        )
        .map_err(|e| e.to_string())?;
        Ok(next == 1)
    }

    pub fn map_delete(conn: &Connection, location_id: &str) -> Result<bool, String> {
        let n = conn
            .execute("DELETE FROM MapLocations WHERE Id = ?1", params![location_id])
            .map_err(|e| e.to_string())?;
        Ok(n > 0)
    }
}
