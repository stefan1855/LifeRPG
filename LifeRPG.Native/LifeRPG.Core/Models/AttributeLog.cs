namespace LifeRPG.Core.Models;

public sealed class AttributeLog
{
    public Guid Id { get; set; }

    public Guid CharacterId { get; set; }

    public string AttributeKey { get; set; } = string.Empty;

    public int ExperienceDelta { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public Guid? SourceId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
