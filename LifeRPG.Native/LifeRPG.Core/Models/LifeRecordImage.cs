namespace LifeRPG.Core.Models;

public sealed class LifeRecordImage
{
    public string Id { get; set; } = string.Empty;
    public string LifeRecordId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
