namespace LifeRPG.Core.Models;

public sealed class CharacterDashboardSnapshot
{
    public Character Character { get; set; } = new();

    public int ExperienceToNextLevel { get; set; }

    public int ExperienceIntoCurrentLevel { get; set; }

    public int CurrentLevelExperienceSpan { get; set; }

    public IReadOnlyList<CharacterAttribute> Attributes { get; set; } = [];

    public IReadOnlyList<AttributeLog> RecentAttributeLogs { get; set; } = [];

    public IReadOnlyList<EquipmentDefinition> EquippedItems { get; set; } = [];

    public IReadOnlyList<SkillDefinition> Skills { get; set; } = [];

    public IReadOnlyList<NpcRelationshipSnapshot> NpcRelationships { get; set; } = [];

    public IReadOnlyList<MapLocation> MapLocations { get; set; } = [];
}
