namespace LifeRPG.Core.Models;

public sealed class DailyRecordStatistics
{
    public DateTime Date { get; set; }
    public int RecordCount { get; set; }
    public int TotalExperience { get; set; }
    public int ImageCount { get; set; }
}
