using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LifeRPG.App.ViewModels;
using LifeRPG.Core.Models;
using LifeRPG.Data.Repositories;
using LifeRPG.Storage.Services;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using Forms = System.Windows.Forms;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfListBox = System.Windows.Controls.ListBox;

namespace LifeRPG.App;

public partial class MainWindow : Window
{
    private const string AllCategoriesOption = "全部分类";

    private readonly LifeRecordRepository _lifeRecordRepository;
    private readonly LifeRecordImageRepository _lifeRecordImageRepository;
    private readonly ActionTaskRepository _actionTaskRepository;
    private readonly StatisticsRepository _statisticsRepository;
    private readonly ImageStorageService _imageStorageService;
    private readonly BackupService _backupService;
    private readonly MarkdownExportService _markdownExportService;
    private readonly List<string> _selectedImagePaths = [];
    private List<LifeRecord> _currentDisplayedRecords = [];

    private string? _selectedRecordId;
    private LifeRecordDetailViewModel? _currentRecordDetail;

    public MainWindow(
        LifeRecordRepository lifeRecordRepository,
        LifeRecordImageRepository lifeRecordImageRepository,
        ActionTaskRepository actionTaskRepository,
        StatisticsRepository statisticsRepository,
        ImageStorageService imageStorageService,
        BackupService backupService,
        MarkdownExportService markdownExportService)
    {
        _lifeRecordRepository = lifeRecordRepository;
        _lifeRecordImageRepository = lifeRecordImageRepository;
        _actionTaskRepository = actionTaskRepository;
        _statisticsRepository = statisticsRepository;
        _imageStorageService = imageStorageService;
        _backupService = backupService;
        _markdownExportService = markdownExportService;

        InitializeComponent();
        ActionTaskControl.LifeRecordSelectionRequested += ActionTaskControl_LifeRecordSelectionRequested;
        ActionTaskControl.LifeRecordLinkChanged += ActionTaskControl_LifeRecordLinkChanged;
        ActionTaskControl.RpgProgressUpdated += ActionTaskControl_RpgProgressUpdated;
        SystemStatusControl.SetStatus(
            version: "v1.1",
            storageMode: "本地 SQLite + 本地图片目录",
            obsidianDependency: "否",
            browserEngine: "否",
            rootDirectory: AppStoragePaths.RootDirectory,
            databasePath: LifeRPG.Data.Services.DatabaseInitializer.DatabasePath,
            assetsDirectory: AppStoragePaths.AssetsDirectory,
            backupsDirectory: AppStoragePaths.BackupsDirectory,
            exportsDirectory: AppStoragePaths.ExportsDirectory);
        UpdateSelectedImagesDisplay();
        ClearSelectedRecordDetail();
        LoadCategories();
        RefreshRecordsList();
        LoadStatisticsDashboard();
        CharacterDashboardControl.RefreshDashboard(showErrorMessage: false);
    }

    private void SaveRecordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var title = TitleTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("标题不能为空。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var record = new LifeRecord
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Category = string.IsNullOrWhiteSpace(CategoryTextBox.Text) ? "未分类" : CategoryTextBox.Text.Trim(),
                Content = ContentTextBox.Text ?? string.Empty,
                Experience = int.TryParse(ExperienceTextBox.Text, out var experience) ? experience : 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _lifeRecordRepository.Create(record);
            var copiedImages = _imageStorageService.CopyImagesForRecord(record.Id, record.Title, _selectedImagePaths);
            foreach (var image in copiedImages)
                _lifeRecordImageRepository.Create(image);
            ClearForm();
            LoadCategories(GetSelectedCategoryFilter());
            RefreshRecordsList(record.Id);
            LoadStatisticsDashboard();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存行为记录失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SelectImagesButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Multiselect = true, Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif" };
        if (dialog.ShowDialog() != true) return;
        _selectedImagePaths.Clear();
        _selectedImagePaths.AddRange(dialog.FileNames);
        UpdateSelectedImagesDisplay();
    }

