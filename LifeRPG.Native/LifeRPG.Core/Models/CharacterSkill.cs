namespace LifeRPG.Core.Models;

public sealed class CharacterSkill
{
    public Guid Id { get; set; }

    public Guid CharacterId { get; set; }

    public Guid SkillDefinitionId { get; set; }

    public int Level { get; set; } = 1;

    public int Experience { get; set; }

    public bool IsUnlocked { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
