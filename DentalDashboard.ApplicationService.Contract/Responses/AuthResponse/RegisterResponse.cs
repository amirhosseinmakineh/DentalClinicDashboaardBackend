namespace DentalDashboard.ApplicationService.Contract.Responses.AuthResponse;

public record RegisterResponse
{
    public Guid UserId { get; init; }

    public string Role { get; init; } = string.Empty;
}
