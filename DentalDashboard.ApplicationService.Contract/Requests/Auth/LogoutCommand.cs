using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Contract.Requests.Auth;

public class LogoutCommand : ICommand
{
    public Guid UserId { get; set; }
}
