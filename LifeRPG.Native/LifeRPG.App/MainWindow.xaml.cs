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
    private readonly Dictionary<string, (Button Nav, ScrollViewer Page)> _pages;

    public MainWindow(
        LifeRecordRepository lifeRecordRepository, LifeRecordImageRepository lifeRecordImageRepository,
        ActionTaskRepository actionTaskRepository, StatisticsRepository statisticsRepository,
        ImageStorageService imageStorageService, BackupService backupService,
        MarkdownExportService markdownExportService,
        CharacterRepository characterRepository, Core.Rpg.RpgEngine rpgEngine,
        ActionTaskImageRepository actionTaskImageRepository,
        ActionTaskImageStorageService actionTaskImageStorageService,
        Data.Services.RpgProgressionService rpgProgressionService)
    {
        _lifeRecordRepository = lifeRecordRepository;
        _lifeRecordImageRepository = lifeRecordImageRepository;
        _actionTaskRepository = actionTaskRepository;
        _statisticsRepository = statisticsRepository;
        _imageStorageService = imageStorageService;
        _backupService = backupService;
        _markdownExportService = markdownExportService;
        InitializeComponent();

        // Inject dependencies into controls
        CharacterDashboardControl.Initialize(characterRepository, rpgEngine);
        AttributesCharacterDashboard.Initialize(characterRepository, rpgEngine);
        ActionTaskControl.Initialize(actionTaskRepository, actionTaskImageRepository,
            lifeRecordRepository, actionTaskImageStorageService, rpgProgressionService, markdownExportService);

        _pages = new()
        {
            ["首页"] = (NavDashboard, DashboardPage),
            ["行动任务"] = (NavTasks, TasksPage),
            ["行为记录"] = (NavRecords, RecordsPage),
            ["成长属性"] = (NavAttributes, AttributesPage),
            ["图片证据"] = (NavImages, ImagesPage),
            ["复盘归档"] = (NavArchive, ArchivePage),
            ["设置"] = (NavSettings, SettingsPage),
        };

        ActionTaskControl.LifeRecordSelectionRequested += _ => NavigateToLifeRecord(_);
        ActionTaskControl.LifeRecordLinkChanged += () => { LoadCategories(GetSelectedCategoryFilter()); RefreshRecordsList(); ClearSelectedRecordDetail(); };
        ActionTaskControl.RpgProgressUpdated += () => { CharacterDashboardControl.RefreshDashboard(false); AttributesCharacterDashboard.RefreshDashboard(false); };

        LoadSettingsPage();
        UpdateSelectedImagesDisplay();
        ClearSelectedRecordDetail();
        LoadCategories();
        RefreshRecordsList();
        LoadStatisticsDashboard();
        CharacterDashboardControl.RefreshDashboard(false);
        AttributesCharacterDashboard.RefreshDashboard(false);
    }

    // ===== 页面切换 =====

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        var key = button.Content.ToString() ?? "";
        if (!_pages.TryGetValue(key, out var entry)) return;

        // Hide all pages
        foreach (var (nav, page) in _pages.Values)
        {
            page.Visibility = Visibility.Collapsed;
            nav.Style = (Style)FindResource("NavButtonStyle");
        }

        // Show target
        entry.Page.Visibility = Visibility.Visible;
        entry.Nav.Style = (Style)FindResource("NavButtonActiveStyle");

        // Load page-specific data
        if (key == "成长属性") LoadAttributesPage();
        if (key == "图片证据") LoadImagesPage();
        if (key == "设置") LoadSettingsPage();
        if (key == "复盘归档") LoadArchivePage();
    }

    private void NavigateToLifeRecord(string lifeRecordId)
    {
        // Switch to records page and select the record
        NavButton_Click(NavRecords, null!);
        LoadCategories(GetSelectedCategoryFilter());
        RefreshRecordsList(lifeRecordId);
        LoadRecordDetail(lifeRecordId);
    }

    // ===== 成长属性页 =====

    private void LoadAttributesPage()
    {
        AttributesCharacterDashboard.RefreshDashboard(false);
    }

    // ===== 图片证据页 =====

    private void LoadImagesPage()
    {
        var allRecords = _lifeRecordRepository.GetAll();
        var items = new List<object>();
        foreach (var record in allRecords)
        {
            var images = _lifeRecordImageRepository.GetByLifeRecordId(record.Id);
            foreach (var img in images)
            {
                BitmapImage? preview = null;
                if (File.Exists(img.FilePath))
                    try { preview = LoadBitmapWithoutFileLock(img.FilePath); } catch { }
                items.Add(new { img.FileName, img.CreatedAt, RecordTitle = record.Title, PreviewImageSource = preview });
            }
        }
        ImagesGalleryItemsControl.ItemsSource = items;
    }

    // ===== 设置页 =====

    private void LoadSettingsPage()
    {
        var dbPath = Data.Services.DatabaseInitializer.DatabasePath;
        var dbSize = File.Exists(dbPath) ? new FileInfo(dbPath).Length : 0;
        var summary = _statisticsRepository.GetSummary();

        SettingsVersionText.Text = "v1.2";
        SettingsRuntimeText.Text = $".NET {Environment.Version.Major}.{Environment.Version.Minor} WPF";
        SettingsRecordCountText.Text = summary.TotalRecords.ToString();
        SettingsImageCountText.Text = summary.TotalImages.ToString();
        SettingsDbSizeText.Text = dbSize switch
        {
            >= 1_048_576 => $"{dbSize / 1_048_576.0:F1} MB",
            >= 1024 => $"{dbSize / 1024.0:F0} KB",
            _ => $"{dbSize} B"
        };
        SettingsRootPathText.Text = AppStoragePaths.RootDirectory;
        SettingsDbPathText.Text = dbPath;
        SettingsAssetsPathText.Text = AppStoragePaths.AssetsDirectory;
        SettingsBackupsPathText.Text = AppStoragePaths.BackupsDirectory;
    }

    private void OpenDataDirButton_Click(object sender, RoutedEventArgs e) => OpenExplorer(AppStoragePaths.RootDirectory);
    private void OpenAssetsDirButton_Click(object sender, RoutedEventArgs e) => OpenExplorer(AppStoragePaths.AssetsDirectory);

    private void ChangeDataDirButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "选择新的数据存储目录",
            SelectedPath = AppStoragePaths.RootDirectory,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog() != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath)) return;

        var newPath = dialog.SelectedPath;
        if (string.Equals(newPath, AppStoragePaths.RootDirectory, StringComparison.OrdinalIgnoreCase)) return;

        AppConfig.DataRootDirectory = newPath;
        AppStoragePaths.EnsureCreated();
        SettingsRootPathText.Text = newPath;
        MessageBox.Show($"数据目录已修改为：{newPath}\n\n重启应用后生效。", "LifeRPG.Native",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static void OpenExplorer(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = path, UseShellExecute = true });
        }
        catch { }
    }

    // ===== 复盘归档 =====

    private void LoadArchivePage() => RefreshBackupList();

    private void RefreshBackupListButton_Click(object sender, RoutedEventArgs e) => RefreshBackupList();

    private void RefreshBackupList() => BackupHistoryDataGrid.ItemsSource = _backupService.ListBackups();

    private void BackupHistoryDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RestoreSelectedBackupButton.IsEnabled = BackupHistoryDataGrid.SelectedItem is BackupService.BackupInfo;
    }

    private void RestoreSelectedBackupButton_Click(object sender, RoutedEventArgs e)
    {
        if (BackupHistoryDataGrid.SelectedItem is not BackupService.BackupInfo info) return;
        if (MessageBox.Show($"确定要恢复备份 \"{info.FileName}\"？当前数据将被覆盖。", "确认恢复",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            ImagePreviewItemsControl.ItemsSource = null;
            var result = _backupService.RestoreFromBackup(info.FilePath);
            LoadCategories(); RefreshRecordsList(); LoadStatisticsDashboard();
            ActionTaskControl.RefreshFromExternalChange();
            CharacterDashboardControl.RefreshDashboard(false); AttributesCharacterDashboard.RefreshDashboard(false);
            UpdateBackupStatus(info.FilePath, result);
            RefreshBackupList();
            MessageBox.Show(result, "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { BackupStatusTextBlock.Text = $"恢复失败：{ex.Message}"; }
    }

    // ===== 业务逻辑 =====

    private void SaveRecordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var title = TitleTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title)) { MessageBox.Show("标题不能为空。"); return; }
            var record = new LifeRecord
            {
                Id = Guid.NewGuid().ToString(), Title = title,
                Category = string.IsNullOrWhiteSpace(CategoryTextBox.Text) ? "未分类" : CategoryTextBox.Text.Trim(),
                Content = ContentTextBox.Text ?? string.Empty,
                Experience = int.TryParse(ExperienceTextBox.Text, out var exp) ? exp : 0,
                CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now
            };
            _lifeRecordRepository.Create(record);
            foreach (var img in _imageStorageService.CopyImagesForRecord(record.Id, record.Title, _selectedImagePaths))
                _lifeRecordImageRepository.Create(img);
            ClearForm(); LoadCategories(GetSelectedCategoryFilter()); RefreshRecordsList(record.Id); LoadStatisticsDashboard();
        }
        catch (Exception ex) { MessageBox.Show($"保存失败：{ex.Message}"); }
    }

    private void SelectImagesButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Multiselect = true, Filter = "图片|*.jpg;*.jpeg;*.png;*.bmp;*.gif" };
        if (dlg.ShowDialog() != true) return;
        _selectedImagePaths.Clear(); _selectedImagePaths.AddRange(dlg.FileNames);
        UpdateSelectedImagesDisplay();
    }

    private void RecordsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecordsListBox.SelectedItem is LifeRecordListItem item) { LoadRecordDetail(item.Id); return; }
        ClearSelectedRecordDetail();
    }

    private void SaveDetailButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_selectedRecordId)) { MessageBox.Show("请先选择一条记录。"); return; }
            var rec = _lifeRecordRepository.GetById(_selectedRecordId);
            if (rec is null) { ClearSelectedRecordDetail(); RefreshRecordsList(); return; }
            var title = DetailTitleTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title)) { MessageBox.Show("标题不能为空。"); return; }
            rec.Title = title;
            rec.Category = string.IsNullOrWhiteSpace(DetailCategoryTextBox.Text) ? "未分类" : DetailCategoryTextBox.Text.Trim();
            rec.Content = DetailContentTextBox.Text ?? string.Empty;
            rec.Experience = int.TryParse(DetailExperienceTextBox.Text, out var exp) ? exp : 0;
            rec.UpdatedAt = DateTime.Now;
            _lifeRecordRepository.Update(rec);
            LoadCategories(GetSelectedCategoryFilter()); RefreshRecordsList(rec.Id); LoadStatisticsDashboard();
        }
        catch (Exception ex) { MessageBox.Show($"保存修改失败：{ex.Message}"); }
    }

    private void DeleteRecordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_selectedRecordId)) { MessageBox.Show("请先选择一条记录。"); return; }
            if (MessageBox.Show("删除后将同时删除关联图片，不可撤销。", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            var rid = _selectedRecordId;
            var rec = _lifeRecordRepository.GetById(rid);
            if (rec is null) { ClearSelectedRecordDetail(); RefreshRecordsList(); LoadStatisticsDashboard(); return; }
            var images = _lifeRecordImageRepository.GetByLifeRecordId(rid);
            var folders = images.Select(i => Path.GetDirectoryName(i.FilePath)).Where(p => !string.IsNullOrWhiteSpace(p)).Cast<string>().Distinct().ToList();
            ImagePreviewItemsControl.ItemsSource = null;
            foreach (var i in images) _imageStorageService.DeleteImageFile(i.FilePath);
            _lifeRecordImageRepository.DeleteByLifeRecordId(rid);
            _actionTaskRepository.UnlinkByLifeRecordId(rid);
            _lifeRecordRepository.Delete(rid);
            foreach (var f in folders) _imageStorageService.TryDeleteEmptyRecordImageFolder(f);
            LoadCategories(GetSelectedCategoryFilter()); RefreshRecordsList(); ClearSelectedRecordDetail();
            LoadStatisticsDashboard(); ActionTaskControl.RefreshFromExternalChange();
        }
        catch (Exception ex) { MessageBox.Show($"删除失败：{ex.Message}"); }
    }

    private void DeleteImageButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: string imageId } || string.IsNullOrWhiteSpace(imageId)) return;
            if (MessageBox.Show("确定删除？", "", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            var img = _lifeRecordImageRepository.GetById(imageId);
            if (img is null) return;
            var folder = Path.GetDirectoryName(img.FilePath);
            ImagePreviewItemsControl.ItemsSource = null;
            _imageStorageService.DeleteImageFile(img.FilePath);
            _lifeRecordImageRepository.Delete(img.Id);
            if (!string.IsNullOrWhiteSpace(folder)) _imageStorageService.TryDeleteEmptyRecordImageFolder(folder);
            if (!string.IsNullOrWhiteSpace(img.LifeRecordId)) { RefreshRecordsList(img.LifeRecordId); LoadRecordDetail(img.LifeRecordId); }
            LoadStatisticsDashboard();
        }
        catch (Exception ex) { MessageBox.Show($"删除图片失败：{ex.Message}"); }
    }

    private void CreateBackupButton_Click(object sender, RoutedEventArgs e)
    {
        try { var p = _backupService.CreateBackup(); UpdateBackupStatus(p, $"成功：{p}"); MessageBox.Show($"备份成功：{p}"); }
        catch (Exception ex) { BackupStatusTextBlock.Text = $"失败：{ex.Message}"; }
    }
    private void CreateBackupToDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new Forms.FolderBrowserDialog { Description = "选择备份目录", UseDescriptionForTitle = true };
        if (dlg.ShowDialog() != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dlg.SelectedPath)) return;
        try { var p = _backupService.CreateBackup(dlg.SelectedPath); UpdateBackupStatus(p, $"成功：{p}"); }
        catch (Exception ex) { BackupStatusTextBlock.Text = $"失败：{ex.Message}"; }
    }
    private void OpenBackupsDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        try { Directory.CreateDirectory(AppStoragePaths.BackupsDirectory); Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = AppStoragePaths.BackupsDirectory, UseShellExecute = true }); }
        catch { }
    }
    private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "备份|*.zip" };
        if (dlg.ShowDialog() != true) return;
        if (MessageBox.Show("恢复会覆盖当前数据，是否继续？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            ImagePreviewItemsControl.ItemsSource = null;
            var r = _backupService.RestoreFromBackup(dlg.FileName);
            LoadCategories(); RefreshRecordsList(); LoadStatisticsDashboard();
            ActionTaskControl.RefreshFromExternalChange();
            CharacterDashboardControl.RefreshDashboard(false); AttributesCharacterDashboard.RefreshDashboard(false);
            UpdateBackupStatus(dlg.FileName, r);
        }
        catch (Exception ex) { BackupStatusTextBlock.Text = $"失败：{ex.Message}"; RefreshRecordsList(); }
    }

    private void RefreshStatisticsButton_Click(object sender, RoutedEventArgs e) => LoadStatisticsDashboard(true);
    private void SearchRecordsButton_Click(object sender, RoutedEventArgs e) { if (TryGetSearchCriteria(out _, out _, out _, out _, true)) { ClearSelectedRecordDetail(); RefreshRecordsList(); } }
    private void ResetSearchButton_Click(object sender, RoutedEventArgs e) { SearchKeywordTextBox.Clear(); CategoryFilterComboBox.SelectedIndex = 0; StartDatePicker.SelectedDate = null; EndDatePicker.SelectedDate = null; ClearSelectedRecordDetail(); RefreshRecordsList(); }

    private void ExportMarkdownButton_Click(object sender, RoutedEventArgs e)
    { try { var p = _markdownExportService.ExportAllRecordsToMarkdown(); UpdateExportStatus(p, $"成功：{p}"); } catch { } }
    private void ExportMarkdownToDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new Forms.FolderBrowserDialog { Description = "选择导出目录" };
        if (dlg.ShowDialog() != Forms.DialogResult.OK) return;
        try { var p = _markdownExportService.ExportAllRecordsToMarkdown(dlg.SelectedPath); UpdateExportStatus(p, $"成功：{p}"); } catch { }
    }
    private void OpenExportsDirectoryButton_Click(object sender, RoutedEventArgs e)
    { try { Directory.CreateDirectory(AppStoragePaths.ExportsDirectory); Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = AppStoragePaths.ExportsDirectory, UseShellExecute = true }); } catch { } }
    private void ExportFilteredMarkdownButton_Click(object sender, RoutedEventArgs e)
    { try { var p = _markdownExportService.ExportRecordsToMarkdown(_currentDisplayedRecords); UpdateExportStatus(p, $"成功：{p}"); } catch { } }
    private void ExportFilteredMarkdownToDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new Forms.FolderBrowserDialog { Description = "选择导出目录" };
        if (dlg.ShowDialog() != Forms.DialogResult.OK) return;
        try { var p = _markdownExportService.ExportRecordsToMarkdown(_currentDisplayedRecords, dlg.SelectedPath); UpdateExportStatus(p, $"成功：{p}"); } catch { }
    }

    private void ViewSourceActionTaskButton_Click(object sender, RoutedEventArgs e)
    {
        var tid = ViewSourceActionTaskButton.Tag as string;
        if (string.IsNullOrWhiteSpace(tid)) return;
        var task = _actionTaskRepository.GetById(tid);
        if (task is null) { ViewSourceActionTaskButton.Tag = null; ViewSourceActionTaskButton.IsEnabled = false; return; }
        // Navigate to dashboard and scroll to action task
        NavButton_Click(NavDashboard, null!);
    }

    // ===== 私有辅助 =====

    private void ClearForm() { TitleTextBox.Clear(); CategoryTextBox.Clear(); ContentTextBox.Clear(); ExperienceTextBox.Clear(); _selectedImagePaths.Clear(); UpdateSelectedImagesDisplay(); }
    private void UpdateSelectedImagesDisplay() { SelectedImagesListBox.ItemsSource = _selectedImagePaths.Select(Path.GetFileName).ToList(); SelectedImagesCountTextBlock.Text = $"已选择 {_selectedImagePaths.Count} 张图片"; }
    private void UpdateBackupStatus(string p, string m) { LatestBackupPathTextBlock.Text = p; BackupStatusTextBlock.Text = m; }
    private void UpdateExportStatus(string p, string m) { LatestExportPathTextBlock.Text = p; ExportStatusTextBlock.Text = m; }

    private void LoadCategories(string? preserve = null)
    {
        var cur = preserve ?? GetSelectedCategoryFilter();
        var cats = _lifeRecordRepository.GetAllCategories();
        CategoryFilterComboBox.ItemsSource = new[] { AllCategoriesOption }.Concat(cats).ToList();
        CategoryFilterComboBox.SelectedItem = cats.Contains(cur) ? cur : AllCategoriesOption;
    }
    private string GetSelectedCategoryFilter() => CategoryFilterComboBox.SelectedItem as string ?? AllCategoriesOption;

    private void RefreshRecordsList(string? selectId = null)
    {
        TryGetSearchCriteria(out var kw, out var cat, out var sd, out var ed, false);
        var records = _lifeRecordRepository.Search(kw, cat, sd, ed);
        _currentDisplayedRecords = records;
        var items = records.Select(r => new LifeRecordListItem
        {
            Id = r.Id, Title = r.Title, Category = r.Category, Experience = r.Experience,
            CreatedAt = r.CreatedAt, ContentPreview = Truncate(r.Content),
            ImageCount = _lifeRecordImageRepository.GetByLifeRecordId(r.Id).Count
        }).ToList();
        RecordsListBox.ItemsSource = items;
        ResultCountTextBlock.Text = $"当前显示 {items.Count} 条记录";
        if (selectId is not null) { var t = items.FirstOrDefault(i => i.Id == selectId); if (t is not null) { RecordsListBox.SelectedItem = t; RecordsListBox.ScrollIntoView(t); } }
    }
    private static string Truncate(string c) => string.IsNullOrWhiteSpace(c) ? "" : c.Length > 200 ? c[..200] + "…" : c;

    private bool TryGetSearchCriteria(out string kw, out string cat, out DateTime? sd, out DateTime? ed, bool showMsg)
    {
        kw = SearchKeywordTextBox.Text.Trim(); cat = GetSelectedCategoryFilter();
        if (cat == AllCategoriesOption) cat = ""; sd = StartDatePicker.SelectedDate; ed = EndDatePicker.SelectedDate;
        if (sd.HasValue && ed.HasValue && sd > ed) { if (showMsg) MessageBox.Show("开始日期不能晚于结束日期。"); return false; }
        return true;
    }

    private void ClearSelectedRecordDetail()
    {
        _selectedRecordId = null; _currentRecordDetail = null;
        DetailTitleTextBox.Clear(); DetailCategoryTextBox.Clear(); DetailContentTextBox.Clear(); DetailExperienceTextBox.Clear();
        DetailCreatedAtTextBlock.Text = ""; DetailUpdatedAtTextBlock.Text = "";
        DetailPanel.Visibility = Visibility.Collapsed;
        ImagePreviewItemsControl.ItemsSource = null;
        SourceActionTaskTextBlock.Text = "无"; ViewSourceActionTaskButton.Tag = null; ViewSourceActionTaskButton.IsEnabled = false;
    }

    private void LoadRecordDetail(string rid)
    {
        var r = _lifeRecordRepository.GetById(rid);
        if (r is null) { ClearSelectedRecordDetail(); return; }
        _selectedRecordId = rid;
        DetailTitleTextBox.Text = r.Title; DetailCategoryTextBox.Text = r.Category;
        DetailContentTextBox.Text = r.Content; DetailExperienceTextBox.Text = r.Experience.ToString();
        DetailCreatedAtTextBlock.Text = r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        DetailUpdatedAtTextBlock.Text = r.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        DetailPanel.Visibility = Visibility.Visible;
        var images = _lifeRecordImageRepository.GetByLifeRecordId(rid);
        ImagePreviewItemsControl.ItemsSource = images.Select(i =>
        {
            BitmapImage? s = null;
            if (File.Exists(i.FilePath)) try { s = LoadBitmapWithoutFileLock(i.FilePath); } catch { }
            return new LifeRecordImageListItem { Id = i.Id, FileName = i.FileName, FilePath = i.FilePath, CreatedAt = i.CreatedAt, PreviewImageSource = s };
        }).ToList();
        var st = _actionTaskRepository.GetByLinkedLifeRecordId(rid);
        if (st is not null) { SourceActionTaskTextBlock.Text = $"{st.Title}（{StatusName(st.Status)}）"; ViewSourceActionTaskButton.Tag = st.Id; ViewSourceActionTaskButton.IsEnabled = true; }
        else { SourceActionTaskTextBlock.Text = "无"; ViewSourceActionTaskButton.Tag = null; ViewSourceActionTaskButton.IsEnabled = false; }
    }

    private void LoadStatisticsDashboard(bool showErr = false)
    {
        try
        {
            var s = _statisticsRepository.GetSummary();
            var cats = _statisticsRepository.GetCategoryStatistics();
            var daily = _statisticsRepository.GetRecentDailyStatistics(7);
            TotalRecordsTextBlock.Text = s.TotalRecords.ToString();
            TotalExperienceTextBlock.Text = s.TotalExperience.ToString();
            TotalImagesTextBlock.Text = s.TotalImages.ToString();
            RecordsWithImagesTextBlock.Text = s.RecordsWithImages.ToString();
            RecordsWithoutImagesTextBlock.Text = s.RecordsWithoutImages.ToString();
            CategoryCountTextBlock.Text = s.CategoryCount.ToString();
            LatestRecordCreatedAtTextBlock.Text = s.LatestRecordCreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "暂无记录";
            CategoryStatisticsDataGrid.ItemsSource = cats;
            DailyStatisticsDataGrid.ItemsSource = daily;
        }
        catch (Exception ex) { if (showErr) MessageBox.Show($"加载统计失败：{ex.Message}"); }
    }

    private static string StatusName(ActionTaskStatus s) => s switch
    {
        ActionTaskStatus.NotStarted => "未开始", ActionTaskStatus.InProgress => "进行中",
        ActionTaskStatus.Completed => "已完成", ActionTaskStatus.Cancelled => "已取消", _ => s.ToString()
    };

    private static BitmapImage LoadBitmapWithoutFileLock(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var bmp = new BitmapImage(); bmp.BeginInit(); bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.StreamSource = fs; bmp.EndInit(); bmp.Freeze(); return bmp;
    }
}
