using System;
using System.Collections.Generic;
using UnityEngine;

namespace LifeRPG
{
    /// <summary>
    /// 游戏核心管理器 —— 单例 MonoBehaviour
    /// 持有所有运行时数据，协调经验获取、等级计算、活动记录等核心逻辑
    ///
    /// 挂载到场景中的持久化 GameObject 上（如 "GameManager"）
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── 单例 ──
        public static GameManager Instance { get; private set; }

        // ── 核心数据引用 ──
        private DatabaseManager _db;

        /// <summary>角色数据（运行时）</summary>
        public CharacterData Character { get; private set; }

        /// <summary>5个契约节点（运行时）</summary>
        public List<ContractNode> Nodes { get; private set; }

        /// <summary>装备槽位列表</summary>
        public List<EquipmentSlot> EquipmentSlots { get; private set; }

        /// <summary>技能列表</summary>
        public List<SkillEntry> Skills { get; private set; }

        // ═══════════════════════════════════════════════════════════════
        //  生命周期
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // 单例模式 —— 确保全局唯一，场景切换时不销毁
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化数据库并加载数据
            _db = DatabaseManager.Instance;
            _db.InitializeDatabase();

            LoadAllData();

            Debug.Log("[GameManager] 初始化完成 —— 暗影契约已就绪");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                _db?.Shutdown();
                Instance = null;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  数据加载
        // ═══════════════════════════════════════════════════════════════

        /// <summary>从数据库加载所有数据到运行时缓存</summary>
        private void LoadAllData()
        {
            Character = _db.GetCharacter();
            if (Character == null)
            {
                Character = new CharacterData();
                Debug.LogWarning("[GameManager] 未找到角色数据，使用默认值");
            }

            Nodes = _db.GetAllNodes();
            if (Nodes == null || Nodes.Count == 0)
            {
                Debug.LogWarning("[GameManager] 未找到契约节点数据");
            }

            EquipmentSlots = _db.GetAllSlots();
            Skills = _db.GetAllSkills();

            Debug.Log($"[GameManager] 数据加载完成 —— 角色: {Character?.CharacterName}, 节点: {Nodes?.Count}, 技能: {Skills?.Count}");
        }

        /// <summary>强制从数据库重新加载所有数据</summary>
        public void ReloadAllData()
        {
            LoadAllData();
        }

        // ═══════════════════════════════════════════════════════════════
        //  核心逻辑
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 给指定契约节点增加经验值，并自动记录活动日志
        /// 同时触发总等级重算和数据库持久化
        /// </summary>
        /// <param name="nodeTag">节点标签（如 "ShadowFlesh"）</param>
        /// <param name="amount">获得的经验值</param>
        /// <param name="title">活动标题（简短描述）</param>
        /// <param name="description">活动详细描述</param>
        /// <returns>授予经验后是否触发了升级</returns>
        public bool AddExp(string nodeTag, int amount, string title, string description)
        {
            if (string.IsNullOrEmpty(nodeTag) || amount <= 0)
                return false;

            // 1. 找到对应的节点
            ContractNode targetNode = null;
            if (Nodes != null)
            {
                foreach (var node in Nodes)
                {
                    if (node.NodeId == nodeTag)
                    {
                        targetNode = node;
                        break;
                    }
                }
            }

            if (targetNode == null)
            {
                Debug.LogWarning($"[GameManager] 未找到节点: {nodeTag}");
                return false;
            }

            // 2. 给节点加经验（内部处理升级逻辑）
            bool leveledUp = targetNode.AddExp(amount);

            // 3. 记录活动日志
            var activity = new ActivityLog
            {
                Timestamp = DateTime.Now,
                ContractNodeTag = nodeTag,
                ExpGained = amount,
                Title = title,
                Description = description
            };
            _db.InsertActivity(activity);

            // 4. 持久化节点数据
            _db.UpdateNode(targetNode);

            // 5. 重新计算总等级
            RecalculateLevel();

            // 6. 更新角色数据到数据库
            if (Character != null)
                _db.UpdateCharacter(Character);

            if (leveledUp)
                Debug.Log($"[GameManager] 🎉 {targetNode.NodeName} 升级！当前等级: Lv.{targetNode.Level}");

            Debug.Log($"[GameManager] +{amount} EXP → {targetNode.NodeName} | {title}");

            return leveledUp;
        }

