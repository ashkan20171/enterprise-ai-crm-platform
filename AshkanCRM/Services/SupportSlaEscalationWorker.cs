using AshkanCRM.Data;
using AshkanCRM.Hubs;
using AshkanCRM.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AshkanCRM.Services;

/// <summary>Idempotent first-response SLA breach escalation. Requires the filtered unique SQL index installed by bootstrap.</summary>
public sealed class SupportSlaEscalationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SupportSlaEscalationWorker> logger,
    IConfiguration configuration) : BackgroundService
{
    private const string AutomationActor = "SLA Automation";
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Allow application database bootstrap to finish before the first check.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try { await CheckAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "SLA escalation cycle failed"); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<SupportHub>>();
        var now = DateTime.UtcNow;
        var candidates = await db.SupportTickets.AsNoTracking()
            .Where(t => t.Status != "Closed" && t.Status != "Resolved")
            .OrderBy(t => t.CreatedAtUtc).Take(500).ToListAsync(ct);
        var ids = candidates.Select(t => t.Id).ToArray();
        var replies = await db.SupportTicketReplies.AsNoTracking()
            .Where(r => ids.Contains(r.TicketId))
            .Select(r => new { r.TicketId, r.Author }).ToListAsync(ct);
        var escalated = await db.SupportEscalations.AsNoTracking()
            .Where(e => ids.Contains(e.TicketId) && e.Actor == AutomationActor)
            .Select(e => e.TicketId).ToListAsync(ct);
        var done = escalated.ToHashSet();
        foreach (var ticket in candidates)
        {
            if (done.Contains(ticket.Id) || replies.Any(r => r.TicketId == ticket.Id && r.Author != ticket.Requester)) continue;
            var fallback = ticket.Priority switch { "Critical" => 4, "High" => 8, "Low" => 48, _ => 24 };
            var hours = configuration.GetValue<int?>("SupportSla:PriorityHours:" + ticket.Priority) ?? fallback;
            if (hours is < 1 or > 1000) hours = fallback;
            var deadline = SupportBusinessCalendar.AddBusinessHours(ticket.CreatedAtUtc, hours, configuration);
            if (deadline >= now) continue;
            db.SupportEscalations.Add(new SupportEscalation
            {
                TicketId = ticket.Id, Actor = AutomationActor,
                Reason = "Automatic first-response SLA breach", CreatedAtUtc = now
            });
            try
            {
                await db.SaveChangesAsync(ct);
                done.Add(ticket.Id);
                await hub.Clients.Group("support-managers").SendAsync("SupportChanged",
                    new { id = ticket.Id, kind = "sla-breached" }, ct);
                logger.LogWarning("First-response SLA breached for support ticket {TicketId}", ticket.Id);
            }
            catch (DbUpdateException ex)
            {
                // Another instance may have inserted the same escalation. Unique index ensures exactly one.
                logger.LogInformation(ex, "SLA escalation already handled or database insert failed for {TicketId}", ticket.Id);
                db.ChangeTracker.Clear();
            }
        }
    }
}
