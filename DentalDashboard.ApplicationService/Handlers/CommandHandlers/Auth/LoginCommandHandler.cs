using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.ApplicationService.Contract.Responses.AuthResponse;
using DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth.Helpers;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Security.Generator;
using DentalDashboard.Utilities.Hasher;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth;

public class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository userRepository;
    private readonly ITokenGenerator tokenGenerator;
    private readonly IValidator<LoginCommand> validator;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator,
        IValidator<LoginCommand> validator)
    {
        this.userRepository = userRepository;
        this.tokenGenerator = tokenGenerator;
        this.validator = validator;
    }

    public async Task<Result<LoginResponse>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<LoginResponse>.Failure(validationResult.Errors.First().ErrorMessage);
        }

        var user = await userRepository.GetAll()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Include(x => x.ConsultantProfile)
            .FirstOrDefaultAsync(x => x.PhoneNumber == command.PhoneNumber, cancellationToken);

        if (user is null || user.IsDeleted)
        {
            return Result<LoginResponse>.Failure("کاربری با این مشخصات یافت نشد");
        }

        if (!user.IsActive)
        {
            return Result<LoginResponse>.Failure("حساب کاربری غیرفعال است");
        }

        var isValidPassword = PasswordHasher.VerifyPassword(
            command.PasswordHash,
            user.PasswordHash);

        if (!isValidPassword)
        {
            return Result<LoginResponse>.Failure("رمز عبور اشتباه است");
        }

        var userRoles = user.UserRoles
            .Where(x => !x.IsDeleted && x.Role != null && !x.Role.IsDeleted)
            .Select(x => x.Role)
            .DistinctBy(x => x.Id)
            .ToList();

        if (userRoles.Count == 0)
        {
            return Result<LoginResponse>.Failure("برای این کاربر هیچ نقشی ثبت نشده است");
        }

        var token = tokenGenerator.GenerateToken(user, userRoles);
        var response = AuthResponseFactory.CreateLoginResponse(user, userRoles, token);

        return Result<LoginResponse>.Success(response, "ورود با موفقیت انجام شد");
    }
}
