namespace LifeRPG.Core.Models;

public sealed class Character
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "未命名角色";

    public string? AvatarImagePath { get; set; }

    public string? Description { get; set; }

    public int Level { get; set; } = 1;

    public int TotalExperience { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastGrowthAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
