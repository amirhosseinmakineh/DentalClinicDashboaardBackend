using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public class UpdateConsultantLimitNumberCommandHandler
    : ICommandHandler<UpdateConsultantLimitNumberCommand>
{
    private readonly IConsultantProfileRepository consultantProfileRepository;

    public UpdateConsultantLimitNumberCommandHandler(
        IConsultantProfileRepository consultantProfileRepository)
    {
        this.consultantProfileRepository = consultantProfileRepository;
    }

    public async Task<Result> HandleAsync(
        UpdateConsultantLimitNumberCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.LimitNumber.HasValue &&
            (command.LimitNumber.Value < 0 || command.LimitNumber.Value > 100))
        {
            return Result.Failure("محدودیت دریافت شماره باید بین ۰ تا ۱۰۰ باشد");
        }

        var profile = await consultantProfileRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == command.ProfileId, cancellationToken);

        if (profile == null)
            return Result.Failure("مشاوری یافت نشد");

        if (profile.IsDeleted)
            return Result.Failure("پروفایل مشاور حذف شده است");

        profile.LimitNumber = command.LimitNumber;

        consultantProfileRepository.Update(profile);
        await consultantProfileRepository.SaveChange();

        return Result.Success("محدودیت دریافت شماره با موفقیت ذخیره شد");
    }
}
