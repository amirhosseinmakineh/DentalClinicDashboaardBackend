namespace DentalDashboard.ApplicationService.Contract.Responses.AuthResponse;

public record LoginResponse : AuthenticatedUserResponse
{
    public string Token { get; init; } = string.Empty;
}
