using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementApi.Models;

public class InventoryScan
{
    public int Id { get; set; }

    public int SessionId { get; set; }
    public InventorySession Session { get; set; } = null!;

    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public string ScannedBarcode { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

    // ეს არის მხოლოდ სახელი, არა ID!
    public string ScannedBy { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
}