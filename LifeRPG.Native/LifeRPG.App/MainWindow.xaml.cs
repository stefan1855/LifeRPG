using System.IO;
using System.Windows;
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

    public MainWindow()
    {
        InitializeComponent();
        RootPathTextBlock.Text = AppStoragePaths.RootDirectory;
        DatabasePathTextBlock.Text = DatabaseInitializer.DatabasePath;
        AssetsPathTextBlock.Text = AppStoragePaths.AssetsDirectory;
        UpdateSelectedImagesDisplay();
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
            var copiedImages = _imageStorageService.CopyImagesForRecord(record.Id, _selectedImagePaths);

            foreach (var image in copiedImages)
            {
                _lifeRecordImageRepository.Create(image);
            }

            ClearForm();
            LoadLifeRecords();
            LoadImagePreviews(record.Id);
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

    private void RecordsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (RecordsListBox.SelectedItem is LifeRecordListItem selectedItem)
        {
            LoadImagePreviews(selectedItem.Id);
            return;
        }

        ImagePreviewItemsControl.ItemsSource = null;
    }

    private void LoadLifeRecords()
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
            .ToList();

        ImagePreviewItemsControl.ItemsSource = previews;
    }
}
