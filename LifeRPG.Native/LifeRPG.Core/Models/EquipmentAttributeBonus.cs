namespace LifeRPG.Core.Models;

public sealed class EquipmentAttributeBonus
{
    public Guid Id { get; set; }

    public Guid EquipmentDefinitionId { get; set; }

    public string AttributeKey { get; set; } = string.Empty;

    public int BonusValue { get; set; }
}
