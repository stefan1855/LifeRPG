namespace LifeRPG.Storage.Services;

public static class AppStoragePaths
{
    public static string AppDataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LifeRPG.Native");

    public static string DataDirectory { get; } = Path.Combine(AppDataRoot, "data");
    public static string AssetsDirectory { get; } = Path.Combine(AppDataRoot, "assets");
    public static string BackupsDirectory { get; } = Path.Combine(AppDataRoot, "backups");
    public static string LogsDirectory { get; } = Path.Combine(AppDataRoot, "logs");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(AppDataRoot);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(AssetsDirectory);
        Directory.CreateDirectory(BackupsDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}
