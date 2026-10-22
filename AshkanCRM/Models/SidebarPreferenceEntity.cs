using System.ComponentModel.DataAnnotations;
namespace AshkanCRM.Models;
public sealed class SidebarPreferenceEntity {
 [Key, MaxLength(450)] public string UserId { get; set; } = "";
 [MaxLength(2000)] public string GroupOrderJson { get; set; } = "[]";
 [MaxLength(16)] public string Density { get; set; } = "normal";
 [MaxLength(2000)] public string FavoritesJson { get; set; } = "[]";
 [MaxLength(2000)] public string GroupExpandedJson { get; set; } = "{}";
 [MaxLength(16)] public string Accent { get; set; } = "indigo";
 [MaxLength(16)] public string FontSize { get; set; } = "normal";
 [MaxLength(16)] public string Appearance { get; set; } = "system";
 public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
