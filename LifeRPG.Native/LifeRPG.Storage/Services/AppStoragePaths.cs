namespace LifeRPG.Storage.Services;

public static class AppStoragePaths
{
    public static string DefaultRootDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LifeRPG.Native");

    public static string RootDirectory => AppConfig.DataRootDirectory;

    public static string AppDataRoot => RootDirectory;

    public static string DataDirectory => Path.Combine(RootDirectory, "data");
    public static string AssetsDirectory => Path.Combine(RootDirectory, "assets");
    public static string BackupsDirectory => Path.Combine(RootDirectory, "backups");
    public static string ExportsDirectory => Path.Combine(RootDirectory, "exports");
    public static string LogsDirectory => Path.Combine(RootDirectory, "logs");

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
