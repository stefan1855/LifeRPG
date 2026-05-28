namespace LifeRPG.Core.Models;

public sealed class CategoryStatistics
{
    public string Category { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public int TotalExperience { get; set; }
    public int ImageCount { get; set; }
}
