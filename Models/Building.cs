using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models;

public class Building
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string CreatedBy { get; set; } = null!;

    // Navigation
    public ICollection<Location> Locations { get; set; } = new List<Location>();
}