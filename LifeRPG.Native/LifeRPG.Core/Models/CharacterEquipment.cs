namespace LifeRPG.Core.Models;

public sealed class CharacterEquipment
{
    public Guid Id { get; set; }

    public Guid CharacterId { get; set; }

    public Guid EquipmentDefinitionId { get; set; }

    public string Status { get; set; } = "Owned";

    public DateTime? EquippedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
