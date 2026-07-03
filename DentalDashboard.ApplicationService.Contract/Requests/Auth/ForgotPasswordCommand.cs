using DentalDashboard.ApplicationService.Contract.Responses.AuthResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Auth
{
    public class ForgotPasswordCommand : ICommand<ForgotPasswordResponse>
    {
        public string PhoneNumber { get; set; } = default!;

        public string NewPasswordHash { get; set; } = default!;
    }
}
