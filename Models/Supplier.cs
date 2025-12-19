using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models;

public class Supplier
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [MaxLength(255)]
    public string? ContactPerson { get; set; }

    [MaxLength(100)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    [MaxLength(255)]
    public string CreatedBy { get; set; } = null!;

    [MaxLength(100)]
    public string? Code { get; set; }

    // Navigation
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}