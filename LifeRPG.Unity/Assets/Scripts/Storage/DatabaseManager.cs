using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Unity 2022 LTS 使用 Mono 后端时，Mono.Data.Sqlite 可直接使用
// 若切换 IL2CPP，需引入原生 sqlite3 插件，替换底层调用即可，接口不变
using Mono.Data.Sqlite;

namespace LifeRPG
{
    /// <summary>
    /// SQLite 数据库管理器 —— 单例模式
    /// 负责数据库初始化、建表，以及所有 CRUD 操作
    ///
    /// 表结构:
    ///   character       — 角色主数据（1行）
    ///   contract_nodes  — 5个暗影契约节点
    ///   activity_logs   — 活动日志
    ///   equipment_slots — 8个装备槽位
    ///   skills          — 技能列表
    /// </summary>
    public class DatabaseManager
    {
        // ── 单例 ──
        private static DatabaseManager _instance;
        private static readonly object _lock = new object();

        public static DatabaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new DatabaseManager();
                    }
                }
                return _instance;
            }
        }

        /// <summary>数据库文件路径</summary>
        private string _dbPath;

        /// <summary>数据库连接字符串</summary>
        private string _connectionString;

        /// <summary>数据库是否已初始化</summary>
        public bool IsInitialized { get; private set; }

        private DatabaseManager() { }

        /// <summary>
        /// 初始化数据库 —— 创建文件、建表、插入默认数据
        /// 在应用启动时调用一次
        /// </summary>
        public void InitializeDatabase()
        {
            if (IsInitialized) return;

            // 数据库文件存放在 persistentDataPath，确保读写权限
            _dbPath = Path.Combine(Application.persistentDataPath, "liferpg.db");
            _connectionString = $"URI=file:{_dbPath}";

            Debug.Log($"[DatabaseManager] 数据库路径: {_dbPath}");

            CreateTables();
            InsertDefaultData();

            IsInitialized = true;
            Debug.Log("[DatabaseManager] 数据库初始化完成");
        }

        /// <summary>
        /// 创建所有表（如果不存在）
        /// </summary>
        private void CreateTables()
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    // ── 角色表 ──
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS character (
                            id              INTEGER PRIMARY KEY AUTOINCREMENT,
                            name            TEXT    NOT NULL DEFAULT '无面者',
                            title           TEXT    NOT NULL DEFAULT '暗影学徒',
                            total_level     INTEGER NOT NULL DEFAULT 5,
                            total_exp       INTEGER NOT NULL DEFAULT 0,
                            exp_to_next     INTEGER NOT NULL DEFAULT 1000,
                            description     TEXT    DEFAULT '',
                            created_at      TEXT    NOT NULL,
                            last_updated    TEXT    NOT NULL
                        );";
                    cmd.ExecuteNonQuery();

                    // ── 契约节点表 ──
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS contract_nodes (
                            id              INTEGER PRIMARY KEY AUTOINCREMENT,
                            node_id         TEXT    NOT NULL UNIQUE,
                            node_name       TEXT    NOT NULL,
                            dimension_name  TEXT    NOT NULL,
                            level           INTEGER NOT NULL DEFAULT 1,
                            exp             INTEGER NOT NULL DEFAULT 0,
                            max_exp         INTEGER NOT NULL DEFAULT 100,
                            icon_name       TEXT    DEFAULT '',
                            description     TEXT    DEFAULT ''
                        );";
                    cmd.ExecuteNonQuery();

                    // ── 活动日志表 ──
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS activity_logs (
                            id              INTEGER PRIMARY KEY AUTOINCREMENT,
                            timestamp       TEXT    NOT NULL,
                            node_tag        TEXT    NOT NULL,
                            exp_gained      INTEGER NOT NULL DEFAULT 0,
                            title           TEXT    DEFAULT '',
                            description     TEXT    DEFAULT ''
                        );";
                    cmd.ExecuteNonQuery();

                    // ── 装备槽位表 ──
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS equipment_slots (
                            id              INTEGER PRIMARY KEY AUTOINCREMENT,
                            slot_name       TEXT    NOT NULL UNIQUE,
                            item_name       TEXT    DEFAULT '',
                            is_locked       INTEGER NOT NULL DEFAULT 0,
                            icon_name       TEXT    DEFAULT ''
                        );";
                    cmd.ExecuteNonQuery();

                    // ── 技能表 ──
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS skills (
                            id              INTEGER PRIMARY KEY AUTOINCREMENT,
                            skill_name      TEXT    NOT NULL,
                            description     TEXT    DEFAULT '',
                            unlocked_count  INTEGER NOT NULL DEFAULT 0,
                            total_count     INTEGER NOT NULL DEFAULT 1,
                            is_expandable   INTEGER NOT NULL DEFAULT 0
                        );";
                    cmd.ExecuteNonQuery();
                }

                conn.Close();
            }
        }

        /// <summary>
        /// 插入默认数据（仅当表为空时）
        /// 包括角色初始数据、5个契约节点、8个装备槽位
        /// </summary>
        private void InsertDefaultData()
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                // ── 插入默认角色（仅当无数据时） ──
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM character;";
                    long count = (long)cmd.ExecuteScalar();
                    if (count == 0)
                    {
                        string now = DateTime.Now.ToString("o");
                        cmd.CommandText = $@"
                            INSERT INTO character (name, title, total_level, total_exp, exp_to_next, description, created_at, last_updated)
                            VALUES ('无面者', '暗影学徒', 5, 0, 1000, '在阴影中行走，聆听灵魂的低语...', '{now}', '{now}');";
                        cmd.ExecuteNonQuery();
                        Debug.Log("[DatabaseManager] 已插入默认角色数据");
                    }
                }

                // ── 插入5个默认契约节点（仅当无数据时） ──
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM contract_nodes;";
                    long count = (long)cmd.ExecuteScalar();
                    if (count == 0)
                    {
                        InsertNode(cmd, "ShadowFlesh", "暗影血肉", "体能健康", "icon_flesh", "你的身体即是暗影的容器。通过锻炼与自律，让血肉更加强韧。");
                        InsertNode(cmd, "FelCreation", "邪能造物", "事业产出", "icon_fel", "以邪能之火锻造世间造物。你的工作、创作与产出皆源于此。");
                        InsertNode(cmd, "VoidWisdom", "虚空之智", "学习知识", "icon_void", "凝视虚空，虚空亦回望于你。阅读、学习、思考，汲取无尽知识。");
                        InsertNode(cmd, "SoulBond", "灵魂羁绊", "人际关系", "icon_soul", "每一条灵魂之线都是一份承诺。培养与他人的联结，缔结不解之缘。");
                        InsertNode(cmd, "EternalWill", "不灭意志", "精神意志", "icon_will", "意志不灭，则暗影永存。冥想、内省、坚持，锤炼你的精神。");
                        Debug.Log("[DatabaseManager] 已插入5个默认契约节点");
                    }
                }

                // ── 插入8个默认装备槽位（仅当无数据时） ──
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM equipment_slots;";
                    long count = (long)cmd.ExecuteScalar();
                    if (count == 0)
                    {
                        InsertSlot(cmd, "头部", "icon_head");
                        InsertSlot(cmd, "肩部", "icon_shoulders");
                        InsertSlot(cmd, "胸部", "icon_chest");
                        InsertSlot(cmd, "护腕", "icon_bracers");
                        InsertSlot(cmd, "手套", "icon_gloves");
                        InsertSlot(cmd, "腰带", "icon_belt");
                        InsertSlot(cmd, "腿部", "icon_legs");
                        InsertSlot(cmd, "靴子", "icon_boots");
                        Debug.Log("[DatabaseManager] 已插入8个默认装备槽位");
                    }
                }

                conn.Close();
            }
        }

        // ── 辅助插入方法 ──
        private void InsertNode(SqliteCommand cmd, string nodeId, string nodeName,
            string dimension, string icon, string desc)
        {
            cmd.CommandText = $@"
                INSERT INTO contract_nodes (node_id, node_name, dimension_name, level, exp, max_exp, icon_name, description)
                VALUES ('{Escape(nodeId)}', '{Escape(nodeName)}', '{Escape(dimension)}', 1, 0, 100, '{Escape(icon)}', '{Escape(desc)}');";
            cmd.ExecuteNonQuery();
        }

        private void InsertSlot(SqliteCommand cmd, string slotName, string icon)
        {
            cmd.CommandText = $@"
                INSERT INTO equipment_slots (slot_name, item_name, is_locked, icon_name)
                VALUES ('{Escape(slotName)}', '', 0, '{Escape(icon)}');";
            cmd.ExecuteNonQuery();
        }

        /// <summary>SQL 字符串转义（防注入）</summary>
        private string Escape(string input)
        {
            return input?.Replace("'", "''") ?? "";
        }

        // ═══════════════════════════════════════════════════════════════
        //  Character CRUD
        // ═══════════════════════════════════════════════════════════════

        /// <summary>获取角色数据（只有一行）</summary>
        public CharacterData GetCharacter()
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM character LIMIT 1;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new CharacterData(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetInt32(3),
                                reader.GetInt32(4),
                                reader.GetInt32(5),
                                reader.GetString(6),
                                DateTime.Parse(reader.GetString(7)),
                                DateTime.Parse(reader.GetString(8))
                            );
                        }
                    }
                }
                conn.Close();
            }
            return null;
        }

        /// <summary>更新角色数据</summary>
        public void UpdateCharacter(CharacterData data)
        {
            if (data == null) return;

            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    string now = DateTime.Now.ToString("o");
                    cmd.CommandText = $@"
                        UPDATE character SET
                            name        = '{Escape(data.CharacterName)}',
                            title       = '{Escape(data.Title)}',
                            total_level = {data.TotalLevel},
                            total_exp   = {data.TotalExp},
                            exp_to_next = {data.ExpToNextLevel},
                            description = '{Escape(data.Description)}',
                            last_updated = '{now}'
                        WHERE id = {data.Id};";
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  ContractNode CRUD
        // ═══════════════════════════════════════════════════════════════

        /// <summary>获取所有契约节点</summary>
        public List<ContractNode> GetAllNodes()
        {
            var nodes = new List<ContractNode>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM contract_nodes ORDER BY id;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nodes.Add(new ContractNode(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetString(3),
                                reader.GetInt32(4),
                                reader.GetInt32(5),
                                reader.GetInt32(6),
                                reader.GetString(7),
                                reader.GetString(8)
                            ));
                        }
                    }
                }
                conn.Close();
            }
            return nodes;
        }

        /// <summary>根据 node_id 获取单个节点</summary>
        public ContractNode GetNodeById(string nodeId)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT * FROM contract_nodes WHERE node_id = '{Escape(nodeId)}';";
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ContractNode(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetString(3),
                                reader.GetInt32(4),
                                reader.GetInt32(5),
                                reader.GetInt32(6),
                                reader.GetString(7),
                                reader.GetString(8)
                            );
                        }
                    }
                }
                conn.Close();
            }
            return null;
        }

        /// <summary>更新单个节点</summary>
        public void UpdateNode(ContractNode node)
        {
            if (node == null) return;

            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
                        UPDATE contract_nodes SET
                            level   = {node.Level},
                            exp     = {node.Exp},
                            max_exp = {node.MaxExp},
                            description = '{Escape(node.Description)}'
                        WHERE id = {node.Id};";
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }

        /// <summary>批量更新所有节点</summary>
        public void UpdateAllNodes(List<ContractNode> nodes)
        {
            if (nodes == null) return;

            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        foreach (var node in nodes)
                        {
                            cmd.CommandText = $@"
                                UPDATE contract_nodes SET
                                    level   = {node.Level},
                                    exp     = {node.Exp},
                                    max_exp = {node.MaxExp}
                                WHERE id = {node.Id};";
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
                conn.Close();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  ActivityLog CRUD
        // ═══════════════════════════════════════════════════════════════

        /// <summary>插入一条活动日志</summary>
        public void InsertActivity(ActivityLog activity)
        {
            if (activity == null) return;

            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    string ts = activity.Timestamp.ToString("o");
                    cmd.CommandText = $@"
                        INSERT INTO activity_logs (timestamp, node_tag, exp_gained, title, description)
                        VALUES ('{ts}', '{Escape(activity.ContractNodeTag)}', {activity.ExpGained},
                                '{Escape(activity.Title)}', '{Escape(activity.Description)}');";
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }

        /// <summary>获取最近的 N 条活动日志（按时间倒序）</summary>
        public List<ActivityLog> GetRecentActivities(int count = 20)
        {
            var logs = new List<ActivityLog>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT * FROM activity_logs ORDER BY timestamp DESC LIMIT {count};";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new ActivityLog(
                                reader.GetInt32(0),
                                DateTime.Parse(reader.GetString(1)),
                                reader.GetString(2),
                                reader.GetInt32(3),
                                reader.GetString(4),
                                reader.GetString(5)
                            ));
                        }
                    }
                }
                conn.Close();
            }
            return logs;
        }

        /// <summary>获取指定节点的最近活动</summary>
        public List<ActivityLog> GetActivitiesByNode(string nodeTag, int count = 10)
        {
            var logs = new List<ActivityLog>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
                        SELECT * FROM activity_logs
                        WHERE node_tag = '{Escape(nodeTag)}'
                        ORDER BY timestamp DESC LIMIT {count};";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new ActivityLog(
                                reader.GetInt32(0),
                                DateTime.Parse(reader.GetString(1)),
                                reader.GetString(2),
                                reader.GetInt32(3),
                                reader.GetString(4),
                                reader.GetString(5)
                            ));
                        }
                    }
                }
                conn.Close();
            }
            return logs;
        }

        /// <summary>删除一条活动日志</summary>
        public void DeleteActivity(int id)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"DELETE FROM activity_logs WHERE id = {id};";
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  EquipmentSlot CRUD
        // ═══════════════════════════════════════════════════════════════

        /// <summary>获取所有装备槽位</summary>
        public List<EquipmentSlot> GetAllSlots()
        {
            var slots = new List<EquipmentSlot>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM equipment_slots ORDER BY id;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            slots.Add(new EquipmentSlot(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetInt32(3) == 1,
                                reader.GetString(4)
                            ));
                        }
                    }
                }
                conn.Close();
            }
            return slots;
        }

        /// <summary>更新单个装备槽位</summary>
        public void UpdateSlot(EquipmentSlot slot)
        {
            if (slot == null) return;

            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
                        UPDATE equipment_slots SET
                            item_name = '{Escape(slot.EquippedItemName)}',
                            is_locked = {(slot.IsLocked ? 1 : 0)},
                            icon_name = '{Escape(slot.IconName)}'
                        WHERE id = {slot.Id};";
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  SkillEntry CRUD
        // ═══════════════════════════════════════════════════════════════

        /// <summary>获取所有技能</summary>
        public List<SkillEntry> GetAllSkills()
        {
            var skills = new List<SkillEntry>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM skills ORDER BY id;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            skills.Add(new SkillEntry(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetInt32(3),
                                reader.GetInt32(4),
                                reader.GetInt32(5) == 1
                            ));
                        }
                    }
                }
                conn.Close();
            }
            return skills;
        }

        /// <summary>插入一个新技能</summary>
        public void InsertSkill(SkillEntry skill)
        {
            if (skill == null) return;

            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
                        INSERT INTO skills (skill_name, description, unlocked_count, total_count, is_expandable)
                        VALUES ('{Escape(skill.SkillName)}', '{Escape(skill.Description)}',
                                {skill.UnlockedCount}, {skill.TotalCount}, {(skill.IsExpandable ? 1 : 0)});";
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }

        /// <summary>更新技能</summary>
        public void UpdateSkill(SkillEntry skill)
        {
            if (skill == null) return;

            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
                        UPDATE skills SET
                            skill_name    = '{Escape(skill.SkillName)}',
                            description   = '{Escape(skill.Description)}',
                            unlocked_count = {skill.UnlockedCount},
                            total_count   = {skill.TotalCount},
                            is_expandable = {(skill.IsExpandable ? 1 : 0)}
                        WHERE id = {skill.Id};";
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }

        /// <summary>删除技能</summary>
        public void DeleteSkill(int id)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"DELETE FROM skills WHERE id = {id};";
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  工具方法
        // ═══════════════════════════════════════════════════════════════

        /// <summary>关闭数据库连接池（应用退出时调用）</summary>
        public void Shutdown()
        {
            SqliteConnection.ClearAllPools();
            IsInitialized = false;
            Debug.Log("[DatabaseManager] 数据库已关闭");
        }
    }
}
