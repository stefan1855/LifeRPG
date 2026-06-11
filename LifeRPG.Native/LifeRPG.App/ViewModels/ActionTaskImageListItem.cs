namespace LifeRPG.App.ViewModels;

public sealed class ActionTaskImageListItem
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string FileSizeText { get; set; } = string.Empty;
    public string CreatedAtText { get; set; } = string.Empty;
}
