namespace LifeRPG.Core.Models;

public sealed class ActionTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string Category { get; set; } = "未分类";

    public ActionTaskStatus Status { get; set; } = ActionTaskStatus.NotStarted;

    public ActionTaskQuadrant Quadrant { get; set; } = ActionTaskQuadrant.ImportantNotUrgent;

    public string GrowthAttribute { get; set; } = string.Empty;

    public int Experience { get; set; }

    public DateTime? DueAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string CompletionNote { get; set; } = string.Empty;

    public string? LinkedLifeRecordId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public static int CalculateExperience(ActionTaskQuadrant quadrant)
    {
        return quadrant switch
        {
            ActionTaskQuadrant.ImportantUrgent => 30,
            ActionTaskQuadrant.ImportantNotUrgent => 25,
            ActionTaskQuadrant.NotImportantUrgent => 15,
            ActionTaskQuadrant.NotImportantNotUrgent => 5,
            _ => 25
        };
    }
}
