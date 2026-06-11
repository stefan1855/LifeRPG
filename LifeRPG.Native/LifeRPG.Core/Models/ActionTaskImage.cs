namespace LifeRPG.Core.Models;

public sealed class ActionTaskImage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ActionTaskId { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
