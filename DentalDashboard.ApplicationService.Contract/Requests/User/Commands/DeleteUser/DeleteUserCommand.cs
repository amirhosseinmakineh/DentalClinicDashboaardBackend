using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
namespace DentalDashboard.ApplicationService.Contract.Requests.User.Commands.DeleteUser
{
    public class DeleteUserCommand : ICommand<object>
    {
        public Guid UserId { get; set; }
    }

}
