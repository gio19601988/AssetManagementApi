using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models;

public class Location
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int BuildingId { get; set; }

    [Required]
    [MaxLength(100)]
    public string RoomNumber { get; set; } = null!;
    public string? Description { get; set; }  // ← დამატებული

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(255)]
    public string CreatedBy { get; set; } = null!;
    public DateTime? UpdatedAt { get; set; }  // ← დამატებული
    public string? UpdatedBy { get; set; }   // ← დამატებული

    // Navigation
    public Building Building { get; set; } = null!;
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}