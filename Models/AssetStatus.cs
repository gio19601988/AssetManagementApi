using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models;

public class AssetStatus
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string StatusName { get; set; } = null!;

    [MaxLength(100)]
    public string? Code { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string CreatedBy { get; set; } = null!;

    // Navigation
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}