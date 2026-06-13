using System.Text.Json;

namespace LifeRPG.Storage.Services;

public static class AppConfig
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LifeRPG.Native", "config.json");

    private static AppConfigData? _cached;

    public static string DataRootDirectory
    {
        get
        {
            LoadIfNeeded();
            return _cached?.DataRootDirectory ?? AppStoragePaths.DefaultRootDirectory;
        }
        set
        {
            LoadIfNeeded();
            _cached ??= new AppConfigData();
            _cached.DataRootDirectory = value;
            Save();
        }
    }

    private static void LoadIfNeeded()
    {
        if (_cached is not null) return;
        if (!File.Exists(ConfigPath)) return;
        try
        {
            var json = File.ReadAllText(ConfigPath);
            _cached = JsonSerializer.Deserialize<AppConfigData>(json);
        }
        catch { }
    }

    private static void Save()
    {
        var dir = Path.GetDirectoryName(ConfigPath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(_cached ?? new AppConfigData());
        File.WriteAllText(ConfigPath, json);
    }

    private sealed class AppConfigData
    {
        public string? DataRootDirectory { get; set; }
        public Dictionary<string, string>? Shortcuts { get; set; }
    }

    public static IReadOnlyDictionary<string, string> GetAllShortcuts()
    {
        LoadIfNeeded();
        _cached ??= new AppConfigData();
        _cached.Shortcuts ??= DefaultShortcuts();
        return _cached.Shortcuts;
    }

    public static void SaveShortcuts(Dictionary<string, string> shortcuts)
    {
        LoadIfNeeded();
        _cached ??= new AppConfigData();
        _cached.Shortcuts = shortcuts;
        Save();
    }

    private static Dictionary<string, string> DefaultShortcuts() => new()
    {
        ["新建记录"] = "Ctrl+N",
        ["保存"] = "Ctrl+S",
        ["搜索"] = "Ctrl+F",
    };
}
