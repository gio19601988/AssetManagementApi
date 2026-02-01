using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManagementApi.Models;

    public class UserRole
    {
        public int UserId { get; set; }
        // public ApplicationUser User { get; set; } // თუ Identity გაქვს, შეცვალე

        public int RoleId { get; set; }
        public Role? Role { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public int? AssignedBy { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
