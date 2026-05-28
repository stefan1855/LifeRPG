using System.Windows;
using LifeRPG.Data.Services;
using LifeRPG.Storage.Services;

namespace LifeRPG.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DatabasePathTextBlock.Text = DatabaseInitializer.DatabasePath;
        AssetsPathTextBlock.Text = AppStoragePaths.AssetsDirectory;
    }
}
