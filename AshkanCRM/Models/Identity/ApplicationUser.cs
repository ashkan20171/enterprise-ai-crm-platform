using Microsoft.AspNetCore.Identity;
namespace AshkanCRM.Models.Identity;
public class ApplicationUser : IdentityUser {
 public string DisplayName { get; set; } = "";
 public string Department { get; set; } = "";
 public string Initials { get; set; } = "";
 public bool IsActive { get; set; } = true;
 public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
 public DateTime? LastLoginAtUtc { get; set; }
}
