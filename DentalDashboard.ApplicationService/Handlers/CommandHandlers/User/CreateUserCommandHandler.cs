using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.CreateUser;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Framwork.IRepositories;
using DentalDashboard.Utilities.Hasher;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.User
{
    public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, object>
    {
        private readonly IUserRepository userRepository;
        private readonly IRoleService roleService;
        private readonly IUnitOfWork unitOfWork;

        public CreateUserCommandHandler(
            IUserRepository userRepository,
            IRoleService roleService,
            IUnitOfWork unitOfWork)
        {
            this.userRepository = userRepository;
            this.roleService = roleService;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Result<object>> HandleAsync(CreateUserCommand command,CancellationToken cancellationToken = default)
        {
            await unitOfWork.BeginTransactionAsync();

            try
            {
                var exists = await userRepository.ExistsAsync(
                    x => x.PhoneNumber == command.PhoneNumber);

                if (exists)
                {
                    return Result<object>.Failure(
                        "کاربری با این شماره موبایل قبلاً ثبت شده است");
                }

                var user = new Domain.Models.User
                {
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    PhoneNumber = command.PhoneNumber,
                    PasswordHash = PasswordHasher.HashPassword(command.PasswordHash),
                    BirthDate = command.BirthDate,
                    Gender = command.Gender,
                    AvatarImageName = command.AvatarImageName,
                    IsActive = false,
                    IsCompleteProfile = false
                };

                await userRepository.AddAsync(user);

                    await roleService.AddRoleToUser(user.Id, command.RoleName);

                await unitOfWork.CommitAsync();

                return Result<object>.Success(user.Id);
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                return Result<object>.Failure($"خطا در ایجاد کاربر: {ex.Message}");
            }
        }
    }
}