using System;

namespace LifeRPG
{
    /// <summary>
    /// 技能条目 —— 技能树中的单个技能
    /// 映射到 SQLite skills 表
    /// </summary>
    [Serializable]
    public class SkillEntry
    {
        /// <summary>主键</summary>
        public int Id { get; set; }

        /// <summary>技能名称</summary>
        public string SkillName { get; set; }

        /// <summary>技能描述</summary>
        public string Description { get; set; }

        /// <summary>已解锁的技能等级数</summary>
        public int UnlockedCount { get; set; }

        /// <summary>技能总等级数上限</summary>
        public int TotalCount { get; set; }

        /// <summary>是否可展开查看下级技能</summary>
        public bool IsExpandable { get; set; }

        /// <summary>默认构造函数</summary>
        public SkillEntry() { }

        /// <summary>全参数构造函数</summary>
        public SkillEntry(int id, string skillName, string description,
            int unlockedCount, int totalCount, bool isExpandable)
        {
            Id = id;
            SkillName = skillName;
            Description = description;
            UnlockedCount = unlockedCount;
            TotalCount = totalCount;
            IsExpandable = isExpandable;
        }

        /// <summary>解锁进度百分比 (0.0 ~ 1.0)</summary>
        public float Progress => TotalCount > 0 ? (float)UnlockedCount / TotalCount : 0f;

        /// <summary>是否已完全解锁</summary>
        public bool IsFullyUnlocked => UnlockedCount >= TotalCount && TotalCount > 0;
    }
}
