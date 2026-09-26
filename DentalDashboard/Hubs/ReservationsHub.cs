using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DentalDashboard.Hubs;

/// <summary>
/// Publishes reservation changes so every open dashboard can refresh its local state.
/// </summary>
[Authorize]
public sealed class ReservationsHub : Hub;
