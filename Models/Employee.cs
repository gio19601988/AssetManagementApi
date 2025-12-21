using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models;

public class Employee
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Position { get; set; }

    public int? DepartmentId { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }

    // Navigation
    public Department? Department { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; }  // ← non-nullable, მაგრამ default მივცეთ

    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<Asset> ResponsibleAssets { get; set; } = new List<Asset>();
}