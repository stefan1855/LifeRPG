namespace LifeRPG.Core.Models;

public sealed class LifeRecord
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Experience { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string ContentSummary =>
        string.IsNullOrWhiteSpace(Content)
            ? string.Empty
            : (Content.Length <= 80 ? Content : $"{Content[..80]}...");
}
