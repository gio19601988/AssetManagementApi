using System.ComponentModel.DataAnnotations;
public class AppUser
{
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Username { get; set; } = string.Empty;

    [StringLength(255)]
    public string? FullName { get; set; }

    [Required]
    [StringLength(100)]
    public string Role { get; set; } = "User";  // NOT NULL

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]  // <--- მთავარი ცვლილება!
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;  // არა nullable!
    // ახალი ველი — ელ.ფოსტა
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;  // ← ეს დაამატე
}