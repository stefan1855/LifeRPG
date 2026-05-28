using System.Windows;
using LifeRPG.Data.Services;
using LifeRPG.Storage.Services;
using MessageBox = System.Windows.MessageBox;

namespace LifeRPG.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            AppStoragePaths.EnsureCreated();
            DatabaseInitializer.Initialize();

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"LifeRPG.Native 初始化失败：{ex.Message}",
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }
}
