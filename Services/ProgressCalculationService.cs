namespace CarpetProgressTracker.Services;

public class ProgressCalculationService
{
    private readonly ImageService _imageService;

    public ProgressCalculationService(ImageService imageService)
    {
        _imageService = imageService;
    }

    public async Task<ProgressResult> CalculateProgressAsync(
        string baseImageUrl,
        string inputImageUrl,
        string mode,
        int standardWidth,
        int standardHeight)
    {
        var resizedUrl = await _imageService.ResizeImageAsync(inputImageUrl, standardWidth, standardHeight);

        decimal progress = mode switch
        {
            "overlay" => _imageService.CalculateOverlayProgress(baseImageUrl, resizedUrl),
            "upload_diff" => _imageService.CalculateDiffProgress(baseImageUrl, resizedUrl),
            _ => throw new ArgumentException($"Invalid mode: {mode}")
        };

        return new ProgressResult
        {
            ImageUrl = resizedUrl,
            ProgressPercentage = progress
        };
    }
}

public class ProgressResult
{
    public string ImageUrl { get; set; } = string.Empty;
    public decimal ProgressPercentage { get; set; }
}
