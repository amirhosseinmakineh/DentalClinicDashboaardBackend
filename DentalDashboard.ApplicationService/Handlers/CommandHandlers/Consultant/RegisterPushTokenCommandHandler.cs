using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class RegisterPushTokenCommandHandler : ICommandHandler<RegisterPushTokenCommand>
    {
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly IPushSubscriptionRepository pushSubscriptionRepository;

        public RegisterPushTokenCommandHandler(
            IConsultantProfileRepository consultantProfileRepository,
            IPushSubscriptionRepository pushSubscriptionRepository)
        {
            this.consultantProfileRepository = consultantProfileRepository;
            this.pushSubscriptionRepository = pushSubscriptionRepository;
        }

        public async Task<Result> HandleAsync(
            RegisterPushTokenCommand command,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.DeviceToken))
                return Result.Failure("توکن نوتیفیکیشن معتبر نیست");

            if (!PushSubscriptionJsonParser.TryParse(
                    command.DeviceToken.Trim(),
                    out var endpoint,
                    out var p256dh,
                    out var auth))
            {
                return Result.Failure("فرمت subscription معتبر نیست");
            }

            try
            {
                var profile = await consultantProfileRepository.GetAll()
                    .Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.Id == command.ProfileId, cancellationToken);

                if (profile == null || profile.IsDeleted)
                    return Result.Failure("مشاوری یافت نشد");

                if (profile.User == null)
                    return Result.Failure("کاربر مرتبط با پروفایل مشاور یافت نشد");

                await pushSubscriptionRepository.UpsertAsync(
                    profile.UserId,
                    endpoint,
                    p256dh,
                    auth,
                    cancellationToken);

                profile.User.PushNotificationToken = PushSubscriptionStorage.UpsertSubscription(
                    profile.User.PushNotificationToken,
                    command.DeviceToken.Trim());
                profile.User.UpdatedAt = DateTime.UtcNow;
                await consultantProfileRepository.SaveChange();

                return Result.Success("توکن نوتیفیکیشن ثبت شد");
            }
            catch (DbUpdateException ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                if (inner.Contains("PushSubscriptions", StringComparison.OrdinalIgnoreCase) ||
                    inner.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
                {
                    return Result.Failure(
                        "جدول PushSubscriptions روی سرور وجود ندارد یا migration اجرا نشده است. بک‌اند را redeploy کنید.");
                }

                if (inner.Contains("String or binary data would be truncated", StringComparison.OrdinalIgnoreCase))
                {
                    return Result.Failure(
                        "طول subscription از حد مجاز دیتابیس بیشتر است. بک‌اند را به آخرین نسخه به‌روز کنید.");
                }

                return Result.Failure($"ثبت subscription در دیتابیس انجام نشد: {inner}");
            }
            catch (Exception ex)
            {
                return Result.Failure($"ثبت subscription انجام نشد: {ex.Message}");
            }
        }
    }
}
