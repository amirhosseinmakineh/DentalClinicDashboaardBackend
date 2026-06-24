namespace DentalDashboard.ApplicationService.Contract.Responses.AuthResponse;

public record DashboardAccessResponse
{
    public string Role { get; init; } = string.Empty;

    public string Dashboard { get; init; } = string.Empty;

    public string Route { get; init; } = string.Empty;
}