        /// <summary>
        /// 根据5个契约节点的等级重新计算总等级和总经验值
        ///
        /// 规则:
        ///   - 总等级 = 5个节点等级之和
        ///   - 总经验 = 5个节点经验之和
        ///   - 升级所需经验 = 总等级 × 200（随等级增长）
        /// </summary>
        public void RecalculateLevel()
        {
            if (Nodes == null || Nodes.Count == 0 || Character == null)
                return;

            int totalLevel = 0;
            int totalExp = 0;

            foreach (var node in Nodes)
            {
                totalLevel += node.Level;
                totalExp += node.Exp;
            }

            Character.TotalLevel = totalLevel;
            Character.TotalExp = totalExp;

            // 升级所需经验随总等级动态增长
            Character.ExpToNextLevel = totalLevel * 200;

            Character.LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// 获取最近的 N 条活动日志
        /// </summary>
        /// <param name="count">获取数量，默认20</param>
        public List<ActivityLog> GetRecentActivities(int count = 20)
        {
            return _db.GetRecentActivities(count);
        }

        /// <summary>
        /// 获取指定节点的最近活动
        /// </summary>
        public List<ActivityLog> GetNodeActivities(string nodeTag, int count = 10)
        {
            return _db.GetActivitiesByNode(nodeTag, count);
        }

        // ═══════════════════════════════════════════════════════════════
        //  节点快捷访问
        // ═══════════════════════════════════════════════════════════════

        /// <summary>根据 nodeId 获取节点引用</summary>
        public ContractNode GetNode(string nodeId)
        {
            if (Nodes == null) return null;
            return Nodes.Find(n => n.NodeId == nodeId);
        }

        /// <summary>暗影血肉节点（体能健康）</summary>
        public ContractNode ShadowFlesh => GetNode(ContractNode.TAG_SHADOW_FLESH);

        /// <summary>邪能造物节点（事业产出）</summary>
        public ContractNode FelCreation => GetNode(ContractNode.TAG_FEL_CREATION);

        /// <summary>虚空之智节点（学习知识）</summary>
        public ContractNode VoidWisdom => GetNode(ContractNode.TAG_VOID_WISDOM);

        /// <summary>灵魂羁绊节点（人际关系）</summary>
        public ContractNode SoulBond => GetNode(ContractNode.TAG_SOUL_BOND);

        /// <summary>不灭意志节点（精神意志）</summary>
        public ContractNode EternalWill => GetNode(ContractNode.TAG_ETERNAL_WILL);

        // ═══════════════════════════════════════════════════════════════
        //  装备槽位操作
        // ═══════════════════════════════════════════════════════════════

        /// <summary>装备物品到指定槽位</summary>
        public void EquipItem(string slotName, string itemName)
        {
            var slot = EquipmentSlots?.Find(s => s.SlotName == slotName);
            if (slot == null || slot.IsLocked) return;

            slot.EquippedItemName = itemName;
            _db.UpdateSlot(slot);
            Debug.Log($"[GameManager] 装备: {itemName} → {slotName}");
        }

        /// <summary>卸下指定槽位的装备</summary>
        public void UnequipSlot(string slotName)
        {
            EquipItem(slotName, "");
        }

        /// <summary>锁定/解锁槽位</summary>
        public void SetSlotLocked(string slotName, bool locked)
        {
            var slot = EquipmentSlots?.Find(s => s.SlotName == slotName);
            if (slot == null) return;

            slot.IsLocked = locked;
            _db.UpdateSlot(slot);
        }

        // ═══════════════════════════════════════════════════════════════
        //  技能操作
        // ═══════════════════════════════════════════════════════════════

        /// <summary>解锁技能等级</summary>
        public void UnlockSkillLevel(int skillId, int count = 1)
        {
            var skill = Skills?.Find(s => s.Id == skillId);
            if (skill == null) return;

            skill.UnlockedCount = Math.Min(skill.UnlockedCount + count, skill.TotalCount);
            _db.UpdateSkill(skill);
        }

        /// <summary>添加新技能</summary>
        public void AddSkill(SkillEntry skill)
        {
            _db.InsertSkill(skill);
            Skills.Add(skill);
        }

        // ═══════════════════════════════════════════════════════════════
        //  角色操作
        // ═══════════════════════════════════════════════════════════════

        /// <summary>更新角色名称</summary>
        public void SetCharacterName(string name)
        {
            if (Character == null || string.IsNullOrEmpty(name)) return;
            Character.CharacterName = name;
            Character.LastUpdated = DateTime.Now;
            _db.UpdateCharacter(Character);
        }

        /// <summary>更新角色称号</summary>
        public void SetCharacterTitle(string title)
        {
            if (Character == null || string.IsNullOrEmpty(title)) return;
            Character.Title = title;
            Character.LastUpdated = DateTime.Now;
            _db.UpdateCharacter(Character);
        }
    }
}
