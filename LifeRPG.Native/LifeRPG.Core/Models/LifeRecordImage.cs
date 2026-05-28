namespace LifeRPG.Core.Models;

public sealed class LifeRecordImage
{
    public Guid Id { get; set; }
    public Guid LifeRecordId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
