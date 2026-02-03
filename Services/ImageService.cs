using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CarpetProgressTracker.Services;

public class ImageService
{
    private readonly IWebHostEnvironment _environment;
    private const int AlphaThreshold = 10;
    private const double ColorTolerance = 30.0;

    public ImageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveImageAsync(IFormFile file, string subFolder)
    {
        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/{subFolder}/{fileName}";
    }

    public (int Width, int Height, int TotalPixels) GetImageInfo(string imageUrl)
    {
        var filePath = GetPhysicalPath(imageUrl);
        using var image = Image.Load<Rgba32>(filePath);

        int totalPixels = 0;
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x].A > AlphaThreshold)
                    {
                        totalPixels++;
                    }
                }
            }
        });

        return (image.Width, image.Height, totalPixels);
    }

    public async Task<string> ResizeImageAsync(string imageUrl, int targetWidth, int targetHeight)
    {
        var sourcePath = GetPhysicalPath(imageUrl);
        using var image = Image.Load<Rgba32>(sourcePath);

        if (image.Width == targetWidth && image.Height == targetHeight)
        {
            return imageUrl;
        }

        image.Mutate(x => x.Resize(targetWidth, targetHeight));

        var directory = Path.GetDirectoryName(sourcePath)!;
        var newFileName = $"{Guid.NewGuid()}.png";
        var newPath = Path.Combine(directory, newFileName);

        await image.SaveAsPngAsync(newPath);

        var urlDirectory = Path.GetDirectoryName(imageUrl)!.Replace("\\", "/");
        return $"{urlDirectory}/{newFileName}";
    }

    public decimal CalculateOverlayProgress(string baseImageUrl, string overlayImageUrl)
    {
        var basePath = GetPhysicalPath(baseImageUrl);
        var overlayPath = GetPhysicalPath(overlayImageUrl);

        using var baseImage = Image.Load<Rgba32>(basePath);
        using var overlayImage = Image.Load<Rgba32>(overlayPath);

        if (overlayImage.Width != baseImage.Width || overlayImage.Height != baseImage.Height)
        {
            overlayImage.Mutate(x => x.Resize(baseImage.Width, baseImage.Height));
        }

        int totalPixels = 0;
        int completedPixels = 0;

        baseImage.ProcessPixelRows(overlayImage, (baseAccessor, overlayAccessor) =>
        {
            for (int y = 0; y < baseAccessor.Height; y++)
            {
                var baseRow = baseAccessor.GetRowSpan(y);
                var overlayRow = overlayAccessor.GetRowSpan(y);

                for (int x = 0; x < baseRow.Length; x++)
                {
                    if (baseRow[x].A > AlphaThreshold)
                    {
                        totalPixels++;
                        if (overlayRow[x].A > AlphaThreshold)
                        {
                            completedPixels++;
                        }
                    }
                }
            }
        });

        if (totalPixels == 0) return 0;
        return Math.Round((decimal)completedPixels / totalPixels * 100, 2);
    }

    public decimal CalculateDiffProgress(string baseImageUrl, string uploadedImageUrl)
    {
        var basePath = GetPhysicalPath(baseImageUrl);
        var uploadedPath = GetPhysicalPath(uploadedImageUrl);

        using var baseImage = Image.Load<Rgba32>(basePath);
        using var uploadedImage = Image.Load<Rgba32>(uploadedPath);

        if (uploadedImage.Width != baseImage.Width || uploadedImage.Height != baseImage.Height)
        {
            uploadedImage.Mutate(x => x.Resize(baseImage.Width, baseImage.Height));
        }

        int totalPixels = 0;
        int completedPixels = 0;

        baseImage.ProcessPixelRows(uploadedImage, (baseAccessor, uploadedAccessor) =>
        {
            for (int y = 0; y < baseAccessor.Height; y++)
            {
                var baseRow = baseAccessor.GetRowSpan(y);
                var uploadedRow = uploadedAccessor.GetRowSpan(y);

                for (int x = 0; x < baseRow.Length; x++)
                {
                    if (baseRow[x].A > AlphaThreshold)
                    {
                        totalPixels++;
                        double distance = CalculateColorDistance(baseRow[x], uploadedRow[x]);
                        if (distance > ColorTolerance)
                        {
                            completedPixels++;
                        }
                    }
                }
            }
        });

        if (totalPixels == 0) return 0;
        return Math.Round((decimal)completedPixels / totalPixels * 100, 2);
    }

    private static double CalculateColorDistance(Rgba32 color1, Rgba32 color2)
    {
        int rDiff = color1.R - color2.R;
        int gDiff = color1.G - color2.G;
        int bDiff = color1.B - color2.B;
        return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
    }

    private string GetPhysicalPath(string imageUrl)
    {
        var relativePath = imageUrl.TrimStart('/');
        return Path.Combine(_environment.WebRootPath, relativePath);
    }
}
