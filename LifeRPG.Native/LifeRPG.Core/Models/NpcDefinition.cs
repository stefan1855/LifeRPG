namespace LifeRPG.Core.Models;

public sealed class NpcDefinition
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string RealIdentity { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string InfluenceType { get; set; } = string.Empty;

    public string LinkedDomain { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
