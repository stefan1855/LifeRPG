namespace LifeRPG.Core.Models;

public sealed class CharacterAttribute
{
    public Guid Id { get; set; }

    public Guid CharacterId { get; set; }

    public string AttributeKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int Level { get; set; } = 1;

    public int Experience { get; set; }

    public int BaseValue { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
