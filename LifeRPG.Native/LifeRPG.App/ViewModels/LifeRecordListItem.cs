namespace LifeRPG.App.ViewModels;

public sealed class LifeRecordListItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Experience { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ContentPreview { get; set; } = string.Empty;
    public int ImageCount { get; set; }
}
