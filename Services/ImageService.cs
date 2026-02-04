using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CarpetProgressTracker.Services;

public class ImageService
{
    private readonly IWebHostEnvironment _environment;
    private const int AlphaThreshold = 10;
    private const double ColorTolerance = 30.0;
    private const int WhiteThreshold = 240;

    public ImageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveImageAsync(IFormFile file, string subFolder)
    {
        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadsPath);

        using var inputStream = file.OpenReadStream();
        using var image = await Image.LoadAsync<Rgba32>(inputStream);

        var fileName = $"{Guid.NewGuid()}.png";
        var filePath = Path.Combine(uploadsPath, fileName);

        await image.SaveAsPngAsync(filePath);

        return $"/uploads/{subFolder}/{fileName}";
    }

    public async Task<string> SaveImageAsync(IFormFile file, string subFolder, int targetWidth, int targetHeight)
    {
        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadsPath);

        using var inputStream = file.OpenReadStream();
        using var image = await Image.LoadAsync<Rgba32>(inputStream);

        if (image.Width != targetWidth || image.Height != targetHeight)
        {
            image.Mutate(x => x.Resize(targetWidth, targetHeight));
        }

        var fileName = $"{Guid.NewGuid()}.png";
        var filePath = Path.Combine(uploadsPath, fileName);

        await image.SaveAsPngAsync(filePath);

        return $"/uploads/{subFolder}/{fileName}";
    }

    public async Task<string> GenerateMaskAsync(string baseImageUrl)
    {
        var basePath = GetPhysicalPath(baseImageUrl);
        using var baseImage = Image.Load<Rgba32>(basePath);

        using var maskImage = new Image<Rgba32>(baseImage.Width, baseImage.Height);

        baseImage.ProcessPixelRows(maskImage, (baseAccessor, maskAccessor) =>
        {
            for (int y = 0; y < baseAccessor.Height; y++)
            {
                var baseRow = baseAccessor.GetRowSpan(y);
                var maskRow = maskAccessor.GetRowSpan(y);

                for (int x = 0; x < baseRow.Length; x++)
                {
                    bool isWorkArea = !IsBackground(baseRow[x]);
                    maskRow[x] = isWorkArea ? new Rgba32(255, 255, 255, 255) : new Rgba32(0, 0, 0, 255);
                }
            }
        });

        var masksPath = Path.Combine(_environment.WebRootPath, "uploads", "masks");
        Directory.CreateDirectory(masksPath);

        var maskFileName = $"{Guid.NewGuid()}.png";
        var maskFilePath = Path.Combine(masksPath, maskFileName);

        await maskImage.SaveAsPngAsync(maskFilePath);

        return $"/uploads/masks/{maskFileName}";
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

    public (int Width, int Height, int TotalPixels) GetImageInfoWithMask(string baseImageUrl)
    {
        var basePath = GetPhysicalPath(baseImageUrl);
        using var baseImage = Image.Load<Rgba32>(basePath);

        int totalPixels = 0;
        baseImage.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    if (!IsBackground(row[x]))
                    {
                        totalPixels++;
                    }
                }
            }
        });

        return (baseImage.Width, baseImage.Height, totalPixels);
    }

    public async Task<string> CompositeImagesAsync(string baseImageUrl, string overlayImageUrl)
    {
        var basePath = GetPhysicalPath(baseImageUrl);
        var overlayPath = GetPhysicalPath(overlayImageUrl);

        using var baseImage = Image.Load<Rgba32>(basePath);
        using var overlayImage = Image.Load<Rgba32>(overlayPath);

        if (overlayImage.Width != baseImage.Width || overlayImage.Height != baseImage.Height)
        {
            overlayImage.Mutate(x => x.Resize(baseImage.Width, baseImage.Height));
        }

        overlayImage.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x].R > WhiteThreshold && row[x].G > WhiteThreshold && row[x].B > WhiteThreshold)
                    {
                        row[x] = new Rgba32(255, 255, 255, 0);
                    }
                }
            }
        });

        baseImage.Mutate(x => x.DrawImage(overlayImage, 1f));

        var mergedPath = Path.Combine(_environment.WebRootPath, "uploads", "merged");
        Directory.CreateDirectory(mergedPath);

        var mergedFileName = $"{Guid.NewGuid()}.png";
        var mergedFilePath = Path.Combine(mergedPath, mergedFileName);

        await baseImage.SaveAsPngAsync(mergedFilePath);

        return $"/uploads/merged/{mergedFileName}";
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
                        if (IsDrawnPixel(overlayRow[x]))
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

    public decimal CalculateOverlayProgressWithMask(string maskImageUrl, string overlayImageUrl)
    {
        var maskPath = GetPhysicalPath(maskImageUrl);
        var overlayPath = GetPhysicalPath(overlayImageUrl);

        using var maskImage = Image.Load<Rgba32>(maskPath);
        using var overlayImage = Image.Load<Rgba32>(overlayPath);

        if (overlayImage.Width != maskImage.Width || overlayImage.Height != maskImage.Height)
        {
            overlayImage.Mutate(x => x.Resize(maskImage.Width, maskImage.Height));
        }

        int totalPixels = 0;
        int completedPixels = 0;

        maskImage.ProcessPixelRows(overlayImage, (maskAccessor, overlayAccessor) =>
        {
            for (int y = 0; y < maskAccessor.Height; y++)
            {
                var maskRow = maskAccessor.GetRowSpan(y);
                var overlayRow = overlayAccessor.GetRowSpan(y);

                for (int x = 0; x < maskRow.Length; x++)
                {
                    if (IsWorkAreaInMask(maskRow[x]))
                    {
                        totalPixels++;
                        if (IsDrawnPixel(overlayRow[x]))
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

    public decimal CalculateDiffProgressWithMask(string maskImageUrl, string baseImageUrl, string uploadedImageUrl)
    {
        var maskPath = GetPhysicalPath(maskImageUrl);
        var basePath = GetPhysicalPath(baseImageUrl);
        var uploadedPath = GetPhysicalPath(uploadedImageUrl);

        using var maskImage = Image.Load<Rgba32>(maskPath);
        using var baseImage = Image.Load<Rgba32>(basePath);
        using var uploadedImage = Image.Load<Rgba32>(uploadedPath);

        if (uploadedImage.Width != maskImage.Width || uploadedImage.Height != maskImage.Height)
        {
            uploadedImage.Mutate(x => x.Resize(maskImage.Width, maskImage.Height));
        }

        int totalPixels = 0;
        int completedPixels = 0;

        for (int y = 0; y < maskImage.Height; y++)
        {
            for (int x = 0; x < maskImage.Width; x++)
            {
                var maskPixel = maskImage[x, y];
                if (IsWorkAreaInMask(maskPixel))
                {
                    totalPixels++;
                    var basePixel = baseImage[x, y];
                    var uploadedPixel = uploadedImage[x, y];
                    double distance = CalculateColorDistance(basePixel, uploadedPixel);
                    if (distance > ColorTolerance)
                    {
                        completedPixels++;
                    }
                }
            }
        }

        if (totalPixels == 0) return 0;
        return Math.Round((decimal)completedPixels / totalPixels * 100, 2);
    }

    public async Task<string> GenerateDiffOverlayAsync(string baseImageUrl, string uploadedImageUrl, int targetWidth, int targetHeight)
    {
        var basePath = GetPhysicalPath(baseImageUrl);
        var uploadedPath = GetPhysicalPath(uploadedImageUrl);

        using var baseImage = Image.Load<Rgba32>(basePath);
        using var uploadedImage = Image.Load<Rgba32>(uploadedPath);

        if (uploadedImage.Width != targetWidth || uploadedImage.Height != targetHeight)
        {
            uploadedImage.Mutate(x => x.Resize(targetWidth, targetHeight));
        }
        if (baseImage.Width != targetWidth || baseImage.Height != targetHeight)
        {
            baseImage.Mutate(x => x.Resize(targetWidth, targetHeight));
        }

        using var overlayImage = new Image<Rgba32>(targetWidth, targetHeight);
        var completedColor = new Rgba32(255, 100, 100, 255);

        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                var basePixel = baseImage[x, y];
                var uploadedPixel = uploadedImage[x, y];

                if (basePixel.A > AlphaThreshold)
                {
                    double distance = CalculateColorDistance(basePixel, uploadedPixel);
                    if (distance > ColorTolerance)
                    {
                        overlayImage[x, y] = completedColor;
                    }
                    else
                    {
                        overlayImage[x, y] = new Rgba32(255, 255, 255, 255);
                    }
                }
                else
                {
                    overlayImage[x, y] = new Rgba32(240, 240, 240, 255);
                }
            }
        }

        var overlaysPath = Path.Combine(_environment.WebRootPath, "uploads", "overlays");
        Directory.CreateDirectory(overlaysPath);

        var overlayFileName = $"{Guid.NewGuid()}.png";
        var overlayFilePath = Path.Combine(overlaysPath, overlayFileName);

        await overlayImage.SaveAsPngAsync(overlayFilePath);

        return $"/uploads/overlays/{overlayFileName}";
    }

    public async Task<string> GenerateDiffOverlayWithMaskAsync(string maskImageUrl, string baseImageUrl, string uploadedImageUrl, int targetWidth, int targetHeight)
    {
        var maskPath = GetPhysicalPath(maskImageUrl);
        var basePath = GetPhysicalPath(baseImageUrl);
        var uploadedPath = GetPhysicalPath(uploadedImageUrl);

        using var maskImage = Image.Load<Rgba32>(maskPath);
        using var baseImage = Image.Load<Rgba32>(basePath);
        using var uploadedImage = Image.Load<Rgba32>(uploadedPath);

        if (uploadedImage.Width != targetWidth || uploadedImage.Height != targetHeight)
        {
            uploadedImage.Mutate(x => x.Resize(targetWidth, targetHeight));
        }
        if (baseImage.Width != targetWidth || baseImage.Height != targetHeight)
        {
            baseImage.Mutate(x => x.Resize(targetWidth, targetHeight));
        }
        if (maskImage.Width != targetWidth || maskImage.Height != targetHeight)
        {
            maskImage.Mutate(x => x.Resize(targetWidth, targetHeight));
        }

        using var overlayImage = new Image<Rgba32>(targetWidth, targetHeight);
        var completedColor = new Rgba32(255, 100, 100, 255);

        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                var maskPixel = maskImage[x, y];

                if (IsWorkAreaInMask(maskPixel))
                {
                    var basePixel = baseImage[x, y];
                    var uploadedPixel = uploadedImage[x, y];
                    double distance = CalculateColorDistance(basePixel, uploadedPixel);
                    if (distance > ColorTolerance)
                    {
                        overlayImage[x, y] = completedColor;
                    }
                    else
                    {
                        overlayImage[x, y] = new Rgba32(255, 255, 255, 255);
                    }
                }
                else
                {
                    overlayImage[x, y] = new Rgba32(240, 240, 240, 255);
                }
            }
        }

        var overlaysPath = Path.Combine(_environment.WebRootPath, "uploads", "overlays");
        Directory.CreateDirectory(overlaysPath);

        var overlayFileName = $"{Guid.NewGuid()}.png";
        var overlayFilePath = Path.Combine(overlaysPath, overlayFileName);

        await overlayImage.SaveAsPngAsync(overlayFilePath);

        return $"/uploads/overlays/{overlayFileName}";
    }

    private static bool IsBackground(Rgba32 pixel)
    {
        if (pixel.A <= AlphaThreshold)
        {
            return true;
        }
        return pixel.R > WhiteThreshold && pixel.G > WhiteThreshold && pixel.B > WhiteThreshold;
    }

    private static bool IsWorkAreaInMask(Rgba32 maskPixel)
    {
        return maskPixel.R > 128;
    }

    private static bool IsDrawnPixel(Rgba32 pixel)
    {
        return pixel.A > AlphaThreshold;
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
