using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public class UpdateConsultantLimitNumberCommandHandler
    : ICommandHandler<UpdateConsultantLimitNumberCommand, ConsultantLimitUpdateResponse>
{
    private readonly IConsultantProfileRepository consultantProfileRepository;
    private readonly ILeadAssignmentLimitService leadAssignmentLimitService;

    public UpdateConsultantLimitNumberCommandHandler(
        IConsultantProfileRepository consultantProfileRepository,
        ILeadAssignmentLimitService leadAssignmentLimitService)
    {
        this.consultantProfileRepository = consultantProfileRepository;
        this.leadAssignmentLimitService = leadAssignmentLimitService;
    }

    public async Task<Result<ConsultantLimitUpdateResponse>> HandleAsync(
        UpdateConsultantLimitNumberCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.LimitNumber.HasValue &&
            (command.LimitNumber.Value < 0 || command.LimitNumber.Value > 100))
        {
            return Result<ConsultantLimitUpdateResponse>.Failure(
                "محدودیت دریافت شماره باید بین ۰ تا ۱۰۰ باشد");
        }

        var profile = await consultantProfileRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == command.ProfileId, cancellationToken);

        if (profile == null)
            return Result<ConsultantLimitUpdateResponse>.Failure("مشاوری یافت نشد");

        if (profile.IsDeleted)
            return Result<ConsultantLimitUpdateResponse>.Failure("پروفایل مشاور حذف شده است");

        profile.LimitNumber = command.LimitNumber;

        consultantProfileRepository.Update(profile);
        await consultantProfileRepository.SaveChange();

        var limitStatus = await leadAssignmentLimitService
            .GetDailyLimitStatusAsync(profile.Id);

        return Result<ConsultantLimitUpdateResponse>.Success(
            new ConsultantLimitUpdateResponse
            {
                LimitNumber = profile.LimitNumber,
                EffectiveDailyLimit = limitStatus.EffectiveDailyLimit,
                TodayPickupCount = limitStatus.TodayPickupCount
            },
            "محدودیت دریافت شماره با موفقیت ذخیره شد");
    }
}
