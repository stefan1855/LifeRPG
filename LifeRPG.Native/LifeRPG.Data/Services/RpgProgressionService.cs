using LifeRPG.Core.Models;
using LifeRPG.Core.Rpg;
using LifeRPG.Data.Repositories;

namespace LifeRPG.Data.Services;

public sealed class RpgProgressionService
{
    private readonly CharacterRepository _characterRepository;
    private readonly RpgEngine _rpgEngine;

    public RpgProgressionService(CharacterRepository characterRepository, RpgEngine rpgEngine)
    {
        _characterRepository = characterRepository;
        _rpgEngine = rpgEngine;
    }

    public RpgExperienceResult ApplyActionTaskCompletion(ActionTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        var gains = _rpgEngine.AllocateTaskExperience(task.Experience, task.GrowthAttribute);
        var character = _characterRepository.GetOrCreateDefaultCharacter();

        return _characterRepository.ApplyExperienceEvent(
            character.Id,
            "ActionTask",
            Guid.Parse(task.Id),
            task.Experience,
            gains,
            $"完成行动任务：{task.Title}");
    }

    public bool RollbackActionTaskCompletion(ActionTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        var character = _characterRepository.GetOrCreateDefaultCharacter();
        return _characterRepository.RollbackExperienceEvent(
            character.Id,
            "ActionTask",
            Guid.Parse(task.Id));
    }
}
