using Microsoft.AspNetCore.Mvc;
using CarpetProgressTracker.Services;

namespace CarpetProgressTracker.Controllers;

public class ProgressController : Controller
{
    private readonly OrderService _orderService;
    private readonly ImageService _imageService;
    private readonly ProgressCalculationService _progressCalculationService;

    public ProgressController(
        OrderService orderService,
        ImageService imageService,
        ProgressCalculationService progressCalculationService)
    {
        _orderService = orderService;
        _imageService = imageService;
        _progressCalculationService = progressCalculationService;
    }

    public async Task<IActionResult> Draw(int orderId)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitDraw(int orderId, IFormFile overlayImage)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }

        if (overlayImage == null || overlayImage.Length == 0)
        {
            TempData["Error"] = "Overlay image is required.";
            return RedirectToAction("Draw", new { orderId });
        }

        var overlayUrl = await _imageService.SaveImageAsync(overlayImage, "overlays", order.StandardWidth, order.StandardHeight);

        ProgressResult result;
        if (!string.IsNullOrEmpty(order.MaskImageUrl))
        {
            result = await _progressCalculationService.CalculateProgressWithMaskAsync(
                order.MaskImageUrl,
                order.BaseImageUrl!,
                overlayUrl,
                "overlay",
                order.StandardWidth,
                order.StandardHeight);
        }
        else
        {
            result = await _progressCalculationService.CalculateProgressAsync(
                order.BaseImageUrl!,
                overlayUrl,
                "overlay",
                order.StandardWidth,
                order.StandardHeight);
        }

        var mergedUrl = await _imageService.CompositeImagesAsync(order.BaseImageUrl!, result.ImageUrl);

        await _orderService.UpdateProgressAsync(order, mergedUrl, result.ProgressPercentage, result.ImageUrl);

        return RedirectToAction("Result", new { orderId, progress = result.ProgressPercentage });
    }

    public async Task<IActionResult> Upload(int orderId)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitUpload(int orderId, IFormFile uploadedImage)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }

        if (uploadedImage == null || uploadedImage.Length == 0)
        {
            TempData["Error"] = "Uploaded image is required.";
            return RedirectToAction("Upload", new { orderId });
        }

        var imageUrl = await _imageService.SaveImageAsync(uploadedImage, "uploads", order.StandardWidth, order.StandardHeight);

        ProgressResult result;
        if (!string.IsNullOrEmpty(order.MaskImageUrl))
        {
            result = await _progressCalculationService.CalculateProgressWithMaskAsync(
                order.MaskImageUrl,
                order.BaseImageUrl!,
                imageUrl,
                "upload_diff",
                order.StandardWidth,
                order.StandardHeight);
        }
        else
        {
            result = await _progressCalculationService.CalculateProgressAsync(
                order.BaseImageUrl!,
                imageUrl,
                "upload_diff",
                order.StandardWidth,
                order.StandardHeight);
        }

        await _orderService.UpdateProgressAsync(order, result.ImageUrl, result.ProgressPercentage);

        return RedirectToAction("Result", new { orderId, progress = result.ProgressPercentage });
    }

    public async Task<IActionResult> Result(int orderId, decimal progress)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }

        ViewBag.Progress = progress;
        return View(order);
    }
}
