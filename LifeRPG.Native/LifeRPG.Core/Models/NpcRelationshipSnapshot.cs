namespace LifeRPG.Core.Models;

public sealed class NpcRelationshipSnapshot
{
    public NpcDefinition Definition { get; set; } = new();

    public int RelationshipValue { get; set; }

    public DateTime? LastInteractionAt { get; set; }
}
