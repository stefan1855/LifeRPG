using System;

namespace LifeRPG
{
    /// <summary>
    /// 装备槽位 —— 角色身上8个装备位（头部/肩部/胸部/护腕/手套/腰带/腿部/靴子）
    /// 映射到 SQLite equipment_slots 表
    /// </summary>
    [Serializable]
    public class EquipmentSlot
    {
        // ── 预定义的8个槽位名称 ──
        public const string SLOT_HEAD = "头部";
        public const string SLOT_SHOULDERS = "肩部";
        public const string SLOT_CHEST = "胸部";
        public const string SLOT_BRACERS = "护腕";
        public const string SLOT_GLOVES = "手套";
        public const string SLOT_BELT = "腰带";
        public const string SLOT_LEGS = "腿部";
        public const string SLOT_BOOTS = "靴子";

        /// <summary>主键</summary>
        public int Id { get; set; }

        /// <summary>槽位名称（如 "头部"）</summary>
        public string SlotName { get; set; }

        /// <summary>当前装备的物品名称（为空表示未装备）</summary>
        public string EquippedItemName { get; set; }

        /// <summary>槽位是否已锁定</summary>
        public bool IsLocked { get; set; }

        /// <summary>图标资源名</summary>
        public string IconName { get; set; }

        /// <summary>默认构造函数</summary>
        public EquipmentSlot() { }

        /// <summary>全参数构造函数</summary>
        public EquipmentSlot(int id, string slotName, string equippedItemName,
            bool isLocked, string iconName)
        {
            Id = id;
            SlotName = slotName;
            EquippedItemName = equippedItemName;
            IsLocked = isLocked;
            IconName = iconName;
        }

        /// <summary>是否已装备物品</summary>
        public bool IsEquipped => !string.IsNullOrEmpty(EquippedItemName);
    }
}
