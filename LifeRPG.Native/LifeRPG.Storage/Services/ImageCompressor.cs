using System.Drawing;
using System.Drawing.Imaging;

namespace LifeRPG.Storage.Services;

public static class ImageCompressor
{
    private const int MaxDimension = 1920;
    private const long Quality = 85L;

    /// <summary>
    /// Copies an image file to the destination, compressing it to JPEG
    /// with a max dimension of 1920px and quality 85.
    /// Returns the actual file size in bytes after compression.
    /// GIF files are copied as-is to preserve animation.
    /// </summary>
    public static long CompressAndSave(string sourcePath, string destinationPath)
    {
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();

        // Don't compress GIFs — they may be animated
        if (extension == ".gif")
        {
            File.Copy(sourcePath, destinationPath, overwrite: false);
            return new FileInfo(destinationPath).Length;
        }

        using var sourceImage = Image.FromFile(sourcePath);
        var (width, height) = CalculateTargetSize(sourceImage.Width, sourceImage.Height);

        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

        graphics.DrawImage(sourceImage, 0, 0, width, height);

        // Save as JPEG
        var jpegEncoder = GetJpegEncoder();
        using var parameters = new EncoderParameters(1);
        using var qualityParam = new EncoderParameter(Encoder.Quality, Quality);
        parameters.Param[0] = qualityParam;

        bitmap.Save(destinationPath, jpegEncoder, parameters);

        return new FileInfo(destinationPath).Length;
    }

    /// <summary>
    /// Returns the compressed extension: always ".jpg" unless GIF.
    /// </summary>
    public static string GetCompressedExtension(string sourcePath)
    {
        var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        return ext == ".gif" ? ".gif" : ".jpg";
    }

    private static (int width, int height) CalculateTargetSize(int originalWidth, int originalHeight)
    {
        if (originalWidth <= MaxDimension && originalHeight <= MaxDimension)
        {
            return (originalWidth, originalHeight);
        }

        var ratio = (double)originalWidth / originalHeight;

        if (originalWidth >= originalHeight)
        {
            return (MaxDimension, (int)(MaxDimension / ratio));
        }

        return ((int)(MaxDimension * ratio), MaxDimension);
    }

    private static ImageCodecInfo GetJpegEncoder()
    {
        return ImageCodecInfo.GetImageEncoders()
            .First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
    }
}
