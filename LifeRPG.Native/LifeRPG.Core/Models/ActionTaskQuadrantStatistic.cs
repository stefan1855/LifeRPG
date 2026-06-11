namespace LifeRPG.Core.Models;

public sealed class ActionTaskQuadrantStatistic
{
    public string Quadrant { get; set; } = string.Empty;
    public string QuadrantDisplayName { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Experience { get; set; }
}
