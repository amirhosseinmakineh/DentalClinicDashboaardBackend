using DentalDashboard.ApplicationService.Contract.Responses.RoleResponses;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Contract.Requests.Role
{
    public class CreateRoleCommand : ICommand
    {
        public string RoleName { get; set; } = default!;
    }
}
