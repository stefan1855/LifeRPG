namespace LifeRPG.Core.Rpg;

public sealed class RpgExperienceResult
{
    public bool Applied { get; init; }

    public bool IsDuplicate { get; init; }

    public int TotalExperienceGranted { get; init; }

    public IReadOnlyList<RpgAttributeGain> Gains { get; init; } = [];
}
