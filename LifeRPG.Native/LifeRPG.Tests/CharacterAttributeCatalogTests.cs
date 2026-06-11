using LifeRPG.Core.Rpg;

namespace LifeRPG.Tests;

public sealed class CharacterAttributeCatalogTests
{
    [Theory]
    [InlineData("体魄", "physique", "体魄")]
    [InlineData("心智", "mind", "心智")]
    [InlineData("纪律", "discipline", "纪律")]
    [InlineData("表达", "expression", "表达")]
    [InlineData("创造", "creativity", "创造")]
    [InlineData("资源", "resources", "资源")]
    public void Resolve_KnownAttribute_ReturnsDefinition(string input, string expectedKey, string expectedDisplayName)
    {
        var result = CharacterAttributeCatalog.Resolve(input);

        Assert.Equal(expectedKey, result.Key);
        Assert.Equal(expectedDisplayName, result.DisplayName);
    }

    [Fact]
    public void Resolve_NullOrEmpty_ReturnsGeneral()
    {
        var nullResult = CharacterAttributeCatalog.Resolve(null);
        Assert.Equal("general", nullResult.Key);
        Assert.Equal("未设置", nullResult.DisplayName);

        var emptyResult = CharacterAttributeCatalog.Resolve("");
        Assert.Equal("general", emptyResult.Key);
    }

    [Fact]
    public void Resolve_UnknownAttribute_ReturnsCustomDefinition()
    {
        var result = CharacterAttributeCatalog.Resolve("魅力");

        Assert.Equal("魅力", result.DisplayName);
        Assert.NotEqual("general", result.Key);
    }

    [Theory]
    [InlineData("PHYSIQUE", "physique")]
    [InlineData(" 体魄 ", "体魄")]
    [InlineData("", "general")]
    [InlineData(null, "general")]
    [InlineData("hello world!", "helloworld")]
    public void NormalizeKey_HandlesVariousInputs(string? input, string expected)
    {
        var result = CharacterAttributeCatalog.NormalizeKey(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetDefaultDefinitions_ReturnsSixAttributes()
    {
        var definitions = CharacterAttributeCatalog.GetDefaultDefinitions();

        Assert.Equal(6, definitions.Count);
        Assert.Contains(definitions, d => d.Key == "physique");
        Assert.Contains(definitions, d => d.Key == "mind");
        Assert.Contains(definitions, d => d.Key == "discipline");
        Assert.Contains(definitions, d => d.Key == "expression");
        Assert.Contains(definitions, d => d.Key == "creativity");
        Assert.Contains(definitions, d => d.Key == "resources");
    }
}
