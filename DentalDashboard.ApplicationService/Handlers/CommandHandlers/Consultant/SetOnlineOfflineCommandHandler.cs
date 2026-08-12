using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class SetOnlineOfflineCommandHandler : ICommandHandler<SetOnlineOfflineCommand>
    {
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly ILeadAssignmentService leadAssignmentService;
        private readonly ILeadDomainService leadDomainService;
        private readonly IUserPresenceService presenceService;

        public SetOnlineOfflineCommandHandler(
            IConsultantProfileRepository consultantProfileRepository,
            ILeadAssignmentService leadAssignmentService,
            ILeadDomainService leadDomainService,
            IUserPresenceService presenceService)
        {
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadAssignmentService = leadAssignmentService;
            this.leadDomainService = leadDomainService;
            this.presenceService = presenceService;
        }

        public async Task<Result> HandleAsync(
            SetOnlineOfflineCommand command,
            CancellationToken cancellationToken = default)
        {
            var profile = await consultantProfileRepository
                .GetAll()
                .Include(x => x.CallAssignments)
                .FirstOrDefaultAsync(
                    x => x.Id == command.ProfileId,
                    cancellationToken);

            if (profile == null)
                return Result.Failure("مشاوری یافت نشد");

            if (profile.IsDeleted)
                return Result.Failure("پروفایل مشاور حذف شده است");

            if (command.IsOnline)
            {
                return await SetOnlineAsync(
                    profile,
                    cancellationToken);
            }

            return await SetOfflineAsync(
                profile,
                cancellationToken);
        }

        private async Task<Result> SetOnlineAsync(
            ConsultantProfile profile,
            CancellationToken cancellationToken)
        {
            // بیزینس قبلی:
            // بعد از ساعت پایان کار امکان آنلاین شدن وجود ندارد.
            if (leadDomainService.IsAfterWorkEnd(DateTime.Now))
            {
                return Result.Failure(
                    "امکان آنلاین شدن بعد از ساعت ۹ شب وجود ندارد");
            }

            // تعداد لیدهای در حال پیگیری
            var pendingLeadsCount = profile.CallAssignments.Count(x =>
                !x.IsDeleted &&
                x.LeadAssignmentState == LeadAssignmentState.Pending);

            // بیزینس جدید:
            // اگر 10 لید Pending یا بیشتر داشته باشد، آنلاین نشود.
            if (pendingLeadsCount >= 10)
            {
                return Result.Failure(
                    $"شما {pendingLeadsCount} شماره در حال پیگیری دارید. " +
                    "لطفاً ابتدا پیگیری شماره‌های فعلی را انجام دهید؛ " +
                    "تا آن زمان امکان آنلاین شدن و دریافت شماره جدید برای شما وجود ندارد.");
            }

            // تعداد لیدهایی که گزارش برایشان ثبت نشده
            var unSubmittedReportCount = profile.CallAssignments.Count(x =>
                !x.IsDeleted &&
                x.ReportSubmittedAt == null);

            // بیزینس جدید:
            // اگر حتی یک گزارش ثبت نشده وجود داشته باشد، آنلاین نشود.
            if (unSubmittedReportCount >= 1)
            {
                var message = unSubmittedReportCount == 1
                    ? "شما یک شماره دارید که هنوز گزارش آن را ثبت نکرده‌اید. " +
                      "لطفاً ابتدا با شماره تماس گرفته و گزارش را ثبت کنید؛ " +
                      "تا آن زمان امکان آنلاین شدن و دریافت شماره جدید برای شما وجود ندارد."
                    : $"شما {unSubmittedReportCount} شماره دارید که هنوز گزارش آن‌ها را ثبت نکرده‌اید. " +
                      "لطفاً ابتدا گزارش شماره‌ها را ثبت کنید؛ " +
                      "تا آن زمان امکان آنلاین شدن و دریافت شماره جدید برای شما وجود ندارد.";

                return Result.Failure(message);
            }

            // بیزینس قبلی
            profile.IsOnline = true;
            profile.LastOnlineAt = DateTime.Now;

            consultantProfileRepository.Update(profile);
            await consultantProfileRepository.SaveChange();

            // بیزینس قبلی Presence
            await presenceService.LogAsync(
                profile.UserId,
                UserPresenceEventType.Online,
                profile.LastOnlineAt,
                cancellationToken: cancellationToken);

            // بیزینس قبلی تخصیص لید لحظه‌ای
            await leadAssignmentService.AssignRealTimeLeadsAsync();

            return Result.Success("شما آنلاین شدید");
        }

        private async Task<Result> SetOfflineAsync(
            ConsultantProfile profile,
            CancellationToken cancellationToken)
        {
            // بیزینس قبلی
            profile.IsOnline = false;
            profile.LastOfflineAt = DateTime.Now;

            consultantProfileRepository.Update(profile);
            await consultantProfileRepository.SaveChange();

            // بیزینس قبلی Presence
            await presenceService.LogAsync(
                profile.UserId,
                UserPresenceEventType.Offline,
                profile.LastOfflineAt,
                cancellationToken: cancellationToken);

            return Result.Success("شما آفلاین شدید");
        }
    }
}