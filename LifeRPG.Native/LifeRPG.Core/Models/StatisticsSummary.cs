namespace LifeRPG.Core.Models;

public sealed class StatisticsSummary
{
    public int TotalRecords { get; set; }
    public int TotalExperience { get; set; }
    public int TotalImages { get; set; }
    public int RecordsWithImages { get; set; }
    public int RecordsWithoutImages { get; set; }
    public int CategoryCount { get; set; }
    public DateTime? LatestRecordCreatedAt { get; set; }
}
