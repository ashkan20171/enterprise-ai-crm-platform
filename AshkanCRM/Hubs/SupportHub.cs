using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
namespace AshkanCRM.Hubs;
[Authorize]
public sealed class SupportHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var name = Context.User?.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name))
            await Groups.AddToGroupAsync(Context.ConnectionId, "support-user:" + name);
        if (Context.User?.IsInRole("Admin") == true || Context.User?.IsInRole("SalesManager") == true)
            await Groups.AddToGroupAsync(Context.ConnectionId, "support-managers");
        await base.OnConnectedAsync();
    }
}
