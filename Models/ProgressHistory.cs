using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarpetProgressTracker.Models;

[Table("progress_history")]
public class ProgressHistory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("order_id")]
    public int OrderId { get; set; }

    [MaxLength(50)]
    [Column("order_number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("progress_percentage", TypeName = "decimal(5,2)")]
    public decimal ProgressPercentage { get; set; }

    [Column("recorded_at")]
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;
}
