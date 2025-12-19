using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models;

public class Department
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string CreatedBy { get; set; } = null!;

    // წაშლილია:
    // public DateTime? UpdatedAt { get; set; }
    // public string? UpdatedBy { get; set; }

    // Navigation
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}