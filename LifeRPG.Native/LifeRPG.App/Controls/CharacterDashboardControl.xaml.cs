using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using LifeRPG.App.Infrastructure;
using LifeRPG.Core.Models;
using LifeRPG.Core.Rpg;
using LifeRPG.Data.Repositories;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using MessageBox = System.Windows.MessageBox;
using WpfButton = System.Windows.Controls.Button;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfWindow = System.Windows.Window;

namespace LifeRPG.App.Controls;

public partial class CharacterDashboardControl : WpfUserControl
{
    private readonly CharacterRepository _characterRepository;
    private readonly RpgEngine _rpgEngine;
    private bool _loaded;
    private Character? _currentCharacter;

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
            _currentCharacter = snapshot.Character;

            CharacterNameTextBlock.Text = snapshot.Character.Name;
            CharacterDescriptionTextBlock.Text = string.IsNullOrWhiteSpace(snapshot.Character.Description)
                ? "暂无简介"
                : snapshot.Character.Description;
            CharacterLevelTextBlock.Text = $"等级 Lv.{snapshot.Character.Level}";
            CharacterTotalExperienceTextBlock.Text = $"总经验 {snapshot.Character.TotalExperience}";
            CharacterNextLevelTextBlock.Text = $"距离下一级 {snapshot.ExperienceToNextLevel} EXP";
            CharacterCreatedAtTextBlock.Text = $"创建时间：{snapshot.Character.CreatedAt:yyyy-MM-dd HH:mm:ss}";
            CharacterLastGrowthAtTextBlock.Text = snapshot.Character.LastGrowthAt.HasValue
                ? $"最后成长时间：{snapshot.Character.LastGrowthAt.Value:yyyy-MM-dd HH:mm:ss}"
                : "最后成长时间：暂无";
            CharacterLevelProgressBar.Maximum = Math.Max(1, snapshot.CurrentLevelExperienceSpan);
            CharacterLevelProgressBar.Value = Math.Min(snapshot.CurrentLevelExperienceSpan, snapshot.ExperienceIntoCurrentLevel);
            LoadAvatar(snapshot.Character.AvatarImagePath);

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

    private void SelectAvatarButton_Click(object sender, RoutedEventArgs e)
    {
        var character = _currentCharacter ?? _characterRepository.GetDefaultCharacter();
        var dialog = new OpenFileDialog
        {
            Multiselect = false,
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.webp;*.bmp"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _characterRepository.UpdateAvatar(character.Id, dialog.FileName);
        RefreshDashboard(showErrorMessage: true);
    }

    private void ClearAvatarButton_Click(object sender, RoutedEventArgs e)
    {
        var character = _currentCharacter ?? _characterRepository.GetDefaultCharacter();
        _characterRepository.UpdateAvatar(character.Id, null);
        RefreshDashboard(showErrorMessage: true);
    }

    private void EditProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var character = _currentCharacter ?? _characterRepository.GetDefaultCharacter();

        var nameTextBox = new WpfTextBox
        {
            Margin = new Thickness(0, 8, 0, 0),
            Text = character.Name,
            MinWidth = 420
        };

        var descriptionTextBox = new WpfTextBox
        {
            Margin = new Thickness(0, 8, 0, 0),
            Text = character.Description ?? string.Empty,
            MinHeight = 120,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var saveButton = new WpfButton
        {
            Content = "保存",
            Width = 90,
            Height = 34,
            IsDefault = true
        };

        var cancelButton = new WpfButton
        {
            Content = "取消",
            Width = 90,
            Height = 34,
            Margin = new Thickness(10, 0, 0, 0),
            IsCancel = true
        };

        var buttonPanel = new WrapPanel
        {
            Margin = new Thickness(0, 16, 0, 0)
        };
        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(cancelButton);

        var panel = new WpfStackPanel
        {
            Margin = new Thickness(18)
        };
        panel.Children.Add(new WpfTextBlock
        {
            Text = "角色名称",
            Foreground = System.Windows.Media.Brushes.White
        });
        panel.Children.Add(nameTextBox);
        panel.Children.Add(new WpfTextBlock
        {
            Margin = new Thickness(0, 16, 0, 0),
            Text = "角色简介",
            Foreground = System.Windows.Media.Brushes.White
        });
        panel.Children.Add(descriptionTextBox);
        panel.Children.Add(buttonPanel);

        var window = new WpfWindow
        {
            Title = "编辑角色资料",
            Width = 520,
            Height = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            Background = System.Windows.Media.Brushes.Black,
            Content = panel,
            ResizeMode = ResizeMode.NoResize
        };

        saveButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show("角色名称不能为空。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _characterRepository.UpdateProfile(character.Id, nameTextBox.Text, descriptionTextBox.Text);
            window.DialogResult = true;
            window.Close();
        };

        if (window.ShowDialog() == true)
        {
            RefreshDashboard(showErrorMessage: true);
        }
    }

    private void LoadAvatar(string? avatarImagePath)
    {
        CharacterAvatarImage.Source = null;
        CharacterAvatarPlaceholderTextBlock.Visibility = Visibility.Visible;

        if (string.IsNullOrWhiteSpace(avatarImagePath) || !File.Exists(avatarImagePath))
        {
            return;
        }

        try
        {
            CharacterAvatarImage.Source = LoadBitmapWithoutFileLock(avatarImagePath);
            CharacterAvatarPlaceholderTextBlock.Visibility = Visibility.Collapsed;
        }
        catch
        {
            CharacterAvatarImage.Source = null;
            CharacterAvatarPlaceholderTextBlock.Visibility = Visibility.Visible;
        }
    }

    private static BitmapImage LoadBitmapWithoutFileLock(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();

        return bitmap;
    }

    private static string ResolveAttributeName(IEnumerable<CharacterAttribute> attributes, string attributeKey)
    {
        return attributes.FirstOrDefault(attribute =>
            string.Equals(attribute.AttributeKey, attributeKey, StringComparison.OrdinalIgnoreCase))?.DisplayName
            ?? CharacterAttributeCatalog.Resolve(attributeKey).DisplayName;
    }
}
