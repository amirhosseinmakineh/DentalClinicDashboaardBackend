using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.CreateUser;
using DentalDashboard.ApplicationService.Contract.Responses.User;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Framwork.IRepositories;
using DentalDashboard.Utilities.Hasher;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.User
{
    public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, CreateUserResponse>
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

        public async Task<Result<CreateUserResponse>> HandleAsync(CreateUserCommand command,CancellationToken cancellationToken = default)
        {
            await unitOfWork.BeginTransactionAsync();

            try
            {
                var exists = await userRepository.ExistsAsync(
                    x => x.PhoneNumber == command.PhoneNumber);

                if (exists)
                {
                    return Result<CreateUserResponse>.Failure(
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
                var response = new CreateUserResponse()
                {
                    Id = user.Id,
                    IsActive = user.IsActive,
                    RoleName = command.RoleName,
                };

                return Result<CreateUserResponse>.Success(response,"ثبت کاربر جدید با موفقیت انجام شد");
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                return Result<CreateUserResponse>.Failure($"خطا در ایجاد کاربر: {ex.Message}");
            }
        }
    }
}