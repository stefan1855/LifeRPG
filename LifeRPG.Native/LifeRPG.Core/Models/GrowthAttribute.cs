namespace LifeRPG.Core.Models;

public sealed class GrowthAttribute
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public DateTime UpdatedAt { get; set; }
}