    private void RecordsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecordsListBox.SelectedItem is LifeRecordListItem selectedItem)
        {
            LoadRecordDetail(selectedItem.Id);
            return;
        }
        ClearSelectedRecordDetail();
    }

    private void SaveDetailButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_selectedRecordId))
            {
                MessageBox.Show("请先选择一条记录。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var existingRecord = _lifeRecordRepository.GetById(_selectedRecordId);
            if (existingRecord is null)
            {
                MessageBox.Show("当前记录不存在，可能已被移除。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
                ClearSelectedRecordDetail();
                RefreshRecordsList();
                return;
            }
            var title = DetailTitleTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("标题不能为空。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            existingRecord.Title = title;
            existingRecord.Category = string.IsNullOrWhiteSpace(DetailCategoryTextBox.Text) ? "未分类" : DetailCategoryTextBox.Text.Trim();
            existingRecord.Content = DetailContentTextBox.Text ?? string.Empty;
            existingRecord.Experience = int.TryParse(DetailExperienceTextBox.Text, out var experience) ? experience : 0;
            existingRecord.UpdatedAt = DateTime.Now;
            _lifeRecordRepository.Update(existingRecord);
            LoadCategories(GetSelectedCategoryFilter());
            RefreshRecordsList(existingRecord.Id);
            LoadStatisticsDashboard();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存记录修改失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteRecordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_selectedRecordId))
            {
                MessageBox.Show("请先选择一条记录。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var recordId = _selectedRecordId;
            var confirm = MessageBox.Show("删除后将同时删除该记录、该记录关联的图片数据库记录，以及本地图片文件。\n此操作不可撤销。", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var record = _lifeRecordRepository.GetById(recordId);
            if (record is null)
            {
                MessageBox.Show("当前记录不存在，可能已被删除。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadCategories(GetSelectedCategoryFilter());
                RefreshRecordsList();
                ClearSelectedRecordDetail();
                LoadStatisticsDashboard();
                return;
            }
            var images = _lifeRecordImageRepository.GetByLifeRecordId(recordId);
            var folderPaths = images.Select(image => Path.GetDirectoryName(image.FilePath)).Where(path => !string.IsNullOrWhiteSpace(path)).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            ImagePreviewItemsControl.ItemsSource = null;
            foreach (var image in images)
            {
                try { _imageStorageService.DeleteImageFile(image.FilePath); }
                catch (Exception ex) { throw new InvalidOperationException($"删除记录关联图片失败：{image.FilePath}。{ex.Message}", ex); }
            }
            _lifeRecordImageRepository.DeleteByLifeRecordId(recordId);
            _actionTaskRepository.UnlinkByLifeRecordId(recordId);
            _lifeRecordRepository.Delete(recordId);
            foreach (var folderPath in folderPaths)
                _imageStorageService.TryDeleteEmptyRecordImageFolder(folderPath);
            LoadCategories(GetSelectedCategoryFilter());
            RefreshRecordsList();
            ClearSelectedRecordDetail();
            LoadStatisticsDashboard();
            ActionTaskControl.RefreshFromExternalChange();
            MessageBox.Show("记录已删除。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"删除记录失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteImageButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: string imageId } || string.IsNullOrWhiteSpace(imageId)) return;
            var confirm = MessageBox.Show("确定删除这张图片证据吗？", "LifeRPG.Native", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            var image = _lifeRecordImageRepository.GetById(imageId);
            if (image is null)
            {
                MessageBox.Show("图片记录不存在，可能已经被删除。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
                if (!string.IsNullOrWhiteSpace(_selectedRecordId))
                {
                    RefreshRecordsList(_selectedRecordId);
                    LoadRecordDetail(_selectedRecordId);
                }
                return;
            }
            var folderPath = Path.GetDirectoryName(image.FilePath);
            ImagePreviewItemsControl.ItemsSource = null;
            _imageStorageService.DeleteImageFile(image.FilePath);
            _lifeRecordImageRepository.Delete(image.Id);
            if (!string.IsNullOrWhiteSpace(folderPath))
                _imageStorageService.TryDeleteEmptyRecordImageFolder(folderPath);
            if (!string.IsNullOrWhiteSpace(image.LifeRecordId))
            {
                RefreshRecordsList(image.LifeRecordId);
                LoadRecordDetail(image.LifeRecordId);
            }
            LoadStatisticsDashboard();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"删除图片失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateBackupButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var backupPath = _backupService.CreateBackup();
            UpdateBackupStatus(backupPath, $"备份成功：{backupPath}");
            MessageBox.Show($"备份成功：{backupPath}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            BackupStatusTextBlock.Text = $"备份失败：{ex.Message}";
            MessageBox.Show($"创建备份失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateBackupToDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog { Description = "请选择备份输出目录", UseDescriptionForTitle = true, AutoUpgradeEnabled = true, ShowNewFolderButton = true };
        if (dialog.ShowDialog() != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath)) return;
        try
        {
            var backupPath = _backupService.CreateBackup(dialog.SelectedPath);
            UpdateBackupStatus(backupPath, $"备份成功：{backupPath}");
            MessageBox.Show($"备份成功：{backupPath}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            BackupStatusTextBlock.Text = $"备份失败：{ex.Message}";
            MessageBox.Show($"选择目录备份失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenBackupsDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(AppStoragePaths.BackupsDirectory);
            Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = AppStoragePaths.BackupsDirectory, UseShellExecute = true });
            BackupStatusTextBlock.Text = $"已打开备份目录：{AppStoragePaths.BackupsDirectory}";
        }
        catch (Exception ex)
        {
            BackupStatusTextBlock.Text = $"打开备份目录失败：{ex.Message}";
            MessageBox.Show($"打开备份目录失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "备份压缩包|*.zip", Multiselect = false };
        if (dialog.ShowDialog() != true) return;
        var confirm = MessageBox.Show("恢复备份会覆盖当前数据库和图片资源，此操作会先自动创建一份恢复前备份。\n是否继续？", "确认恢复", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;
        try
        {
            PrepareForRestore();
            var restoreResult = _backupService.RestoreFromBackup(dialog.FileName);
            LoadCategories();
            RefreshRecordsList();
            LoadStatisticsDashboard();
            ActionTaskControl.RefreshFromExternalChange();
            CharacterDashboardControl.RefreshDashboard(showErrorMessage: false);
            UpdateBackupStatus(dialog.FileName, restoreResult);
            MessageBox.Show(restoreResult, "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            BackupStatusTextBlock.Text = $"恢复失败：{ex.Message}";
            MessageBox.Show($"从备份恢复失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
            LoadCategories();
            RefreshRecordsList();
            LoadStatisticsDashboard();
            CharacterDashboardControl.RefreshDashboard(showErrorMessage: false);
        }
    }

    private void RefreshStatisticsButton_Click(object sender, RoutedEventArgs e) => LoadStatisticsDashboard(showErrorMessage: true);

    private void SearchRecordsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSearchCriteria(out _, out _, out _, out _, showValidationMessage: true)) return;
        ClearSelectedRecordDetail();
        RefreshRecordsList();
    }

    private void ResetSearchButton_Click(object sender, RoutedEventArgs e)
    {
        SearchKeywordTextBox.Clear();
        CategoryFilterComboBox.SelectedIndex = 0;
        StartDatePicker.SelectedDate = null;
        EndDatePicker.SelectedDate = null;
        ClearSelectedRecordDetail();
        RefreshRecordsList();
    }

    private void ExportMarkdownButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var exportPath = _markdownExportService.ExportAllRecordsToMarkdown();
            UpdateExportStatus(exportPath, $"导出成功：{exportPath}");
            MessageBox.Show($"导出成功：{exportPath}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ExportStatusTextBlock.Text = $"导出失败：{ex.Message}";
            MessageBox.Show($"导出 Markdown 失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportMarkdownToDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog { Description = "请选择导出目录", UseDescriptionForTitle = true, AutoUpgradeEnabled = true, ShowNewFolderButton = true };
        if (dialog.ShowDialog() != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath)) return;
        try
        {
            var exportPath = _markdownExportService.ExportAllRecordsToMarkdown(dialog.SelectedPath);
            UpdateExportStatus(exportPath, $"导出成功：{exportPath}");
            MessageBox.Show($"导出成功：{exportPath}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ExportStatusTextBlock.Text = $"导出失败：{ex.Message}";
            MessageBox.Show($"选择目录导出 Markdown 失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenExportsDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(AppStoragePaths.ExportsDirectory);
            Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = AppStoragePaths.ExportsDirectory, UseShellExecute = true });
            ExportStatusTextBlock.Text = $"已打开导出目录：{AppStoragePaths.ExportsDirectory}";
        }
        catch (Exception ex)
        {
            ExportStatusTextBlock.Text = $"打开导出目录失败：{ex.Message}";
            MessageBox.Show($"打开导出目录失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportFilteredMarkdownButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var exportPath = _markdownExportService.ExportRecordsToMarkdown(_currentDisplayedRecords);
            UpdateExportStatus(exportPath, $"导出成功：{exportPath}");
            MessageBox.Show($"导出成功：{exportPath}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ExportStatusTextBlock.Text = $"导出失败：{ex.Message}";
            MessageBox.Show($"导出筛选结果 Markdown 失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportFilteredMarkdownToDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog { Description = "请选择导出目录", UseDescriptionForTitle = true, AutoUpgradeEnabled = true, ShowNewFolderButton = true };
        if (dialog.ShowDialog() != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath)) return;
        try
        {
            var exportPath = _markdownExportService.ExportRecordsToMarkdown(_currentDisplayedRecords, dialog.SelectedPath);
            UpdateExportStatus(exportPath, $"导出成功：{exportPath}");
            MessageBox.Show($"导出成功：{exportPath}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ExportStatusTextBlock.Text = $"导出失败：{ex.Message}";
            MessageBox.Show($"选择目录导出筛选结果失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ViewSourceActionTaskButton_Click(object sender, RoutedEventArgs e)
    {
        var taskId = ViewSourceActionTaskButton.Tag as string;
        if (string.IsNullOrWhiteSpace(taskId)) return;
        var task = _actionTaskRepository.GetById(taskId);
        if (task is null)
        {
            MessageBox.Show("该行动任务不存在，可能已被删除。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
            ViewSourceActionTaskButton.Tag = null;
            ViewSourceActionTaskButton.IsEnabled = false;
            return;
        }
        // Expand the action task panel and select the task
        ActionTaskPanel.Visibility = Visibility.Visible;
        UpdateToggleButtonText(ToggleActionTaskButton, true);
        ActionTaskPanel.BringIntoView();
    }

    // === Panel Toggle ===

    private void TogglePanel_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        UIElement? panel = button.Name switch
        {
            "ToggleDashboardButton" => DashboardPanel,
            "ToggleActionTaskButton" => ActionTaskPanel,
            "ToggleRecordsButton" => RecordsPanel,
            "ToggleSystemButton" => SystemPanel,
            _ => null
        };
        if (panel is null) return;
        var isExpanded = panel.Visibility == Visibility.Visible;
        panel.Visibility = isExpanded ? Visibility.Collapsed : Visibility.Visible;
        UpdateToggleButtonText(button, !isExpanded);
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        var targetPanel = button.Content.ToString() switch
        {
            "首页" => DashboardPanel,
            "行为记录" => RecordsPanel,
            "成长属性" => DashboardPanel,
            "图片证据" => DashboardPanel,
            "复盘归档" => RecordsPanel,
            "设置" => SystemPanel,
            _ => null
        };
        if (targetPanel is null) return;
        targetPanel.Visibility = Visibility.Visible;
        Button toggleButton = targetPanel == DashboardPanel ? ToggleDashboardButton
            : targetPanel == ActionTaskPanel ? ToggleActionTaskButton
            : targetPanel == RecordsPanel ? ToggleRecordsButton : ToggleSystemButton;
        UpdateToggleButtonText(toggleButton, true);
        targetPanel.BringIntoView();
    }

    private static void UpdateToggleButtonText(Button button, bool isExpanded)
    {
        var text = button.Content.ToString() ?? "";
        var cleanText = text.TrimStart('▶', '▾', ' ');
        button.Content = isExpanded ? $"▾ {cleanText}" : $"▶ {cleanText}";
    }

    // === Scroll handling for nested DataGrid/ListBox inside panel ScrollViewers ===

    private void PanelScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (IsInsideScrollableControl(e.OriginalSource as DependencyObject) && sender is ScrollViewer sv)
        {
            sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }

    private static bool IsInsideScrollableControl(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is WpfDataGrid || element is WpfListBox) return true;
            element = VisualTreeHelper.GetParent(element);
        }
        return false;
    }

    // === Private helpers below — unchanged from original ===

    private void ClearForm()
    {
        TitleTextBox.Clear();
        CategoryTextBox.Clear();
        ContentTextBox.Clear();
        ExperienceTextBox.Clear();
        _selectedImagePaths.Clear();
        UpdateSelectedImagesDisplay();
    }

    private void UpdateSelectedImagesDisplay()
    {
        SelectedImagesListBox.ItemsSource = _selectedImagePaths.Select(Path.GetFileName).ToList();
        SelectedImagesCountTextBlock.Text = $"已选择 {_selectedImagePaths.Count} 张图片";
    }

    private void UpdateBackupStatus(string path, string message)
    {
        LatestBackupPathTextBlock.Text = path;
        BackupStatusTextBlock.Text = message;
    }

    private void UpdateExportStatus(string path, string message)
    {
        LatestExportPathTextBlock.Text = path;
        ExportStatusTextBlock.Text = message;
    }

    private void LoadCategories(string? preserveSelection = null)
    {
        var currentSelection = preserveSelection ?? GetSelectedCategoryFilter();
        var categories = _lifeRecordRepository.GetAllCategories();
        CategoryFilterComboBox.ItemsSource = new[] { AllCategoriesOption }.Concat(categories).ToList();
        CategoryFilterComboBox.SelectedItem = categories.Contains(currentSelection) ? currentSelection : AllCategoriesOption;
    }

    private string GetSelectedCategoryFilter()
    {
        return CategoryFilterComboBox.SelectedItem as string ?? AllCategoriesOption;
    }

    private void RefreshRecordsList(string? selectRecordId = null)
    {
        TryGetSearchCriteria(out var keyword, out var category, out var startDate, out var endDate, showValidationMessage: false);
        var records = _lifeRecordRepository.Search(keyword, category, startDate, endDate);
        _currentDisplayedRecords = records;
        var items = records.Select(record =>
        {
            var imageCount = _lifeRecordImageRepository.GetByLifeRecordId(record.Id).Count;
            return new LifeRecordListItem
            {
                Id = record.Id,
                Title = record.Title,
                Category = record.Category,
                Experience = record.Experience,
                CreatedAt = record.CreatedAt,
                ContentPreview = TruncateContent(record.Content),
                ImageCount = imageCount
            };
        }).ToList();
        RecordsListBox.ItemsSource = items;
        ResultCountTextBlock.Text = $"当前显示 {items.Count} 条记录";
        if (selectRecordId is not null)
        {
            var target = items.FirstOrDefault(i => i.Id == selectRecordId);
            if (target is not null)
            {
                RecordsListBox.SelectedItem = target;
                RecordsListBox.ScrollIntoView(target);
            }
        }
    }

    private static string TruncateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;
        return content.Length > 200 ? content[..200] + "…" : content;
    }

    private bool TryGetSearchCriteria(out string keyword, out string category, out DateTime? startDate, out DateTime? endDate, bool showValidationMessage)
    {
        keyword = SearchKeywordTextBox.Text.Trim();
        category = GetSelectedCategoryFilter();
        if (string.Equals(category, AllCategoriesOption, StringComparison.Ordinal)) category = string.Empty;
        startDate = StartDatePicker.SelectedDate;
        endDate = EndDatePicker.SelectedDate;
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            if (showValidationMessage) MessageBox.Show("开始日期不能晚于结束日期。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    private void ClearSelectedRecordDetail()
    {
        _selectedRecordId = null;
        _currentRecordDetail = null;
        DetailTitleTextBox.Clear();
        DetailCategoryTextBox.Clear();
        DetailContentTextBox.Clear();
        DetailExperienceTextBox.Clear();
        DetailCreatedAtTextBlock.Text = string.Empty;
        DetailUpdatedAtTextBlock.Text = string.Empty;
        DetailPlaceholderTextBlock.Visibility = Visibility.Visible;
        DetailPanel.Visibility = Visibility.Collapsed;
        ImagePreviewItemsControl.ItemsSource = null;
        SourceActionTaskTextBlock.Text = "无";
        ViewSourceActionTaskButton.Tag = null;
        ViewSourceActionTaskButton.IsEnabled = false;
    }

    private void LoadRecordDetail(string recordId)
    {
        var record = _lifeRecordRepository.GetById(recordId);
        if (record is null)
        {
            ClearSelectedRecordDetail();
            return;
        }
        _selectedRecordId = recordId;
        _currentRecordDetail = new LifeRecordDetailViewModel { Id = record.Id, Title = record.Title, Category = record.Category, Content = record.Content, Experience = record.Experience, CreatedAt = record.CreatedAt, UpdatedAt = record.UpdatedAt };
        DetailTitleTextBox.Text = record.Title;
        DetailCategoryTextBox.Text = record.Category;
        DetailContentTextBox.Text = record.Content;
        DetailExperienceTextBox.Text = record.Experience.ToString();
        DetailCreatedAtTextBlock.Text = record.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        DetailUpdatedAtTextBlock.Text = record.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        DetailPlaceholderTextBlock.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility = Visibility.Visible;
        var images = _lifeRecordImageRepository.GetByLifeRecordId(recordId);
        ImagePreviewItemsControl.ItemsSource = images.Select(image =>
        {
            BitmapImage? previewSource = null;
            if (File.Exists(image.FilePath))
            {
                try { previewSource = LoadBitmapWithoutFileLock(image.FilePath); } catch { }
            }
            return new LifeRecordImageListItem { Id = image.Id, FileName = image.FileName, FilePath = image.FilePath, CreatedAt = image.CreatedAt, PreviewImageSource = previewSource };
        }).ToList();
        RefreshSourceActionTask(recordId);
    }

    private void LoadStatisticsDashboard(bool showErrorMessage = false)
    {
        try
        {
            var summary = _statisticsRepository.GetSummary();
            var categoryStatistics = _statisticsRepository.GetCategoryStatistics();
            var dailyStatistics = _statisticsRepository.GetRecentDailyStatistics(7);
            TotalRecordsTextBlock.Text = summary.TotalRecords.ToString();
            TotalExperienceTextBlock.Text = summary.TotalExperience.ToString();
            TotalImagesTextBlock.Text = summary.TotalImages.ToString();
            RecordsWithImagesTextBlock.Text = summary.RecordsWithImages.ToString();
            RecordsWithoutImagesTextBlock.Text = summary.RecordsWithoutImages.ToString();
            CategoryCountTextBlock.Text = summary.CategoryCount.ToString();
            LatestRecordCreatedAtTextBlock.Text = summary.LatestRecordCreatedAt.HasValue ? summary.LatestRecordCreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "暂无记录";
            CategoryStatisticsDataGrid.ItemsSource = categoryStatistics;
            DailyStatisticsDataGrid.ItemsSource = dailyStatistics;
        }
        catch (Exception ex)
        {
            if (showErrorMessage) MessageBox.Show($"加载统计看板失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PrepareForRestore()
    {
        ImagePreviewItemsControl.ItemsSource = null;
    }

    private void ActionTaskControl_LifeRecordSelectionRequested(string lifeRecordId)
    {
        LoadCategories(GetSelectedCategoryFilter());
        RefreshRecordsList(lifeRecordId);
        LoadRecordDetail(lifeRecordId);
    }

    private void ActionTaskControl_LifeRecordLinkChanged()
    {
        LoadCategories(GetSelectedCategoryFilter());
        RefreshRecordsList();
        ClearSelectedRecordDetail();
    }

    private void ActionTaskControl_RpgProgressUpdated()
    {
        CharacterDashboardControl.RefreshDashboard(showErrorMessage: false);
    }

    private void RefreshSourceActionTask(string lifeRecordId)
    {
        var sourceTask = _actionTaskRepository.GetByLinkedLifeRecordId(lifeRecordId);
        if (sourceTask is null)
        {
            SourceActionTaskTextBlock.Text = "无";
            ViewSourceActionTaskButton.Tag = null;
            ViewSourceActionTaskButton.IsEnabled = false;
            return;
        }
        SourceActionTaskTextBlock.Text = $"{sourceTask.Title}（{GetActionTaskStatusDisplayName(sourceTask.Status)}）";
        ViewSourceActionTaskButton.Tag = sourceTask.Id;
        ViewSourceActionTaskButton.IsEnabled = true;
    }

    private static string GetActionTaskStatusDisplayName(ActionTaskStatus status) => status switch
    {
        ActionTaskStatus.NotStarted => "未开始",
        ActionTaskStatus.InProgress => "进行中",
        ActionTaskStatus.Completed => "已完成",
        ActionTaskStatus.Cancelled => "已取消",
        _ => status.ToString()
    };

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
}
