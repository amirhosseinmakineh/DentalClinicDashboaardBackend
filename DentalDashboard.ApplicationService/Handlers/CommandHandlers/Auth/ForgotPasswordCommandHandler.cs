using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.ApplicationService.Contract.Responses.AuthResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Utilities.Hasher;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth;

public class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private const string PasswordChangedNotificationTitle = "تغییر رمز عبور";
    private const string PasswordChangedNotificationBody = "کلمه عبور شما با موفقیت تغییر کرد";

    private readonly IUserRepository userRepository;
    private readonly IPushNotificationService pushNotificationService;
    private readonly IValidator<ForgotPasswordCommand> validator;
    private readonly ILogger<ForgotPasswordCommandHandler> logger;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPushNotificationService pushNotificationService,
        IValidator<ForgotPasswordCommand> validator,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        this.userRepository = userRepository;
        this.pushNotificationService = pushNotificationService;
        this.validator = validator;
        this.logger = logger;
    }

    public async Task<Result<ForgotPasswordResponse>> HandleAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<ForgotPasswordResponse>.Failure(validationResult.Errors.First().ErrorMessage);
        }

        var user = await userRepository.GetAll()
            .FirstOrDefaultAsync(x => x.PhoneNumber == command.PhoneNumber, cancellationToken);

        if (user is null || user.IsDeleted)
        {
            return Result<ForgotPasswordResponse>.Failure("کاربری با این شماره موبایل یافت نشد");
        }

        if (!user.IsActive)
        {
            return Result<ForgotPasswordResponse>.Failure("حساب کاربری غیرفعال است");
        }

        user.PasswordHash = PasswordHasher.HashPassword(command.NewPasswordHash);
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);
        await userRepository.SaveChange();

        var pushSent = await pushNotificationService.SendAsync(
            user.Id,
            PasswordChangedNotificationTitle,
            PasswordChangedNotificationBody,
            new Dictionary<string, string>
            {
                ["type"] = "password_changed",
                ["phoneNumber"] = user.PhoneNumber
            },
            cancellationToken);

        if (!pushSent)
        {
            logger.LogWarning(
                "Password-changed push was not delivered for user {UserId}. Ensure the user logged in on this device, granted notification permission, and Web Push VAPID keys are configured.",
                user.Id);
        }

        return Result<ForgotPasswordResponse>.Success(
            new ForgotPasswordResponse { UserId = user.Id },
            "رمز عبور با موفقیت تغییر کرد");
    }
}
