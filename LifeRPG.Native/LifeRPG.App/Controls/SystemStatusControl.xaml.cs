using System.Windows.Controls;

namespace LifeRPG.App.Controls;

public partial class SystemStatusControl : System.Windows.Controls.UserControl
{
    public SystemStatusControl()
    {
        InitializeComponent();
    }

    public void SetStatus(
        string version,
        string storageMode,
        string obsidianDependency,
        string browserEngine,
        string rootDirectory,
        string databasePath,
        string assetsDirectory,
        string backupsDirectory,
        string exportsDirectory)
    {
        RootPathTextBlock.Text = rootDirectory;
        DatabasePathTextBlock.Text = databasePath;
        AssetsPathTextBlock.Text = assetsDirectory;
        BackupsPathTextBlock.Text = backupsDirectory;
        ExportsPathTextBlock.Text = exportsDirectory;
    }
}
