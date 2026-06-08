namespace LifeRPG.Core.Rpg;

public sealed class RpgEngine
{
    public RpgLevelResult CalculateLevel(int totalExperience)
    {
        var normalizedExperience = Math.Max(0, totalExperience);
        var level = 1;
        var experienceRequiredForCurrentLevel = 0;
        var experienceRequiredForNextLevel = 100;

        while (normalizedExperience >= experienceRequiredForNextLevel)
        {
            level++;
            experienceRequiredForCurrentLevel = experienceRequiredForNextLevel;
            experienceRequiredForNextLevel += 100 * level;
        }

        return new RpgLevelResult
        {
            Level = level,
            TotalExperience = normalizedExperience,
            ExperienceIntoCurrentLevel = normalizedExperience - experienceRequiredForCurrentLevel,
            ExperienceRequiredForCurrentLevel = experienceRequiredForCurrentLevel,
            ExperienceRequiredForNextLevel = experienceRequiredForNextLevel,
            ExperienceToNextLevel = Math.Max(0, experienceRequiredForNextLevel - normalizedExperience)
        };
    }

    public IReadOnlyList<RpgAttributeGain> AllocateTaskExperience(int taskExperience, string? growthAttribute)
    {
        var normalizedExperience = Math.Max(0, taskExperience);
        var definition = CharacterAttributeCatalog.Resolve(growthAttribute);

        return
        [
            new RpgAttributeGain
            {
                AttributeKey = definition.Key,
                DisplayName = definition.DisplayName,
                ExperienceDelta = normalizedExperience
            }
        ];
    }

    public int CalculateExperienceToNextLevel(int currentLevel)
    {
        return Math.Max(0, 100 * Math.Max(1, currentLevel));
    }

    public IReadOnlyDictionary<string, int> ApplyEquipmentBonuses(
        IReadOnlyDictionary<string, int> baseAttributes,
        IReadOnlyList<RpgEquipmentBonus> equipmentBonuses)
    {
        var result = baseAttributes.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        foreach (var bonus in equipmentBonuses)
        {
            var key = CharacterAttributeCatalog.NormalizeKey(bonus.AttributeKey);
            result.TryGetValue(key, out var currentValue);
            result[key] = currentValue + bonus.BonusValue;
        }

        return result;
    }
}
