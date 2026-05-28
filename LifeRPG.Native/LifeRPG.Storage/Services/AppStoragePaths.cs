namespace LifeRPG.Storage.Services;

public static class AppStoragePaths
{
    public static string RootDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LifeRPG.Native");

    public static string AppDataRoot => RootDirectory;

    public static string DataDirectory { get; } = Path.Combine(RootDirectory, "data");
    public static string AssetsDirectory { get; } = Path.Combine(RootDirectory, "assets");
    public static string BackupsDirectory { get; } = Path.Combine(RootDirectory, "backups");
    public static string ExportsDirectory { get; } = Path.Combine(RootDirectory, "exports");
    public static string LogsDirectory { get; } = Path.Combine(RootDirectory, "logs");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(AssetsDirectory);
        Directory.CreateDirectory(BackupsDirectory);
        Directory.CreateDirectory(ExportsDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}
