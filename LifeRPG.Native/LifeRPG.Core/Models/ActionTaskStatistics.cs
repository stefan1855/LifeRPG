namespace LifeRPG.Core.Models;

public sealed class ActionTaskStatistics
{
    public int TotalTasks { get; set; }
    public int NotStartedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int CancelledTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int DueTodayTasks { get; set; }
    public int DueThisWeekTasks { get; set; }
    public int TotalExperience { get; set; }
    public int CompletedExperience { get; set; }
    public List<ActionTaskQuadrantStatistic> QuadrantStatistics { get; set; } = [];
    public List<ActionTaskAttributeStatistic> AttributeStatistics { get; set; } = [];
}
