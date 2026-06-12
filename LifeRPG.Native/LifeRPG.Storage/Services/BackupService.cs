using System.IO.Compression;
using System.Text.Json;

namespace LifeRPG.Storage.Services;

public sealed class BackupService
{
    private const string DatabaseFileName = "liferpg.db";
    private const string DefaultBackupPrefix = "LifeRPG.Native_Backup";
    private const string AutoBackupPrefix = "LifeRPG.Native_AutoBackupBeforeRestore";
    private const string ManifestEntryPath = "manifest.json";
    private const string DatabaseEntryPath = "data/liferpg.db";
    private const string AssetsEntryRoot = "assets";

    public string CreateBackup()
    {
        return CreateBackupInternal(AppStoragePaths.BackupsDirectory, DefaultBackupPrefix);
    }

    public string CreateBackup(string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new ArgumentException("备份目标目录不能为空。", nameof(targetDirectory));
        }

        return CreateBackupInternal(targetDirectory, DefaultBackupPrefix);
    }

    public string RestoreFromBackup(string backupZipPath)
    {
        return RestoreFromBackupInternal(backupZipPath, createSafetyBackup: true);
    }

    public List<BackupInfo> ListBackups()
    {
        var results = new List<BackupInfo>();
        var dir = AppStoragePaths.BackupsDirectory;
        if (!Directory.Exists(dir)) return results;

        foreach (var file in Directory.GetFiles(dir, "*.zip"))
        {
            var info = new FileInfo(file);
            results.Add(new BackupInfo
            {
                FilePath = file,
                FileName = Path.GetFileName(file),
                SizeBytes = info.Length,
                CreatedAt = info.CreationTime
            });
        }
        return results.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public sealed class BackupInfo
    {
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public long SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SizeDisplay => SizeBytes switch
        {
            >= 1_048_576 => $"{SizeBytes / 1_048_576.0:F1} MB",
            >= 1024 => $"{SizeBytes / 1024.0:F0} KB",
            _ => $"{SizeBytes} B"
        };
    }

    private string CreateBackupInternal(string targetDirectory, string fileNamePrefix)
    {
        AppStoragePaths.EnsureCreated();

        var databasePath = Path.Combine(AppStoragePaths.DataDirectory, DatabaseFileName);
        if (!File.Exists(databasePath))
        {
            throw new FileNotFoundException($"未找到数据库文件：{databasePath}", databasePath);
        }

        Directory.CreateDirectory(targetDirectory);

        var backupFileName = $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
        var backupZipPath = Path.Combine(targetDirectory, backupFileName);

        using var archive = ZipFile.Open(backupZipPath, ZipArchiveMode.Create);
        AddFileToArchive(archive, databasePath, DatabaseEntryPath);

        if (Directory.Exists(AppStoragePaths.AssetsDirectory))
        {
            AddDirectoryToArchive(archive, AppStoragePaths.AssetsDirectory, AssetsEntryRoot);
        }

        var manifest = new BackupManifest
        {
            AppName = "LifeRPG.Native",
            Version = "v0.1.0-native-dev",
            CreatedAt = DateTime.Now,
            DatabaseRelativePath = DatabaseEntryPath,
            AssetsRelativePath = AssetsEntryRoot,
            BackupType = "FullLocalBackup"
        };

        var manifestEntry = archive.CreateEntry(ManifestEntryPath, CompressionLevel.Optimal);
        using var manifestStream = manifestEntry.Open();
        JsonSerializer.Serialize(manifestStream, manifest, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return backupZipPath;
    }

    private string RestoreFromBackupInternal(string backupZipPath, bool createSafetyBackup)
    {
        if (string.IsNullOrWhiteSpace(backupZipPath))
        {
            throw new ArgumentException("备份文件路径不能为空。", nameof(backupZipPath));
        }

        if (!File.Exists(backupZipPath))
        {
            throw new FileNotFoundException($"未找到备份文件：{backupZipPath}", backupZipPath);
        }

        AppStoragePaths.EnsureCreated();

        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"LifeRPG.Native_Restore_{Guid.NewGuid():N}");

        string? safetyBackupPath = null;
        var restoreStarted = false;

        try
        {
            using (var archive = ZipFile.OpenRead(backupZipPath))
            {
                var databaseEntry = archive.Entries.FirstOrDefault(entry =>
                    string.Equals(NormalizeEntryPath(entry.FullName), DatabaseEntryPath, StringComparison.OrdinalIgnoreCase));

                if (databaseEntry is null)
                {
                    throw new InvalidOperationException("备份包缺少 data/liferpg.db，无法恢复。");
                }
            }

            if (createSafetyBackup)
            {
                safetyBackupPath = CreateBackupInternal(AppStoragePaths.BackupsDirectory, AutoBackupPrefix);
            }

            Directory.CreateDirectory(tempDirectory);
            ZipFile.ExtractToDirectory(backupZipPath, tempDirectory);

            var extractedDatabasePath = Path.Combine(tempDirectory, "data", DatabaseFileName);
            if (!File.Exists(extractedDatabasePath))
            {
                throw new InvalidOperationException("备份包中未找到可恢复的数据库文件 data/liferpg.db。");
            }

            restoreStarted = true;

            var currentDatabasePath = Path.Combine(AppStoragePaths.DataDirectory, DatabaseFileName);
            Directory.CreateDirectory(AppStoragePaths.DataDirectory);
            File.Copy(extractedDatabasePath, currentDatabasePath, overwrite: true);

            var extractedAssetsPath = Path.Combine(tempDirectory, AssetsEntryRoot);
            if (Directory.Exists(extractedAssetsPath))
            {
                ReplaceAssetsDirectory(extractedAssetsPath);
            }

            return createSafetyBackup && !string.IsNullOrWhiteSpace(safetyBackupPath)
                ? $"恢复成功。恢复前安全备份：{safetyBackupPath}"
                : "恢复成功。";
        }
        catch (Exception ex)
        {
            if (restoreStarted && createSafetyBackup && !string.IsNullOrWhiteSpace(safetyBackupPath) && File.Exists(safetyBackupPath))
            {
                try
                {
                    RestoreFromBackupInternal(safetyBackupPath, createSafetyBackup: false);
                }
                catch (Exception rollbackEx)
                {
                    throw new InvalidOperationException(
                        $"恢复备份失败：{ex.Message}。恢复前安全备份：{safetyBackupPath}。自动回滚失败：{rollbackEx.Message}",
                        ex);
                }
            }

            if (!string.IsNullOrWhiteSpace(safetyBackupPath))
            {
                throw new InvalidOperationException(
                    $"恢复备份失败：{ex.Message}。恢复前安全备份：{safetyBackupPath}",
                    ex);
            }

            throw new InvalidOperationException($"恢复备份失败：{ex.Message}", ex);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static void AddDirectoryToArchive(ZipArchive archive, string sourceDirectory, string archiveRoot)
    {
        var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file)
                .Replace('\\', '/');
            var entryPath = $"{archiveRoot}/{relativePath}";
            AddFileToArchive(archive, file, entryPath);
        }
    }

    private static void AddFileToArchive(ZipArchive archive, string sourceFilePath, string entryPath)
    {
        var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);

        using var sourceStream = new FileStream(
            sourceFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        using var entryStream = entry.Open();
        sourceStream.CopyTo(entryStream);
    }

    private static void ReplaceAssetsDirectory(string extractedAssetsPath)
    {
        Directory.CreateDirectory(AppStoragePaths.AssetsDirectory);
        DeleteDirectoryContents(AppStoragePaths.AssetsDirectory);

        var files = Directory.GetFiles(extractedAssetsPath, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(extractedAssetsPath, file);
            var targetPath = Path.Combine(AppStoragePaths.AssetsDirectory, relativePath);
            var targetDirectory = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Copy(file, targetPath, overwrite: true);
        }
    }

    private static void DeleteDirectoryContents(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(directoryPath))
        {
            File.Delete(file);
        }

        foreach (var subDirectory in Directory.GetDirectories(directoryPath))
        {
            Directory.Delete(subDirectory, recursive: true);
        }
    }

    private static void TryDeleteDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return;
        }

        try
        {
            Directory.Delete(directoryPath, recursive: true);
        }
        catch
        {
        }
    }

    private static string NormalizeEntryPath(string entryPath)
    {
        return entryPath.Replace('\\', '/').TrimStart('/');
    }

    private sealed class BackupManifest
    {
        public required string AppName { get; init; }
        public required string Version { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required string DatabaseRelativePath { get; init; }
        public required string AssetsRelativePath { get; init; }
        public required string BackupType { get; init; }
    }
}
