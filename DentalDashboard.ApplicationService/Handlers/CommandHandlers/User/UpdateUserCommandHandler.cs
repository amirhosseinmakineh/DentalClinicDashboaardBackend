using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.UpddateUser;
using DentalDashboard.ApplicationService.Contract.Responses.User;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.User
{
    public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand,UpdateUserResponse>
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

        public async Task<Result<UpdateUserResponse>> HandleAsync(
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
                    return Result<UpdateUserResponse>.Failure("کاربر یافت نشد");
                }

                user.FirstName = command.FirstName;
                user.LastName = command.LastName;
                user.PhoneNumber = command.PhoneNumber;
                user.IsCompleteProfile = command.IsCompleteProfile;
                user.AvatarImageName = command.AvatarImageName;
                user.Gender = command.Gender;
                user.IsActive = command.IsActive;

                userRepository.Update(user);

                await roleService.SetUserRole(user.Id, command.RoleName);

                await userRepository.SaveChange();
                await unitOfWork.CommitAsync();
                var response = new UpdateUserResponse()
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsActive = user.IsActive,
                    RoleName = command.RoleName
                };
                return Result<UpdateUserResponse>.Success(response,"ویرایش کاربر با موفقیت انجام شد");
            }
            catch
            {
                await unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}