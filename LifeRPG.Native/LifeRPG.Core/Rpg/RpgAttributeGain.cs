namespace LifeRPG.Core.Rpg;

public sealed class RpgAttributeGain
{
    public string AttributeKey { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public int ExperienceDelta { get; init; }
}
