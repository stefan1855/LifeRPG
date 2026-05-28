using System.IO;
using System.Windows;
using System.Windows.Controls;
using LifeRPG.App.ViewModels;
using LifeRPG.Core.Models;
using LifeRPG.Data.Repositories;
using LifeRPG.Data.Services;
using LifeRPG.Storage.Services;
using Microsoft.Win32;

namespace LifeRPG.App;

public partial class MainWindow : Window
{
    private readonly LifeRecordRepository _lifeRecordRepository = new();
    private readonly LifeRecordImageRepository _lifeRecordImageRepository = new();
    private readonly ImageStorageService _imageStorageService = new();
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
        ClearDetailPanel();
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

        _selectedRecordId = null;
        _currentRecordDetail = null;
        ClearDetailPanel();
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
                ClearDetailPanel();
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
            _selectedRecordId = null;
            _currentRecordDetail = null;
            ClearDetailPanel();
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
            _selectedRecordId = null;
            _currentRecordDetail = null;
            ClearDetailPanel();
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
                CreatedAt = image.CreatedAt
            })
            .ToList();

        ImagePreviewItemsControl.ItemsSource = previews;
    }

    private void ClearDetailPanel()
    {
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
}
