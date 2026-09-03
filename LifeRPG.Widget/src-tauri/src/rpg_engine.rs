use crate::models::{RpgAttributeGain, RpgLevelResult};

pub const DEFAULT_ATTRIBUTES: &[(&str, &str)] = &[
    ("physique", "体魄"),
    ("mind", "心智"),
    ("discipline", "纪律"),
    ("expression", "表达"),
    ("creativity", "创造"),
    ("resources", "资源"),
];

pub struct RpgEngine;

impl RpgEngine {
    /// 计算总经验对应的等级及进度
    pub fn calculate_level(total_experience: i32) -> RpgLevelResult {
        let normalized = total_experience.max(0);
        let mut level = 1;
        let mut exp_req_current = 0;
        let mut exp_req_next = 100;

        while normalized >= exp_req_next {
            level += 1;
            exp_req_current = exp_req_next;
            exp_req_next += 100 * level;
        }

        let exp_into_current = normalized - exp_req_current;
        let span = (exp_req_next - exp_req_current).max(1);
        let exp_to_next = (exp_req_next - normalized).max(0);
        let progress_percent = ((exp_into_current as f64 / span as f64) * 100.0).clamp(0.0, 100.0);

        RpgLevelResult {
            level,
            total_experience: normalized,
            experience_into_current_level: exp_into_current,
            experience_required_for_current_level: exp_req_current,
            experience_required_for_next_level: exp_req_next,
            experience_to_next_level: exp_to_next,
            progress_percent,
        }
    }

    /// 标准化属性键（移除非字母数字，转小写）
    pub fn normalize_attribute_key(raw: &str) -> String {
        let trimmed = raw.trim();
        if trimmed.is_empty() {
            return "general".to_string();
        }

        // 优先匹配预设六大属性的中文名与英文名
        for (key, display) in DEFAULT_ATTRIBUTES {
            if trimmed.eq_ignore_ascii_case(key) || trimmed.eq_ignore_ascii_case(display) {
                return key.to_string();
            }
        }

        let filtered: String = trimmed
            .chars()
            .filter(|c| c.is_alphanumeric())
            .flat_map(|c| c.to_lowercase())
            .collect();

        if filtered.is_empty() {
            "general".to_string()
        } else {
            filtered
        }
    }

    /// 解析属性定义（键与显示名称）
    pub fn resolve_attribute(raw: Option<&str>) -> (String, String) {
        match raw {
            None => ("general".to_string(), "未设置".to_string()),
            Some(val) => {
                let trimmed = val.trim();
                if trimmed.is_empty() {
                    return ("general".to_string(), "未设置".to_string());
                }

                for (key, display) in DEFAULT_ATTRIBUTES {
                    if trimmed.eq_ignore_ascii_case(key) || trimmed.eq_ignore_ascii_case(display) {
                        return (key.to_string(), display.to_string());
                    }
                }

                let key = Self::normalize_attribute_key(trimmed);
                (key, trimmed.to_string())
            }
        }
    }

    /// 根据任务设定分配属性经验
    pub fn allocate_task_experience(
        task_experience: i32,
        growth_attribute: Option<&str>,
    ) -> Vec<RpgAttributeGain> {
        let normalized_exp = task_experience.max(0);
        let (key, display_name) = Self::resolve_attribute(growth_attribute);

        vec![RpgAttributeGain {
            attribute_key: key,
            display_name,
            experience_delta: normalized_exp,
        }]
    }

    /// 根据四象限计算默认经验值
    pub fn calculate_quadrant_experience(quadrant: Option<&str>) -> i32 {
        match quadrant.map(|s| s.trim()).unwrap_or("") {
            "Q1" | "ImportantUrgent" | "重要紧急" => 50,
            "Q2" | "ImportantNotUrgent" | "重要不紧急" => 40,
            "Q3" | "NotImportantUrgent" | "不重要紧急" => 20,
            "Q4" | "NotImportantNotUrgent" | "不重要不紧急" => 10,
            _ => 30,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_calculate_level_progression() {
        let lv1 = RpgEngine::calculate_level(0);
        assert_eq!(lv1.level, 1);
        assert_eq!(lv1.experience_into_current_level, 0);
        assert_eq!(lv1.experience_to_next_level, 100);

        let lv1_50 = RpgEngine::calculate_level(50);
        assert_eq!(lv1_50.level, 1);
        assert_eq!(lv1_50.experience_into_current_level, 50);
        assert_eq!(lv1_50.experience_to_next_level, 50);
        assert!((lv1_50.progress_percent - 50.0).abs() < f64::EPSILON);

        // 100 EXP = Level 2 (needs 200 more to reach 300)
        let lv2 = RpgEngine::calculate_level(100);
        assert_eq!(lv2.level, 2);
        assert_eq!(lv2.experience_into_current_level, 0);
        assert_eq!(lv2.experience_to_next_level, 200);

        // 300 EXP = Level 3 (needs 300 more to reach 600)
        let lv3 = RpgEngine::calculate_level(300);
        assert_eq!(lv3.level, 3);
        assert_eq!(lv3.experience_into_current_level, 0);
        assert_eq!(lv3.experience_to_next_level, 300);
    }

    #[test]
    fn test_resolve_attribute() {
        assert_eq!(
            RpgEngine::resolve_attribute(Some("体魄")),
            ("physique".to_string(), "体魄".to_string())
        );
        assert_eq!(
            RpgEngine::resolve_attribute(Some("mind")),
            ("mind".to_string(), "心智".to_string())
        );
        assert_eq!(
            RpgEngine::resolve_attribute(None),
            ("general".to_string(), "未设置".to_string())
        );
    }
}
