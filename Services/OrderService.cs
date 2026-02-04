using CarpetProgressTracker.Data;
using CarpetProgressTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace CarpetProgressTracker.Services;

public class OrderService
{
    private readonly AppDbContext _context;
    private readonly ImageService _imageService;

    public OrderService(AppDbContext context, ImageService imageService)
    {
        _context = context;
        _imageService = imageService;
    }

    public async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .OrderByDescending(o => o.UpdatedAt)
            .ToListAsync();
    }

    public async Task<(List<Order> Orders, int TotalCount)> GetOrdersPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        string? mode = null)
    {
        var query = _context.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.OrderNumber.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrWhiteSpace(mode) && mode != "all")
            query = query.Where(o => o.ProgressMode == mode);

        var totalCount = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (orders, totalCount);
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.ProgressHistories.OrderByDescending(p => p.RecordedAt))
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
    {
        return await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<Order> CreateOrderAsync(string orderNumber, IFormFile baseImage, string progressMode)
    {
        var imageUrl = await _imageService.SaveImageAsync(baseImage, "base");
        var maskUrl = await _imageService.GenerateMaskAsync(imageUrl);
        var (width, height, totalPixels) = _imageService.GetImageInfoWithMask(imageUrl);

        var order = new Order
        {
            OrderNumber = orderNumber,
            BaseImageUrl = imageUrl,
            MaskImageUrl = maskUrl,
            StandardWidth = width,
            StandardHeight = height,
            TotalPixels = totalPixels,
            ProgressMode = progressMode,
            CurrentProgress = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order;
    }

    public async Task UpdateProgressAsync(Order order, string imageUrl, decimal progress, string? overlayUrl = null)
    {
        order.CurrentImageUrl = imageUrl;
        order.CurrentOverlayUrl = overlayUrl;
        order.CurrentProgress = progress;
        order.UpdatedAt = DateTime.UtcNow;

        var history = new ProgressHistory
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            ImageUrl = imageUrl,
            OverlayUrl = overlayUrl,
            ProgressPercentage = progress,
            RecordedAt = DateTime.UtcNow
        };

        _context.ProgressHistories.Add(history);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ProgressHistory>> GetProgressHistoryAsync(int orderId)
    {
        return await _context.ProgressHistories
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.RecordedAt)
            .ToListAsync();
    }

    public async Task DeleteOrderAsync(int id)
    {
        var order = await _context.Orders
            .Include(o => o.ProgressHistories)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return;

        if (order.ProgressHistories != null && order.ProgressHistories.Count > 0)
            _context.ProgressHistories.RemoveRange(order.ProgressHistories);

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
    }

}
