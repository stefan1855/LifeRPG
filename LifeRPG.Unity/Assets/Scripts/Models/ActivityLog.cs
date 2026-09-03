using System;

namespace LifeRPG
{
    /// <summary>
    /// 活动日志条目 —— 记录玩家每次获得经验的行动
    /// 映射到 SQLite activity_logs 表
    /// </summary>
    [Serializable]
    public class ActivityLog
    {
        /// <summary>主键</summary>
        public int Id { get; set; }

        /// <summary>活动发生时间</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>关联的契约节点标签（如 "ShadowFlesh"）</summary>
        public string ContractNodeTag { get; set; }

        /// <summary>获得的经验值</summary>
        public int ExpGained { get; set; }

        /// <summary>活动标题（简短描述）</summary>
        public string Title { get; set; }

        /// <summary>活动详细描述</summary>
        public string Description { get; set; }

        /// <summary>人性化的时间显示（如 "3小时前"）—— 运行时计算，不存库</summary>
        public string TimeAgoDisplay
        {
            get
            {
                TimeSpan span = DateTime.Now - Timestamp;

                if (span.TotalSeconds < 60)
                    return "刚刚";
                if (span.TotalMinutes < 60)
                    return $"{(int)span.TotalMinutes}分钟前";
                if (span.TotalHours < 24)
                    return $"{(int)span.TotalHours}小时前";
                if (span.TotalDays < 30)
                    return $"{(int)span.TotalDays}天前";
                if (span.TotalDays < 365)
                    return $"{(int)(span.TotalDays / 30)}个月前";

                return $"{(int)(span.TotalDays / 365)}年前";
            }
        }

        /// <summary>默认构造函数</summary>
        public ActivityLog() { }

        /// <summary>全参数构造函数</summary>
        public ActivityLog(int id, DateTime timestamp, string contractNodeTag,
            int expGained, string title, string description)
        {
            Id = id;
            Timestamp = timestamp;
            ContractNodeTag = contractNodeTag;
            ExpGained = expGained;
            Title = title;
            Description = description;
        }
    }
}
