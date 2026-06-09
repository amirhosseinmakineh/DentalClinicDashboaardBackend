using DentalDashboard.ApplicationService.Contract.Responses.RoleResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Contract.Requests.Role
{
    public class CreateRoleCommand : ICommand<CreateRoleResponse>
    {
        public string RoleName { get; set; } = default!;
    }
}
