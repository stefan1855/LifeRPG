using LifeRPG.Core.Models;

namespace LifeRPG.Tests;

public sealed class ActionTaskTests
{
    [Theory]
    [InlineData(ActionTaskQuadrant.ImportantUrgent, 30)]
    [InlineData(ActionTaskQuadrant.ImportantNotUrgent, 25)]
    [InlineData(ActionTaskQuadrant.NotImportantUrgent, 15)]
    [InlineData(ActionTaskQuadrant.NotImportantNotUrgent, 5)]
    public void CalculateExperience_ReturnsExpectedValue(ActionTaskQuadrant quadrant, int expected)
    {
        var result = ActionTask.CalculateExperience(quadrant);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NewTask_HasDefaultValues()
    {
        var task = new ActionTask();

        Assert.Equal(ActionTaskStatus.NotStarted, task.Status);
        Assert.Equal(ActionTaskQuadrant.ImportantNotUrgent, task.Quadrant);
        Assert.Equal("未分类", task.Category);
        Assert.Empty(task.GrowthAttribute);
        Assert.NotEmpty(task.Id);
    }

    [Fact]
    public void NewTask_CalculatesExperienceFromQuadrant()
    {
        var task = new ActionTask { Quadrant = ActionTaskQuadrant.ImportantUrgent };
        var exp = ActionTask.CalculateExperience(task.Quadrant);
        task.Experience = exp;

        Assert.Equal(30, task.Experience);
    }

    [Fact]
    public void SetStatus_ChangesCorrectly()
    {
        var task = new ActionTask();

        task.Status = ActionTaskStatus.InProgress;
        Assert.Equal(ActionTaskStatus.InProgress, task.Status);

        task.Status = ActionTaskStatus.Completed;
        Assert.Equal(ActionTaskStatus.Completed, task.Status);

        task.Status = ActionTaskStatus.Cancelled;
        Assert.Equal(ActionTaskStatus.Cancelled, task.Status);
    }
}
