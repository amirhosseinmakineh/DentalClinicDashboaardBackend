using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Responses.Presence;

public class UserPresenceOverviewItemResponse
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string PhoneNumber { get; set; } = default!;

    public string RoleName { get; set; } = default!;

    public bool IsCurrentlyOnline { get; set; }

    public string? LastSeenAtPersian { get; set; }

    public bool? ConsultantIsOnline { get; set; }

    public bool? ConsultantIsAvailable { get; set; }

    public string SelectedDatePersian { get; set; } = default!;

    public string? FirstLoginAtPersian { get; set; }

    public string? LastLogoutAtPersian { get; set; }

    public string? FirstOnlineAtPersian { get; set; }

    public string? LastOfflineAtPersian { get; set; }

    public string? FirstCheckInAtPersian { get; set; }

    public string? LastCheckOutAtPersian { get; set; }

    public int EventCountForDay { get; set; }
}
