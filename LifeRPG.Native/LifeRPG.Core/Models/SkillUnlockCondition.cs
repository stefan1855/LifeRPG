namespace LifeRPG.Core.Models;

public sealed class SkillUnlockCondition
{
    public Guid Id { get; set; }

    public Guid SkillDefinitionId { get; set; }

    public string ConditionType { get; set; } = string.Empty;

    public string ConditionValue { get; set; } = string.Empty;
}
