using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarpetProgressTracker.Models;

[Table("orders")]
public class Order
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("order_number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Column("base_image_url")]
    public string? BaseImageUrl { get; set; }

    [Column("mask_image_url")]
    public string? MaskImageUrl { get; set; }

    [Column("standard_width")]
    public int StandardWidth { get; set; }

    [Column("standard_height")]
    public int StandardHeight { get; set; }

    [Column("total_pixels")]
    public int TotalPixels { get; set; }

    [Column("current_image_url")]
    public string? CurrentImageUrl { get; set; }

    [Column("current_progress", TypeName = "decimal(5,2)")]
    public decimal CurrentProgress { get; set; }

    [MaxLength(20)]
    [Column("progress_mode")]
    public string ProgressMode { get; set; } = "overlay";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProgressHistory> ProgressHistories { get; set; } = new List<ProgressHistory>();
}
