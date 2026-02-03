using Microsoft.AspNetCore.Mvc;
using CarpetProgressTracker.Services;

namespace CarpetProgressTracker.Controllers;

public class CompareController : Controller
{
    private readonly OrderService _orderService;
    private readonly ImageService _imageService;
    private readonly ProgressCalculationService _progressCalculationService;

    public CompareController(
        OrderService orderService,
        ImageService imageService,
        ProgressCalculationService progressCalculationService)
    {
        _orderService = orderService;
        _imageService = imageService;
        _progressCalculationService = progressCalculationService;
    }

    public async Task<IActionResult> Index(int orderId)
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
    public async Task<IActionResult> Compare(int orderId, IFormFile compareImage, string mode)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }

        if (compareImage == null || compareImage.Length == 0)
        {
            TempData["Error"] = "Image is required for comparison.";
            return RedirectToAction("Index", new { orderId });
        }

        if (string.IsNullOrWhiteSpace(mode))
        {
            mode = order.ProgressMode;
        }

        var imageUrl = await _imageService.SaveImageAsync(compareImage, "compare", order.StandardWidth, order.StandardHeight);

        ProgressResult result;
        if (!string.IsNullOrEmpty(order.MaskImageUrl))
        {
            result = await _progressCalculationService.CalculateProgressWithMaskAsync(
                order.MaskImageUrl,
                order.BaseImageUrl!,
                imageUrl,
                mode,
                order.StandardWidth,
                order.StandardHeight);
        }
        else
        {
            result = await _progressCalculationService.CalculateProgressAsync(
                order.BaseImageUrl!,
                imageUrl,
                mode,
                order.StandardWidth,
                order.StandardHeight);
        }

        string displayUrl;
        string? overlayUrl = null;

        if (mode == "overlay")
        {
            displayUrl = await _imageService.CompositeImagesAsync(order.BaseImageUrl!, result.ImageUrl);
            overlayUrl = result.ImageUrl;
        }
        else
        {
            displayUrl = result.ImageUrl;
        }

        await _orderService.UpdateProgressAsync(order, displayUrl, result.ProgressPercentage, overlayUrl);

        return RedirectToAction("Result", new { orderId, imageUrl = displayUrl, progress = result.ProgressPercentage, mode });
    }

    public async Task<IActionResult> Result(int orderId, string imageUrl, decimal progress, string mode)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }

        ViewBag.CompareImageUrl = imageUrl;
        ViewBag.Progress = progress;
        ViewBag.Mode = mode;
        return View(order);
    }

    public async Task<IActionResult> History(int orderId)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }

        var history = await _orderService.GetProgressHistoryAsync(orderId);
        ViewBag.Order = order;
        return View(history);
    }
}
