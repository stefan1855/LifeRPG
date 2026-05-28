using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using LifeRPG.App.ViewModels;
using LifeRPG.Core.Models;
using LifeRPG.Data.Repositories;
using LifeRPG.Data.Services;
using LifeRPG.Storage.Services;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using Forms = System.Windows.Forms;

namespace LifeRPG.App;

public partial class MainWindow : Window
{
    private readonly LifeRecordRepository _lifeRecordRepository = new();
    private readonly LifeRecordImageRepository _lifeRecordImageRepository = new();
    private readonly ImageStorageService _imageStorageService = new();
    private readonly BackupService _backupService = new();
    private readonly List<string> _selectedImagePaths = [];

    private string? _selectedRecordId;
    private LifeRecordDetailViewModel? _currentRecordDetail;

    public MainWindow()
    {
        InitializeComponent();
        RootPathTextBlock.Text = AppStoragePaths.RootDirectory;
        DatabasePathTextBlock.Text = DatabaseInitializer.DatabasePath;
        AssetsPathTextBlock.Text = AppStoragePaths.AssetsDirectory;
        UpdateSelectedImagesDisplay();
        ClearSelectedRecordDetail();
        LoadLifeRecords();
    }

    private void SaveRecordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var title = TitleTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(
                    "标题不能为空。",
                    "LifeRPG.Native",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
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
            {
                _lifeRecordImageRepository.Create(image);
            }

            ClearForm();
            LoadLifeRecords(record.Id);
            LoadRecordDetail(record.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"保存行为记录失败：{ex.Message}",
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SelectImagesButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

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
                MessageBox.Show(
                    "请先选择一条记录。",
                    "LifeRPG.Native",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var existingRecord = _lifeRecordRepository.GetById(_selectedRecordId);
            if (existingRecord is null)
            {
                MessageBox.Show(
                    "当前记录不存在，可能已被移除。",
                    "LifeRPG.Native",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ClearSelectedRecordDetail();
                LoadLifeRecords();
                return;
            }

            var title = DetailTitleTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(
                    "标题不能为空。",
                    "LifeRPG.Native",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            existingRecord.Title = title;
            existingRecord.Category = string.IsNullOrWhiteSpace(DetailCategoryTextBox.Text) ? "未分类" : DetailCategoryTextBox.Text.Trim();
            existingRecord.Content = DetailContentTextBox.Text ?? string.Empty;
            existingRecord.Experience = int.TryParse(DetailExperienceTextBox.Text, out var experience) ? experience : 0;
            existingRecord.UpdatedAt = DateTime.Now;

            _lifeRecordRepository.Update(existingRecord);
            LoadLifeRecords(existingRecord.Id);
            LoadRecordDetail(existingRecord.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"保存记录修改失败：{ex.Message}",
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void DeleteRecordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_selectedRecordId))
            {
                MessageBox.Show(
                    "请先选择一条记录。",
                    "LifeRPG.Native",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var recordId = _selectedRecordId;
            var confirm = MessageBox.Show(
                "删除后将同时删除该记录、该记录关联的图片数据库记录，以及本地图片文件。\n此操作不可撤销。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var record = _lifeRecordRepository.GetById(recordId);
            if (record is null)
            {
                MessageBox.Show(
                    "当前记录不存在，可能已被删除。",
                    "LifeRPG.Native",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                LoadLifeRecords();
                ClearSelectedRecordDetail();
                return;
            }

            var images = _lifeRecordImageRepository.GetByLifeRecordId(recordId);
            var folderPaths = images
                .Select(image => Path.GetDirectoryName(image.FilePath))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            ImagePreviewItemsControl.ItemsSource = null;

            foreach (var image in images)
            {
                try
                {
                    _imageStorageService.DeleteImageFile(image.FilePath);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"删除记录关联图片失败：{image.FilePath}。{ex.Message}",
                        ex);
                }
            }

            _lifeRecordImageRepository.DeleteByLifeRecordId(recordId);
            _lifeRecordRepository.Delete(recordId);

            foreach (var folderPath in folderPaths)
            {
                _imageStorageService.TryDeleteEmptyRecordImageFolder(folderPath);
            }

            LoadLifeRecords();
            ClearSelectedRecordDetail();

            MessageBox.Show(
                "记录已删除。",
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"删除记录失败：{ex.Message}",
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void DeleteImageButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: string imageId } || string.IsNullOrWhiteSpace(imageId))
            {
                return;
            }

            var confirm = MessageBox.Show(
                "确定删除这张图片证据吗？",
                "LifeRPG.Native",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var image = _lifeRecordImageRepository.GetById(imageId);
            if (image is null)
            {
                MessageBox.Show(
                    "图片记录不存在，可能已经被删除。",
                    "LifeRPG.Native",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                if (!string.IsNullOrWhiteSpace(_selectedRecordId))
                {
                    LoadLifeRecords(_selectedRecordId);
                    LoadRecordDetail(_selectedRecordId);
                }
                return;
            }

            var folderPath = Path.GetDirectoryName(image.FilePath);
            ImagePreviewItemsControl.ItemsSource = null;
            _imageStorageService.DeleteImageFile(image.FilePath);
            _lifeRecordImageRepository.Delete(image.Id);

            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                _imageStorageService.TryDeleteEmptyRecordImageFolder(folderPath);
            }

            if (!string.IsNullOrWhiteSpace(image.LifeRecordId))
            {
                LoadLifeRecords(image.LifeRecordId);
                LoadRecordDetail(image.LifeRecordId);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"删除图片失败：{ex.Message}",
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CreateBackupButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var backupPath = _backupService.CreateBackup();
            UpdateBackupStatus(backupPath, $"备份成功：{backupPath}");

            MessageBox.Show(
                $"备份成功：{backupPath}",
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            BackupStatusTextBlock.Text = $"备份失败：{ex.Message}";
            MessageBox.Show(
                $"创建备份失败：{ex.Message}",
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CreateBackupToDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "请选择备份输出目录",
            UseDescriptionForTitle = true,
            AutoUpgradeEnabled = true,
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            return;
        }

        try
        {
            var backupPath = _backupService.CreateBackup(dialog.SelectedPath);
            UpdateBackupStatus(backupPath, $"备份成功：{backupPath}");

            MessageBox.Show(
                $"备份成功：{backupPath}",
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            BackupStatusTextBlock.Text = $"备份失败：{ex.Message}";
            MessageBox.Show(
                $"选择目录备份失败：{ex.Message}",
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenBackupsDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(AppStoragePaths.BackupsDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = AppStoragePaths.BackupsDirectory,
                UseShellExecute = true
            });

            BackupStatusTextBlock.Text = $"已打开备份目录：{AppStoragePaths.BackupsDirectory}";
        }
        catch (Exception ex)
        {
            BackupStatusTextBlock.Text = $"打开备份目录失败：{ex.Message}";
            MessageBox.Show(
                $"打开备份目录失败：{ex.Message}",
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "备份压缩包|*.zip",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var confirm = MessageBox.Show(
            "恢复备份会覆盖当前数据库和图片资源，此操作会先自动创建一份恢复前备份。\n是否继续？",
            "确认恢复",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            PrepareForRestore();
            var restoreResult = _backupService.RestoreFromBackup(dialog.FileName);
            LoadLifeRecords();
            UpdateBackupStatus(dialog.FileName, restoreResult);

            MessageBox.Show(
                restoreResult,
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            BackupStatusTextBlock.Text = $"恢复失败：{ex.Message}";
            MessageBox.Show(
                $"从备份恢复失败：{ex.Message}",
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            LoadLifeRecords();
        }
    }

    private void LoadLifeRecords(string? preserveSelectedRecordId = null)
    {
        var records = _lifeRecordRepository.GetAll();
        var items = records.Select(record =>
        {
            var images = _lifeRecordImageRepository.GetByLifeRecordId(record.Id);
            return new LifeRecordListItem
            {
                Id = record.Id,
                Title = record.Title,
                Category = record.Category,
                Experience = record.Experience,
                CreatedAt = record.CreatedAt,
                ContentPreview = record.ContentSummary,
                ImageCount = images.Count
            };
        }).ToList();

        RecordsListBox.ItemsSource = items;

        var selectedId = preserveSelectedRecordId ?? _selectedRecordId;
        if (string.IsNullOrWhiteSpace(selectedId))
        {
            return;
        }

        var selectedItem = items.FirstOrDefault(item => item.Id == selectedId);
        if (selectedItem is null)
        {
            ClearSelectedRecordDetail();
            return;
        }

        RecordsListBox.SelectedItem = selectedItem;
        RecordsListBox.ScrollIntoView(selectedItem);
    }

    private void LoadRecordDetail(string recordId)
    {
        var record = _lifeRecordRepository.GetById(recordId);
        if (record is null)
        {
            ClearSelectedRecordDetail();
            return;
        }

        _selectedRecordId = record.Id;
        _currentRecordDetail = new LifeRecordDetailViewModel
        {
            Id = record.Id,
            Title = record.Title,
            Category = record.Category,
            Experience = record.Experience,
            Content = record.Content,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };

        DetailPlaceholderTextBlock.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility = Visibility.Visible;

        DetailTitleTextBox.Text = _currentRecordDetail.Title;
        DetailCategoryTextBox.Text = _currentRecordDetail.Category;
        DetailExperienceTextBox.Text = _currentRecordDetail.Experience.ToString();
        DetailContentTextBox.Text = _currentRecordDetail.Content;
        DetailCreatedAtTextBlock.Text = _currentRecordDetail.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        DetailUpdatedAtTextBlock.Text = _currentRecordDetail.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");

        LoadImagePreviews(record.Id);
    }

    private void ClearForm()
    {
        TitleTextBox.Clear();
        CategoryTextBox.Clear();
        ExperienceTextBox.Clear();
        ContentTextBox.Clear();
        _selectedImagePaths.Clear();
        UpdateSelectedImagesDisplay();
    }

    private void UpdateSelectedImagesDisplay()
    {
        SelectedImagesCountTextBlock.Text = $"已选择 {_selectedImagePaths.Count} 张图片";
        SelectedImagesListBox.ItemsSource = _selectedImagePaths
            .Select(Path.GetFileName)
            .ToList();
    }

    private void LoadImagePreviews(string lifeRecordId)
    {
        var previews = _lifeRecordImageRepository
            .GetByLifeRecordId(lifeRecordId)
            .Where(image => File.Exists(image.FilePath))
            .Select(image => new LifeRecordImageListItem
            {
                Id = image.Id,
                FileName = image.FileName,
                FilePath = image.FilePath,
                CreatedAt = image.CreatedAt,
                PreviewImageSource = LoadBitmapWithoutFileLock(image.FilePath)
            })
            .Where(item => item.PreviewImageSource is not null)
            .ToList();

        ImagePreviewItemsControl.ItemsSource = previews;
    }

    private static BitmapImage? LoadBitmapWithoutFileLock(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
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
        catch
        {
            return null;
        }
    }

    private void ClearSelectedRecordDetail()
    {
        _selectedRecordId = null;
        _currentRecordDetail = null;
        RecordsListBox.SelectedItem = null;
        DetailPlaceholderTextBlock.Visibility = Visibility.Visible;
        DetailPanel.Visibility = Visibility.Collapsed;
        DetailTitleTextBox.Text = string.Empty;
        DetailCategoryTextBox.Text = string.Empty;
        DetailExperienceTextBox.Text = string.Empty;
        DetailContentTextBox.Text = string.Empty;
        DetailCreatedAtTextBlock.Text = string.Empty;
        DetailUpdatedAtTextBlock.Text = string.Empty;
        ImagePreviewItemsControl.ItemsSource = null;
    }

    private void PrepareForRestore()
    {
        ClearForm();
        ClearSelectedRecordDetail();
        RecordsListBox.ItemsSource = null;
    }

    private void UpdateBackupStatus(string latestBackupPath, string statusMessage)
    {
        LatestBackupPathTextBlock.Text = latestBackupPath;
        BackupStatusTextBlock.Text = statusMessage;
    }
}
