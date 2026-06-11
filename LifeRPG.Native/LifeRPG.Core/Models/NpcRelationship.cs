namespace LifeRPG.Core.Models;

public sealed class NpcRelationship
{
    public Guid Id { get; set; }

    public Guid CharacterId { get; set; }

    public Guid NpcDefinitionId { get; set; }

    public int RelationshipValue { get; set; }

    public DateTime? LastInteractionAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
