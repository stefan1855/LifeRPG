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

    [Fact]
    public void CalculateLevel_WithNegativeExperience_ReturnsLevelOne()
    {
        var result = _engine.CalculateLevel(-50);

        Assert.Equal(1, result.Level);
        Assert.Equal(0, result.ExperienceIntoCurrentLevel);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(299, 2)]
    [InlineData(300, 3)]
    [InlineData(599, 3)]
    [InlineData(600, 4)]
    [InlineData(999, 4)]
    [InlineData(1000, 5)]
    [InlineData(1499, 5)]
    [InlineData(1500, 6)]
    public void CalculateLevel_MatchesLevelFormula(int exp, int expectedLevel)
    {
        var result = _engine.CalculateLevel(exp);
        Assert.Equal(expectedLevel, result.Level);
    }

    [Fact]
    public void CalculateLevel_LevelFiveNeeds500ExpForNextLevel()
    {
        var result = _engine.CalculateLevel(1000);

        Assert.Equal(5, result.Level);
        Assert.Equal(500, result.ExperienceToNextLevel);
    }

    [Fact]
    public void AllocateTaskExperience_WithUnknownAttribute_CreatesItAsDisplayName()
    {
        var result = _engine.AllocateTaskExperience(20, "魅力");

        var gain = Assert.Single(result);
        Assert.Equal("魅力", gain.DisplayName);
        Assert.Equal(20, gain.ExperienceDelta);
    }

    [Fact]
    public void AllocateTaskExperience_WithZeroExperience_ReturnsZeroGain()
    {
        var result = _engine.AllocateTaskExperience(0, "体魄");

        var gain = Assert.Single(result);
        Assert.Equal(0, gain.ExperienceDelta);
    }

    [Fact]
    public void ApplyEquipmentBonuses_WithEmptyList_ReturnsCopyOfBase()
    {
        IReadOnlyDictionary<string, int> baseAttributes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["physique"] = 5
        };

        var result = _engine.ApplyEquipmentBonuses(baseAttributes, []);

        Assert.Equal(5, result["physique"]);
        Assert.NotSame(baseAttributes, result);
    }

    [Fact]
    public void ApplyEquipmentBonuses_WithMultipleBonuses_Accumulates()
    {
        IReadOnlyDictionary<string, int> baseAttributes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["mind"] = 10,
            ["physique"] = 5
        };

        var bonuses = new List<RpgEquipmentBonus>
        {
            new() { AttributeKey = "mind", BonusValue = 3 },
            new() { AttributeKey = "mind", BonusValue = 2 },
            new() { AttributeKey = "physique", BonusValue = 1 }
        };

        var result = _engine.ApplyEquipmentBonuses(baseAttributes, bonuses);

        Assert.Equal(15, result["mind"]);
        Assert.Equal(6, result["physique"]);
    }

    [Fact]
    public void CalculateExperienceToNextLevel_LevelOne_Returns100()
    {
        var result = _engine.CalculateExperienceToNextLevel(1);
        Assert.Equal(100, result);
    }

    [Fact]
    public void CalculateExperienceToNextLevel_LevelTen_Returns1000()
    {
        var result = _engine.CalculateExperienceToNextLevel(10);
        Assert.Equal(1000, result);
    }
}
