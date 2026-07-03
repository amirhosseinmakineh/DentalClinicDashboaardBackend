namespace DentalDashboard.ApplicationService.Contract.Responses.AuthResponse;

public record ForgotPasswordResponse
{
    public Guid UserId { get; init; }
}
