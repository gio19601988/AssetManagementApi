using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models;
public class InventorySession
{
    public int Id { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public int? DepartmentId { get; set; }  // ახალი: optional
    public Department? Department { get; set; }  // navigation
    public string? Notes { get; set; }

    public ICollection<InventoryScan> Scans { get; set; } = new List<InventoryScan>();
}