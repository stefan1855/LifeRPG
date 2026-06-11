namespace LifeRPG.Core.Models;

public sealed class EquipmentDefinition
{
    public Guid Id { get; set; }

    public string RealItemName { get; set; } = string.Empty;

    public string GameItemName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Status { get; set; } = "Owned";

    public string RealEffect { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
