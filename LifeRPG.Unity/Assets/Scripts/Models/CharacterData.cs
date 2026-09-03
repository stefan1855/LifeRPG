using System;

namespace LifeRPG
{
    /// <summary>
    /// 角色数据模型 —— 代表玩家的暗影契约者身份
    /// 映射到 SQLite character 表
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        /// <summary>主键</summary>
        public int Id { get; set; }

        /// <summary>角色名（默认 "无面者"）</summary>
        public string CharacterName { get; set; } = "无面者";

        /// <summary>称号（默认 "暗影学徒"）</summary>
        public string Title { get; set; } = "暗影学徒";

        /// <summary>总等级 —— 由5个契约节点等级综合计算得出</summary>
        public int TotalLevel { get; set; } = 5;

        /// <summary>总经验值</summary>
        public int TotalExp { get; set; }

        /// <summary>升级所需经验值</summary>
        public int ExpToNextLevel { get; set; } = 1000;

        /// <summary>角色描述 / 简介文字</summary>
        public string Description { get; set; } = "在阴影中行走，聆听灵魂的低语...";

        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>最后更新时间</summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>默认构造函数</summary>
        public CharacterData() { }

        /// <summary>从数据库行构造</summary>
        public CharacterData(int id, string characterName, string title, int totalLevel,
            int totalExp, int expToNextLevel, string description,
            DateTime createdAt, DateTime lastUpdated)
        {
            Id = id;
            CharacterName = characterName;
            Title = title;
            TotalLevel = totalLevel;
            TotalExp = totalExp;
            ExpToNextLevel = expToNextLevel;
            Description = description;
            CreatedAt = createdAt;
            LastUpdated = lastUpdated;
        }
    }
}
