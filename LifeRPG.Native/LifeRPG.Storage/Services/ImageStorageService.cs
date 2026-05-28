using LifeRPG.Core.Models;

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

    public List<LifeRecordImage> CopyImagesForRecord(string lifeRecordId, IEnumerable<string> sourceFilePaths)
    {
        var copiedImages = new List<LifeRecordImage>();
        var recordDirectory = Path.Combine(AppStoragePaths.AssetsDirectory, lifeRecordId);

        Directory.CreateDirectory(recordDirectory);

        foreach (var sourceFilePath in sourceFilePaths)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                continue;
            }

            var extension = Path.GetExtension(sourceFilePath).ToLowerInvariant();
            if (!SupportedExtensions.Contains(extension))
            {
                continue;
            }

            var newFileName = $"{Guid.NewGuid()}{extension}";
            var destinationPath = Path.Combine(recordDirectory, newFileName);

            if (File.Exists(destinationPath))
            {
                continue;
            }

            File.Copy(sourceFilePath, destinationPath, overwrite: false);

            copiedImages.Add(new LifeRecordImage
            {
                Id = Guid.NewGuid().ToString(),
                LifeRecordId = lifeRecordId,
                FileName = newFileName,
                FilePath = destinationPath,
                CreatedAt = DateTime.Now
            });
        }

        return copiedImages;
    }
}
