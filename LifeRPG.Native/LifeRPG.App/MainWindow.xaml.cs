using System.Windows;
using LifeRPG.Core.Models;
using LifeRPG.Data.Repositories;
using LifeRPG.Data.Services;
using LifeRPG.Storage.Services;

namespace LifeRPG.App;

public partial class MainWindow : Window
{
    private readonly LifeRecordRepository _lifeRecordRepository = new();

    public MainWindow()
    {
        InitializeComponent();
        RootPathTextBlock.Text = AppStoragePaths.RootDirectory;
        DatabasePathTextBlock.Text = DatabaseInitializer.DatabasePath;
        AssetsPathTextBlock.Text = AppStoragePaths.AssetsDirectory;
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
            ClearForm();
            LoadLifeRecords();
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

    private void LoadLifeRecords()
    {
        RecordsListBox.ItemsSource = _lifeRecordRepository.GetAll();
    }

    private void ClearForm()
    {
        TitleTextBox.Clear();
        CategoryTextBox.Clear();
        ExperienceTextBox.Clear();
        ContentTextBox.Clear();
    }
}
