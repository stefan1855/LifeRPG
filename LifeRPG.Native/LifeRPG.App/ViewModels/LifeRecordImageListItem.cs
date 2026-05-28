namespace LifeRPG.App.ViewModels;

public sealed class LifeRecordImageListItem
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
