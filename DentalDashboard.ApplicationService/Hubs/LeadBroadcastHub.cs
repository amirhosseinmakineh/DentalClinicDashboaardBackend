using Microsoft.AspNetCore.SignalR;

namespace DentalDashboard.ApplicationService.Hubs;

public sealed class LeadBroadcastHub : Hub
{
    public const string OnlineConsultantsGroup = "online-consultants";

    public Task JoinOnlineConsultants() =>
        Groups.AddToGroupAsync(Context.ConnectionId, OnlineConsultantsGroup);

    public Task LeaveOnlineConsultants() =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, OnlineConsultantsGroup);
}
