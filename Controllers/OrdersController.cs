using Microsoft.AspNetCore.Mvc;
using CarpetProgressTracker.Services;
using CarpetProgressTracker.Models;

namespace CarpetProgressTracker.Controllers;

public class OrdersController : Controller
{
    private readonly OrderService _orderService;
    private static readonly int[] AllowedPageSizes = { 5, 10, 25, 50, 100 };

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? search = null, string? mode = null)
    {
        if (page < 1) page = 1;
        if (!AllowedPageSizes.Contains(pageSize)) pageSize = 10;

        var (orders, totalCount) = await _orderService.GetOrdersPagedAsync(page, pageSize, search, mode);

        var viewModel = new OrdersListViewModel
        {
            Orders = orders,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = search,
            Mode = mode
        };

        return View(viewModel);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string orderNumber, IFormFile baseImage, string progressMode)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            ModelState.AddModelError("orderNumber", "Order number is required.");
            return View();
        }

        if (baseImage == null || baseImage.Length == 0)
        {
            ModelState.AddModelError("baseImage", "Base image is required.");
            return View();
        }

        var existingOrder = await _orderService.GetOrderByNumberAsync(orderNumber);
        if (existingOrder != null)
        {
            ModelState.AddModelError("orderNumber", "Order number already exists.");
            return View();
        }

        if (string.IsNullOrWhiteSpace(progressMode))
        {
            progressMode = "overlay";
        }

        var order = await _orderService.CreateOrderAsync(orderNumber, baseImage, progressMode);
        return RedirectToAction("Details", new { id = order.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _orderService.DeleteOrderAsync(id);
            TempData["Success"] = "Order deleted successfully.";
            return RedirectToAction("Index");
        }
        catch (Exception)
        {
            TempData["Error"] = "Failed to delete order.";
            return RedirectToAction("Details", new { id });
        }
    }
}
