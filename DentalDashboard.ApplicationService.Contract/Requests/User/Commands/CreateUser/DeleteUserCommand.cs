using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.User.Commands.CreateUser
{
    public class DeleteUserCommand : ICommand<object>
    {
        public Guid Id { get; set; }
    }
}