namespace LifeRPG.Core.Rpg;

public sealed class RpgAttributeDefinition
{
    public string Key { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public int BaseValue { get; init; }
}
