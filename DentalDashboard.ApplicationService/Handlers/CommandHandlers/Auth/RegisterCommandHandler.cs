using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.ApplicationService.Contract.Responses.AuthResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Utilities.Hasher;
using FluentValidation;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth;

public class RegisterCommandHandler : ICommandHandler<RegisterCommand, RegisterResponse>
{
    private const string DefaultRoleName = "NormalUser";

    private readonly IUserRepository userRepository;
    private readonly IRoleService roleService;
    private readonly IUnitOfWork unitOfWork;
    private readonly IValidator<RegisterCommand> validator;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IRoleService roleService,
        IUnitOfWork unitOfWork,
        IValidator<RegisterCommand> validator)
    {
        this.userRepository = userRepository;
        this.roleService = roleService;
        this.unitOfWork = unitOfWork;
        this.validator = validator;
    }

    public async Task<Result<RegisterResponse>> HandleAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<RegisterResponse>.Failure(validationResult.Errors.First().ErrorMessage);
        }

        var exists = await userRepository.ExistsAsync(x => x.PhoneNumber == command.PhoneNumber);
        if (exists)
        {
            return Result<RegisterResponse>.Failure("کاربری با این شماره موبایل قبلاً ثبت شده است");
        }

        await unitOfWork.BeginTransactionAsync();

        try
        {
            var user = new Domain.Models.User
            {
                Id = Guid.NewGuid(),
                FirstName = command.FirstName.Trim(),
                LastName = command.LastName.Trim(),
                PhoneNumber = command.PhoneNumber.Trim(),
                PasswordHash = PasswordHasher.HashPassword(command.PasswordHash),
                BirthDate = command.BirthDate,
                Gender = command.Gender,
                AvatarImageName = command.AvatarImageName,
                IsActive = true,
                IsCompleteProfile = false,
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            };

            await userRepository.AddAsync(user);
            await roleService.AddRoleToUser(user.Id, DefaultRoleName);
            await unitOfWork.CommitAsync();

            return Result<RegisterResponse>.Success(
                new RegisterResponse
                {
                    UserId = user.Id,
                    Role = DefaultRoleName
                },
                "ثبت نام با موفقیت انجام شد");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync();
            return Result<RegisterResponse>.Failure($"خطا در ثبت نام: {ex.Message}");
        }
    }
}
