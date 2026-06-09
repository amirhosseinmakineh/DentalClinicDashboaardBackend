using DentalDashboard.ApplicationService.Contract.Responses.RoleResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Role
{
    public class UpdateRoleCommaand : ICommand<UpdateRoleResponse>
    {
        public long RoleId { get; set; }
        public string RoleName { get; set; } = default!;
    }
}
