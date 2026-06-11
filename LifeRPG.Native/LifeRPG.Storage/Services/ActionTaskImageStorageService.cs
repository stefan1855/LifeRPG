using System.Text;
using LifeRPG.Core.Models;

namespace LifeRPG.Storage.Services;

public sealed class ActionTaskImageStorageService
{
    private const string ActionTaskAssetsFolderName = "action-tasks";

    private static readonly HashSet<string> SupportedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".bmp",
        ".gif"
    ];

    public IReadOnlyList<ActionTaskImage> CopyImagesForTask(
        string actionTaskId,
        string taskTitle,
        DateTime taskCreatedAt,
        IReadOnlyList<string> sourceFilePaths)
    {
        if (sourceFilePaths.Count == 0)
        {
            return [];
        }

        var validSourceFiles = sourceFilePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Where(File.Exists)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            .ToList();

        if (validSourceFiles.Count == 0)
        {
            return [];
        }

        var createdAt = DateTime.Now;
        var taskFolderName = BuildTaskFolderName(actionTaskId, taskTitle, taskCreatedAt);
        var taskAssetsRoot = GetActionTaskAssetsRoot();
        var taskDirectory = Path.Combine(taskAssetsRoot, taskFolderName);
        Directory.CreateDirectory(taskDirectory);

        var copiedImages = new List<ActionTaskImage>();

        for (var index = 0; index < validSourceFiles.Count; index++)
        {
            var sourcePath = validSourceFiles[index];
            var imageId = Guid.NewGuid().ToString();
            var extension = NormalizeExtension(Path.GetExtension(sourcePath));
            var compressedExtension = ImageCompressor.GetCompressedExtension(sourcePath);
            var originalFileName = Path.GetFileName(sourcePath);
            var originalBaseName = Path.GetFileNameWithoutExtension(sourcePath);
            var safeBaseName = SanitizeSegment(originalBaseName, "image", 40);
            var fileName = $"{index + 1:00}_{safeBaseName}_{imageId[..8]}{compressedExtension}";
            var destinationPath = EnsureUniqueDestinationPath(taskDirectory, fileName);

            var fileSize = ImageCompressor.CompressAndSave(sourcePath, destinationPath);

            var relativePath = Path.Combine(
                ActionTaskAssetsFolderName,
                taskFolderName,
                Path.GetFileName(destinationPath));

            copiedImages.Add(new ActionTaskImage
            {
                Id = imageId,
                ActionTaskId = actionTaskId,
                OriginalFileName = originalFileName,
                FileName = Path.GetFileName(destinationPath),
                FilePath = destinationPath,
                RelativePath = relativePath,
                FileExtension = compressedExtension,
                FileSizeBytes = fileSize,
                CreatedAt = createdAt
            });
        }

        return copiedImages;
    }

    public bool DeleteImageFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        File.Delete(filePath);
        return true;
    }

    public void DeleteTaskImageFolderIfEmpty(string actionTaskId)
    {
        if (string.IsNullOrWhiteSpace(actionTaskId))
        {
            return;
        }

        var taskAssetsRoot = GetActionTaskAssetsRoot();
        if (!Directory.Exists(taskAssetsRoot))
        {
            return;
        }

        var actionTaskIdPrefix = GetIdPrefix(actionTaskId);
        foreach (var directory in Directory.EnumerateDirectories(taskAssetsRoot))
        {
            var directoryName = Path.GetFileName(directory);
            if (!directoryName.EndsWith($"_{actionTaskIdPrefix}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            TryDeleteEmptyDirectory(directory);
        }
    }

    public void TryDeleteEmptyDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return;
        }

        var normalizedAssetsRoot = Path.GetFullPath(AppStoragePaths.AssetsDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedActionTasksRoot = Path.GetFullPath(GetActionTaskAssetsRoot())
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedDirectoryPath = Path.GetFullPath(directoryPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.Equals(normalizedDirectoryPath, normalizedAssetsRoot, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedDirectoryPath, normalizedActionTasksRoot, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (Directory.EnumerateFileSystemEntries(normalizedDirectoryPath).Any())
        {
            return;
        }

        Directory.Delete(normalizedDirectoryPath, recursive: false);
    }

    private static string GetActionTaskAssetsRoot()
    {
        return Path.Combine(AppStoragePaths.AssetsDirectory, ActionTaskAssetsFolderName);
    }

    private static string BuildTaskFolderName(string actionTaskId, string taskTitle, DateTime taskCreatedAt)
    {
        var safeTaskTitle = SanitizeSegment(taskTitle, "ActionTask", 24);
        var timestamp = taskCreatedAt == default ? DateTime.Now : taskCreatedAt;
        return $"{timestamp:yyyyMMdd_HHmmss}_{safeTaskTitle}_{GetIdPrefix(actionTaskId)}";
    }

    private static string GetIdPrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        return value.Length >= 8 ? value[..8] : value;
    }

    private static string NormalizeExtension(string extension)
    {
        var normalized = string.IsNullOrWhiteSpace(extension) ? string.Empty : extension.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? string.Empty
            : normalized.StartsWith('.') ? normalized.ToLowerInvariant() : $".{normalized.ToLowerInvariant()}";
    }

    private static string EnsureUniqueDestinationPath(string directoryPath, string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var destinationPath = Path.Combine(directoryPath, fileName);

        if (!File.Exists(destinationPath))
        {
            return destinationPath;
        }

        var counter = 2;
        while (File.Exists(destinationPath))
        {
            destinationPath = Path.Combine(directoryPath, $"{baseName}_{counter:00}{extension}");
            counter++;
        }

        return destinationPath;
    }

    private static string SanitizeSegment(string? value, string fallback, int maxLength)
    {
        var source = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        var builder = new StringBuilder(source.Length);

        foreach (var character in source)
        {
            if (Path.GetInvalidFileNameChars().Contains(character))
            {
                continue;
            }

            builder.Append(character);
        }

        var sanitized = builder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = fallback;
        }

        return sanitized.Length > maxLength
            ? sanitized[..maxLength]
            : sanitized;
    }
}
