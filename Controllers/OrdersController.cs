using Microsoft.AspNetCore.Mvc;
using CarpetProgressTracker.Services;

namespace CarpetProgressTracker.Controllers;

public class OrdersController : Controller
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        return View(orders);
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
        await _orderService.DeleteOrderAsync(id);
        return RedirectToAction("Index");
    }
}
