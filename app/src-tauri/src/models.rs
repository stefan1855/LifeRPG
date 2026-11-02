use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Character {
    pub id: String,
    pub name: String,
    pub description: Option<String>,
    pub avatar_image_path: Option<String>,
    pub avatar_fit: String,
    pub level: i32,
    pub total_experience: i32,
    pub created_at: String,
    pub last_growth_at: Option<String>,
    pub updated_at: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct CharacterAttribute {
    pub id: String,
    pub character_id: String,
    pub attribute_key: String,
    pub display_name: String,
    pub level: i32,
    pub experience: i32,
    pub base_value: i32,
    pub created_at: String,
    pub updated_at: String,
    // 计算字段：当前等级内进度及升级所需
    pub exp_into_current_level: Option<i32>,
    pub exp_to_next_level: Option<i32>,
    pub exp_span: Option<i32>,
    pub progress_percent: Option<f64>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AttributeLog {
    pub id: String,
    pub character_id: String,
    pub attribute_key: String,
    pub experience_delta: i32,
    pub source_type: String,
    pub source_id: String,
    pub reason: String,
    pub created_at: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ActionTask {
    pub id: String,
    pub title: String,
    pub content: String,
    pub category: String,
    pub status: String,
    pub quadrant: String,
    pub growth_attribute: String,
    pub experience: i32,
    pub due_at: Option<String>,
    pub completed_at: Option<String>,
    pub completion_note: String,
    pub linked_life_record_id: Option<String>,
    pub created_at: String,
    pub updated_at: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct RpgLevelResult {
    pub level: i32,
    pub total_experience: i32,
    pub experience_into_current_level: i32,
    pub experience_required_for_current_level: i32,
    pub experience_required_for_next_level: i32,
    pub experience_to_next_level: i32,
    pub progress_percent: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct RpgAttributeGain {
    pub attribute_key: String,
    pub display_name: String,
    pub experience_delta: i32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct RpgExperienceResult {
    pub applied: bool,
    pub is_duplicate: bool,
    pub total_experience_granted: i32,
    pub gains: Vec<RpgAttributeGain>,
    pub new_level: i32,
    pub leveled_up: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DashboardSnapshot {
    pub character: Character,
    pub level_info: RpgLevelResult,
    pub attributes: Vec<CharacterAttribute>,
    pub today_completed_count: i32,
    pub today_total_count: i32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct CreateTaskInput {
    pub title: String,
    pub content: Option<String>,
    pub category: Option<String>,
    pub quadrant: Option<String>,
    pub growth_attribute: Option<String>,
    pub experience: Option<i32>,
    pub due_at: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UpdateTaskInput {
    pub id: String,
    pub title: String,
    pub content: Option<String>,
    pub category: Option<String>,
    pub quadrant: Option<String>,
    pub growth_attribute: Option<String>,
    pub experience: Option<i32>,
    pub due_at: Option<String>,
}

// ============================ 行为记录 ============================

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct LifeRecordImage {
    pub id: String,
    pub life_record_id: String,
    pub file_name: String,
    pub file_path: String,
    pub created_at: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct LifeRecord {
    pub id: String,
    pub title: String,
    pub content: String,
    pub category: String,
    pub experience: i32,
    pub created_at: String,
    pub updated_at: String,
    pub images: Vec<LifeRecordImage>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct CreateRecordInput {
    pub title: String,
    pub content: Option<String>,
    pub category: Option<String>,
    pub experience: Option<i32>,
    /// 待入库的源图片绝对路径（由前端文件选择器给出）
    pub image_paths: Option<Vec<String>>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UpdateRecordInput {
    pub id: String,
    pub title: String,
    pub content: Option<String>,
    pub category: Option<String>,
    pub experience: Option<i32>,
    /// 新增图片
    pub add_image_paths: Option<Vec<String>>,
    /// 要移除的图片记录 Id
    pub remove_image_ids: Option<Vec<String>>,
}

// ============================ 统计与归档 ============================

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DailyActivity {
    pub date: String,
    pub task_count: i32,
    pub record_count: i32,
    pub experience: i32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ArchiveStats {
    pub total_tasks: i32,
    pub completed_tasks: i32,
    pub pending_tasks: i32,
    pub overdue_tasks: i32,
    pub total_records: i32,
    pub total_images: i32,
    pub total_experience: i32,
    pub streak_days: i32,
    pub daily: Vec<DailyActivity>,
    pub category_breakdown: Vec<CategoryStat>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct CategoryStat {
    pub name: String,
    pub count: i32,
    pub experience: i32,
}

// ============================ 备份 ============================

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct BackupInfo {
    pub file_name: String,
    pub file_path: String,
    pub size_bytes: u64,
    pub created_at: String,
}

// ============================ 装备 ============================

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct EquipmentBonus {
    pub attribute_key: String,
    pub bonus_value: i32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct EquipmentItem {
    pub id: String,
    pub real_item_name: String,
    pub game_item_name: String,
    /// 槽位 key：head/shoulder/chest/hands/waist/legs/feet/wrist/mainhand/offhand/trinket1/trinket2
    pub category: String,
    pub status: String,
    pub real_effect: String,
    pub created_at: String,
    pub updated_at: String,
    pub bonuses: Vec<EquipmentBonus>,
    pub equipped: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct EquipmentSlot {
    pub key: String,
    pub label: String,
    pub item: Option<EquipmentItem>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct EquipmentSnapshot {
    pub slots: Vec<EquipmentSlot>,
    /// 已装备物品带来的属性加成合计
    pub total_bonuses: Vec<EquipmentBonus>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SaveEquipmentInput {
    pub id: Option<String>,
    pub real_item_name: String,
    pub game_item_name: String,
    pub category: String,
    pub real_effect: Option<String>,
    pub bonuses: Option<Vec<EquipmentBonus>>,
}

// ============================ 技能 ============================

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SkillItem {
    pub id: String,
    pub name: String,
    pub category: String,
    pub description: String,
    pub level: i32,
    pub experience: i32,
    pub is_unlocked: bool,
    pub created_at: String,
    pub updated_at: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SaveSkillInput {
    pub id: Option<String>,
    pub name: String,
    pub category: String,
    pub description: Option<String>,
}

// ============================ NPC ============================

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct NpcItem {
    pub id: String,
    pub name: String,
    pub real_identity: String,
    pub category: String,
    pub influence_type: String,
    pub linked_domain: String,
    pub relationship_value: i32,
    pub last_interaction_at: Option<String>,
    pub created_at: String,
    pub updated_at: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SaveNpcInput {
    pub id: Option<String>,
    pub name: String,
    pub real_identity: Option<String>,
    pub category: Option<String>,
    pub influence_type: Option<String>,
    pub linked_domain: Option<String>,
}

// ============================ 地图 ============================

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct MapLocationItem {
    pub id: String,
    pub name: String,
    pub location_type: String,
    pub description: String,
    pub is_unlocked: bool,
    pub created_at: String,
    pub updated_at: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SaveMapInput {
    pub id: Option<String>,
    pub name: String,
    pub location_type: Option<String>,
    pub description: Option<String>,
}

// ============================ 通用 MCP 协议模型 ============================

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct McpServerConfig {
    pub id: String,
    pub name: String,
    pub command: String,
    #[serde(default)]
    pub args: Vec<String>,
    #[serde(default)]
    pub env: std::collections::HashMap<String, String>,
    #[serde(default = "default_true")]
    pub enabled: bool,
}

fn default_true() -> bool {
    true
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct McpToolDefinition {
    pub server_id: String,
    pub server_name: String,
    pub name: String,
    pub description: Option<String>,
    pub input_schema: serde_json::Value,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct McpToolCallResult {
    pub is_error: bool,
    pub content: String,
}
