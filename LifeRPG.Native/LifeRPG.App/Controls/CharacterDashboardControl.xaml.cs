using System.Windows;
using System.Windows.Controls;
using LifeRPG.App.Infrastructure;
using LifeRPG.Core.Models;
using LifeRPG.Core.Rpg;
using LifeRPG.Data.Repositories;
using MessageBox = System.Windows.MessageBox;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace LifeRPG.App.Controls;

public partial class CharacterDashboardControl : WpfUserControl
{
    private readonly CharacterRepository _characterRepository;
    private readonly RpgEngine _rpgEngine;
    private bool _loaded;

    public CharacterDashboardControl()
    {
        _characterRepository = (CharacterRepository)AppServiceProvider.Services.GetService(typeof(CharacterRepository))!;
        _rpgEngine = (RpgEngine)AppServiceProvider.Services.GetService(typeof(RpgEngine))!;

        InitializeComponent();
        Loaded += CharacterDashboardControl_Loaded;
    }

    public void RefreshDashboard(bool showErrorMessage = true)
    {
        try
        {
            var snapshot = _characterRepository.GetDashboardSnapshot();

            CharacterNameTextBlock.Text = snapshot.Character.Name;
            CharacterLevelTextBlock.Text = $"等级 Lv.{snapshot.Character.Level}";
            CharacterTotalExperienceTextBlock.Text = $"总经验 {snapshot.Character.TotalExperience}";
            CharacterNextLevelTextBlock.Text = $"距离下一级 {snapshot.ExperienceToNextLevel} EXP";
            CharacterLevelProgressBar.Maximum = Math.Max(1, snapshot.CurrentLevelExperienceSpan);
            CharacterLevelProgressBar.Value = Math.Min(snapshot.CurrentLevelExperienceSpan, snapshot.ExperienceIntoCurrentLevel);

            AttributesItemsControl.ItemsSource = snapshot.Attributes.Select(attribute =>
            {
                var attributeLevel = _rpgEngine.CalculateLevel(attribute.Experience);
                return new
                {
                    attribute.DisplayName,
                    LevelText = $"Lv.{attribute.Level}",
                    ExperienceText = $"{attribute.Experience} EXP | 距离下一级 {attributeLevel.ExperienceToNextLevel}",
                    ProgressValue = attributeLevel.ExperienceIntoCurrentLevel,
                    ProgressMaximum = Math.Max(1, attributeLevel.ExperienceRequiredForNextLevel - attributeLevel.ExperienceRequiredForCurrentLevel)
                };
            }).ToList();

            var logs = snapshot.RecentAttributeLogs
                .Select(log => new
                {
                    Title = $"{ResolveAttributeName(snapshot.Attributes, log.AttributeKey)} +{log.ExperienceDelta} EXP",
                    Detail = log.Reason,
                    CreatedAtText = log.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                })
                .ToList();

            AttributeLogsItemsControl.ItemsSource = logs;
            AttributeLogsEmptyTextBlock.Visibility = logs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            EquippedItemsControl.ItemsSource = snapshot.EquippedItems;
            EquippedItemsEmptyTextBlock.Visibility = snapshot.EquippedItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            SkillsItemsControl.ItemsSource = snapshot.Skills
                .Select(skill => $"{skill.Name} | {skill.Category} | 占位骨架")
                .ToList();

            NpcAndMapItemsControl.ItemsSource = snapshot.NpcRelationships
                .Select(npc => $"NPC：{npc.Definition.Name} | 影响值 {npc.RelationshipValue}")
                .Concat(snapshot.MapLocations.Select(location => $"地图：{location.Name} | {location.LocationType}"))
                .ToList();
        }
        catch (Exception ex)
        {
            if (showErrorMessage)
            {
                MessageBox.Show(
                    $"加载角色仪表盘失败：{ex.Message}",
                    "LifeRPG.Native",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void CharacterDashboardControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        RefreshDashboard(showErrorMessage: false);
    }

    private void RefreshDashboardButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshDashboard(showErrorMessage: true);
    }

    private static string ResolveAttributeName(IEnumerable<CharacterAttribute> attributes, string attributeKey)
    {
        return attributes.FirstOrDefault(attribute =>
            string.Equals(attribute.AttributeKey, attributeKey, StringComparison.OrdinalIgnoreCase))?.DisplayName
            ?? CharacterAttributeCatalog.Resolve(attributeKey).DisplayName;
    }
}
