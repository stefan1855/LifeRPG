using System;

namespace LifeRPG
{
    /// <summary>
    /// 暗影契约节点 —— 将5个现实维度映射为RPG天赋树节点
    /// 映射到 SQLite contract_nodes 表
    /// </summary>
    [Serializable]
    public class ContractNode
    {
        // ── 预定义的5个节点标签常量 ──
        public const string TAG_SHADOW_FLESH = "ShadowFlesh";    // 暗影血肉 → 体能健康
        public const string TAG_FEL_CREATION = "FelCreation";    // 邪能造物 → 事业产出
        public const string TAG_VOID_WISDOM = "VoidWisdom";      // 虚空之智 → 学习知识
        public const string TAG_SOUL_BOND = "SoulBond";          // 灵魂羁绊 → 人际关系
        public const string TAG_ETERNAL_WILL = "EternalWill";    // 不灭意志 → 精神意志

        /// <summary>主键</summary>
        public int Id { get; set; }

        /// <summary>节点唯一标识符（如 "ShadowFlesh"）</summary>
        public string NodeId { get; set; }

        /// <summary>节点名称（如 "暗影血肉"）</summary>
        public string NodeName { get; set; }

        /// <summary>现实维度名称（如 "体能健康"）</summary>
        public string DimensionName { get; set; }

        /// <summary>当前等级</summary>
        public int Level { get; set; } = 1;

        /// <summary>当前累积经验值</summary>
        public int Exp { get; set; }

        /// <summary>升至下一级所需经验</summary>
        public int MaxExp { get; set; } = 100;

        /// <summary>图标资源名</summary>
        public string IconName { get; set; }

        /// <summary>节点描述</summary>
        public string Description { get; set; }

        /// <summary>默认构造函数</summary>
        public ContractNode() { }

        /// <summary>全参数构造函数</summary>
        public ContractNode(int id, string nodeId, string nodeName, string dimensionName,
            int level, int exp, int maxExp, string iconName, string description)
        {
            Id = id;
            NodeId = nodeId;
            NodeName = nodeName;
            DimensionName = dimensionName;
            Level = level;
            Exp = exp;
            MaxExp = maxExp;
            IconName = iconName;
            Description = description;
        }

        /// <summary>
        /// 为该节点增加经验值，自动处理升级逻辑
        /// </summary>
        /// <param name="amount">获得的经验值</param>
        /// <returns>是否触发了升级</returns>
        public bool AddExp(int amount)
        {
            if (amount <= 0) return false;

            Exp += amount;
            bool leveledUp = false;

            // 循环升级 —— 溢出的经验带入下一级
            while (Exp >= MaxExp)
            {
                Exp -= MaxExp;
                Level++;
                // 每升一级所需经验递增 50%
                MaxExp = (int)(MaxExp * 1.5f);
                leveledUp = true;
            }

            return leveledUp;
        }

        /// <summary>经验百分比 (0.0 ~ 1.0)，用于进度条显示</summary>
        public float ExpProgress => MaxExp > 0 ? (float)Exp / MaxExp : 0f;
    }
}
