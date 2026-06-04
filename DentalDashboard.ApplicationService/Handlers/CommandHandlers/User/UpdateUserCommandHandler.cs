using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.UpddateUser;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.User
{
    public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
    {
        private readonly IUserRepository userRepository;
        private readonly IUnitOfWork unitOfWork;
        private readonly IRoleService roleService;

        public UpdateUserCommandHandler(
            IUnitOfWork unitOfWork,
            IUserRepository userRepository,
            IRoleService roleService)
        {
            this.unitOfWork = unitOfWork;
            this.userRepository = userRepository;
            this.roleService = roleService;
        }

        public async Task<Result> HandleAsync(
            UpdateUserCommand command,
            CancellationToken cancellationToken = default)
        {
            await unitOfWork.BeginTransactionAsync();

            try
            {
                var user = await userRepository.GetByIdAsync(command.Id);

                if (user == null)
                {
                    await unitOfWork.RollbackAsync();
                    return Result.Failure("کاربر یافت نشد");
                }

                user.FirstName = command.FirstName;
                user.LastName = command.LastName;
                user.PhoneNumber = command.PhoneNumber;
                user.IsCompleteProfile = command.IsCompleteProfile;
                user.AvatarImageName = command.AvatarImageName;
                user.Gender = command.Gender;
                user.IsActive = command.IsActive;

                userRepository.Update(user);

                await roleService.AddRoleToUser(user.Id, command.RoleName);

                await userRepository.SaveChange();
                await unitOfWork.CommitAsync();

                return Result.Success("ویرایش کاربر با موفقیت انجام شد");
            }
            catch
            {
                await unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}