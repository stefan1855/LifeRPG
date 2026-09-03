use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Character {
    pub id: String,
    pub name: String,
    pub description: Option<String>,
    pub avatar_image_path: Option<String>,
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
