namespace LifeRPG.Core.Models;

public sealed class MapLocation
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string LocationType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsUnlocked { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
