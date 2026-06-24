namespace DentalDashboard.ApplicationService.Contract.Responses.AuthResponse;

public record AuthenticatedUserResponse
{
    public Guid UserId { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();

    public long? ConsultantProfileId { get; init; }

    public string DefaultDashboard { get; init; } = string.Empty;

    public string DefaultDashboardRoute { get; init; } = string.Empty;

    public IReadOnlyCollection<DashboardAccessResponse> DashboardAccess { get; init; } = Array.Empty<DashboardAccessResponse>();
}
