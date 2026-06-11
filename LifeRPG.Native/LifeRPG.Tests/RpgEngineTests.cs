using LifeRPG.Core.Rpg;

namespace LifeRPG.Tests;

public sealed class RpgEngineTests
{
    private readonly RpgEngine _engine = new();

    [Fact]
    public void CalculateLevel_WithZeroExperience_ReturnsLevelOne()
    {
        var result = _engine.CalculateLevel(0);

        Assert.Equal(1, result.Level);
        Assert.Equal(100, result.ExperienceToNextLevel);
    }

    [Fact]
    public void CalculateLevel_WithOneHundredExperience_ReturnsLevelTwo()
    {
        var result = _engine.CalculateLevel(100);

        Assert.Equal(2, result.Level);
        Assert.Equal(200, result.ExperienceToNextLevel);
    }

    [Fact]
    public void AllocateTaskExperience_WithGrowthAttribute_AssignsAllExperienceToThatAttribute()
    {
        var result = _engine.AllocateTaskExperience(12, "纪律");

        var gain = Assert.Single(result);
        Assert.Equal("纪律", gain.DisplayName);
        Assert.Equal(12, gain.ExperienceDelta);
    }

    [Fact]
    public void AllocateTaskExperience_WithoutGrowthAttribute_UsesGeneralAttribute()
    {
        var result = _engine.AllocateTaskExperience(8, null);

        var gain = Assert.Single(result);
        Assert.Equal(CharacterAttributeCatalog.GeneralDisplayName, gain.DisplayName);
        Assert.Equal(CharacterAttributeCatalog.GeneralAttributeKey, gain.AttributeKey);
    }

    [Fact]
    public void ApplyEquipmentBonuses_DoesNotMutateOriginalDictionary()
    {
        IReadOnlyDictionary<string, int> baseAttributes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["mind"] = 10
        };

        var result = _engine.ApplyEquipmentBonuses(baseAttributes, [new RpgEquipmentBonus
        {
            AttributeKey = "mind",
            BonusValue = 3
        }]);

        Assert.Equal(10, baseAttributes["mind"]);
        Assert.Equal(13, result["mind"]);
    }

    [Fact]
    public void CalculateExperienceToNextLevel_NeverReturnsNegative()
    {
        var result = _engine.CalculateExperienceToNextLevel(-5);

        Assert.True(result >= 0);
    }
}
