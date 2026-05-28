using LifeRPG.Core.Models;
using System.Text;

namespace LifeRPG.Storage.Services;

public sealed class ImageStorageService
{
    private static readonly HashSet<string> SupportedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".bmp",
        ".gif"
    ];

    public List<LifeRecordImage> CopyImagesForRecord(
        string lifeRecordId,
        string recordTitle,
        IEnumerable<string> sourceFilePaths)
    {
        var validSourceFiles = sourceFilePaths
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            .ToList();

        if (validSourceFiles.Count == 0)
        {
            return [];
        }

        var copiedImages = new List<LifeRecordImage>();
        var createdAt = DateTime.Now;
        var recordDirectoryName = BuildRecordDirectoryName(lifeRecordId, recordTitle, createdAt);
        var recordDirectory = Path.Combine(AppStoragePaths.AssetsDirectory, recordDirectoryName);

        Directory.CreateDirectory(recordDirectory);

        for (var index = 0; index < validSourceFiles.Count; index++)
        {
            var sourceFilePath = validSourceFiles[index];
            var extension = Path.GetExtension(sourceFilePath).ToLowerInvariant();
            var imageId = Guid.NewGuid().ToString();
            var sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilePath);
            var safeSourceFileName = SanitizeSegment(sourceFileNameWithoutExtension, "未命名图片", 40);
            var newFileName = $"{index + 1:00}_{safeSourceFileName}_{imageId[..8]}{extension}";

            var destinationPath = Path.Combine(recordDirectory, newFileName);
            File.Copy(sourceFilePath, destinationPath, overwrite: false);

            copiedImages.Add(new LifeRecordImage
            {
                Id = imageId,
                LifeRecordId = lifeRecordId,
                FileName = newFileName,
                FilePath = destinationPath,
                CreatedAt = createdAt
            });
        }

        return copiedImages;
    }

    private static string BuildRecordDirectoryName(string lifeRecordId, string recordTitle, DateTime createdAt)
    {
        var safeRecordTitle = SanitizeSegment(recordTitle, "未命名记录", 30);
        var recordIdPrefix = lifeRecordId.Length >= 8 ? lifeRecordId[..8] : lifeRecordId;
        return $"{createdAt:yyyyMMdd_HHmmss}_{safeRecordTitle}_{recordIdPrefix}";
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
