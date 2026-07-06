using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Responses.Presence;

public class UserPresenceEventItemResponse
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string PhoneNumber { get; set; } = default!;

    public string RoleName { get; set; } = default!;

    public UserPresenceEventType EventType { get; set; }

    public string EventTypeLabel { get; set; } = default!;

    public string OccurredAtPersian { get; set; } = default!;

    public string? Description { get; set; }
}
