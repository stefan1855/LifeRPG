namespace LifeRPG.Core.Models;

public sealed class RpgExperienceEvent
{
    public Guid Id { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public Guid SourceId { get; set; }

    public Guid CharacterId { get; set; }

    public int TotalExperience { get; set; }

    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? RolledBackAt { get; set; }

    public bool IsRolledBack => RolledBackAt.HasValue;
}
