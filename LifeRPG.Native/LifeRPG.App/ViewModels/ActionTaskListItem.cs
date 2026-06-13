namespace LifeRPG.App.ViewModels;

public sealed class ActionTaskListItem
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string GrowthAttribute { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public string QuadrantText { get; set; } = string.Empty;

    public int Experience { get; set; }

    public string DueAtText { get; set; } = string.Empty;

    public string CreatedAtText { get; set; } = string.Empty;

    public bool IsOverdue { get; set; }
}
