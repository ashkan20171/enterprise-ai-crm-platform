using System.Security.Claims;
using System.Text.Json;
using AshkanCRM.Data;
using AshkanCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AshkanCRM.Controllers;

[Authorize]
[Route("api/sidebar-preferences")]
public sealed class SidebarPreferencesController(CrmDbContext db) : Controller {
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) {
        if (UserId is not { Length: > 0 } userId) return Unauthorized();
        var entity = await db.CrmSidebarPreferences.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (entity is null) return Ok(new { hasSaved = false });
        return Ok(new { hasSaved = true, groupOrder = JsonSerializer.Deserialize<string[]>(entity.GroupOrderJson), density = entity.Density, favorites = JsonSerializer.Deserialize<string[]>(entity.FavoritesJson), groupExpanded = JsonSerializer.Deserialize<Dictionary<string, bool>>(entity.GroupExpandedJson), accent = entity.Accent, fontSize = entity.FontSize, appearance = entity.Appearance });
    }

    public sealed class SaveRequest {
        public string? Accent { get; set; }
        public string? FontSize { get; set; }
        public string? Appearance { get; set; }
        public string[]? GroupOrder { get; set; }
        public string? Density { get; set; }
        public string[]? Favorites { get; set; }
        public Dictionary<string, bool>? GroupExpanded { get; set; }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Consumes("application/json")]
    public async Task<IActionResult> Save([FromBody] SaveRequest? request, CancellationToken cancellationToken) {
        if (UserId is not { Length: > 0 } userId) return Unauthorized();
        if (request?.Accent is not ("indigo" or "teal" or "amber") ||
            request.FontSize is not ("normal" or "large") ||
            request.Appearance is not ("system" or "light" or "dark") ||
            request.GroupOrder is null || request.GroupOrder.Length > 30 ||
            request.Density is not ("normal" or "compact") ||
            request.GroupOrder.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 100) ||
            request.GroupOrder.Distinct(StringComparer.Ordinal).Count() != request.GroupOrder.Length ||
            request.Favorites is null || request.Favorites.Length > 30 ||
            request.Favorites.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 300 || !x.StartsWith("/CRM/", StringComparison.OrdinalIgnoreCase) && !x.StartsWith("/Support/", StringComparison.OrdinalIgnoreCase)) ||
            request.Favorites.Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Favorites.Length ||
            request.GroupExpanded is null || request.GroupExpanded.Count > 30 ||
            request.GroupExpanded.Keys.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 100))
            return BadRequest(new { error = "Invalid sidebar preferences" });
        var json = JsonSerializer.Serialize(request.GroupOrder);
        if (json.Length > 2000) return BadRequest(new { error = "Sidebar preferences too long" });
        var entity = await db.CrmSidebarPreferences.FindAsync([userId], cancellationToken);
        if (entity is null) {
            entity = new SidebarPreferenceEntity { UserId = userId };
            db.CrmSidebarPreferences.Add(entity);
        }
        entity.Accent = request.Accent;
        entity.FontSize = request.FontSize;
        entity.Appearance = request.Appearance;
        entity.GroupOrderJson = json;
        entity.Density = request.Density;
        var favoritesJson = JsonSerializer.Serialize(request.Favorites);
        var expandedJson = JsonSerializer.Serialize(request.GroupExpanded);
        if (favoritesJson.Length > 2000 || expandedJson.Length > 2000) return BadRequest(new { error = "Sidebar preferences too long" });
        entity.FavoritesJson = favoritesJson;
        entity.GroupExpandedJson = expandedJson;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { saved = true });
    }
}
