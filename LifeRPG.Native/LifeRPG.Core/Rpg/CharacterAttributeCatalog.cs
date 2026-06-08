namespace LifeRPG.Core.Rpg;

public static class CharacterAttributeCatalog
{
    public const string GeneralAttributeKey = "general";
    public const string GeneralDisplayName = "未设置";

    private static readonly IReadOnlyList<RpgAttributeDefinition> DefaultDefinitions =
    [
        new() { Key = "physique", DisplayName = "体魄", BaseValue = 0 },
        new() { Key = "mind", DisplayName = "心智", BaseValue = 0 },
        new() { Key = "discipline", DisplayName = "纪律", BaseValue = 0 },
        new() { Key = "expression", DisplayName = "表达", BaseValue = 0 },
        new() { Key = "creativity", DisplayName = "创造", BaseValue = 0 },
        new() { Key = "resources", DisplayName = "资源", BaseValue = 0 }
    ];

    public static IReadOnlyList<RpgAttributeDefinition> GetDefaultDefinitions()
    {
        return DefaultDefinitions;
    }

    public static RpgAttributeDefinition Resolve(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return new RpgAttributeDefinition
            {
                Key = GeneralAttributeKey,
                DisplayName = GeneralDisplayName,
                BaseValue = 0
            };
        }

        var normalized = rawValue.Trim();
        var existing = DefaultDefinitions.FirstOrDefault(definition =>
            string.Equals(definition.DisplayName, normalized, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(definition.Key, NormalizeKey(normalized), StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return existing;
        }

        return new RpgAttributeDefinition
        {
            Key = NormalizeKey(normalized),
            DisplayName = normalized,
            BaseValue = 0
        };
    }

    public static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GeneralAttributeKey;
        }

        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.Length == 0 ? GeneralAttributeKey : builder.ToString();
    }
}
