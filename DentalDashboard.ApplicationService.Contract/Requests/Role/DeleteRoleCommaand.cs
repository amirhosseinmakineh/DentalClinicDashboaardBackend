using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Role
{
    public class DeleteRoleCommaand : ICommand 
    {
        public long RoleId { get; set; }
    }
}
