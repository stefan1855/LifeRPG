namespace LifeRPG.Core.Rpg;

public sealed class RpgLevelResult
{
    public int Level { get; init; }

    public int TotalExperience { get; init; }

    public int ExperienceIntoCurrentLevel { get; init; }

    public int ExperienceRequiredForCurrentLevel { get; init; }

    public int ExperienceRequiredForNextLevel { get; init; }

    public int ExperienceToNextLevel { get; init; }
}
