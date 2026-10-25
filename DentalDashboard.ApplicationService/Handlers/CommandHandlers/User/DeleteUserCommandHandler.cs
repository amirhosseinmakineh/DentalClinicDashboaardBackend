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
            if (command.UserId == Guid.Empty)
                return Result<object>.Failure("شناسه کاربر معتبر نیست");

            await unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var user = await userRepository.GetByIdAsync(command.UserId);

                if (user == null || user.IsDeleted)
                {
                    await unitOfWork.RollbackAsync(cancellationToken);
                    return Result<object>.Failure("کاربر یافت نشد");
                }

                var deletedAt = DateTime.UtcNow;
                var userRoles = await userRoleRepository.FindAsync(x =>
                    x.UserId == command.UserId && !x.IsDeleted);

                foreach (var userRole in userRoles)
                {
                    userRole.IsDeleted = true;
                    userRole.DeletedAt = deletedAt;
                    userRole.UpdatedAt = deletedAt;
                    userRoleRepository.Update(userRole);
                }

                user.IsDeleted = true;
                user.IsActive = false;
                user.DeletedAt = deletedAt;
                user.UpdatedAt = deletedAt;
                userRepository.Update(user);

                await unitOfWork.CommitAsync(cancellationToken);

                return Result<object>.Success(true,"حذف کاربر با موفقیت انجام شد");
            }
            catch
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
