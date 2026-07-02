using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.CreateUser;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.DeleteUser;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.User
{
    public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand,object>
    {
        private readonly IUserRepository userRepository;
        private readonly IUserRoleRepository userRoleRepository;
        private readonly IUnitOfWork unitOfWork;

        public DeleteUserCommandHandler(
            IUserRepository userRepository,
            IUserRoleRepository userRoleRepository,
            IUnitOfWork unitOfWork)
        {
            this.userRepository = userRepository;
            this.userRoleRepository = userRoleRepository;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Result<object>> HandleAsync(DeleteUserCommand command,CancellationToken cancellationToken = default)
        {
            await unitOfWork.BeginTransactionAsync();

            var user = await userRepository.GetByIdAsync(command.UserId);

            if (user == null)
            {
                return Result<object>.Failure("کاربر یافت نشد");
            }

            var userRoles = await userRoleRepository.FindAsync(x => x.UserId == command.UserId);

            foreach (var userRole in userRoles)
            {
                userRoleRepository.Delete(userRole);
            }

            userRepository.Delete(user);

            await unitOfWork.CommitAsync();

            return Result<object>.Success(true,"حذف کاربر با موفقیت انجام شد");
        }
    }
}