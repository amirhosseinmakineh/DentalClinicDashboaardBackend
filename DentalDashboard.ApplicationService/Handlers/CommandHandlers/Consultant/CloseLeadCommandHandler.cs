using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public class CloseLeadCommandHandler : ICommandHandler<CloseLeadCommand, object>
{
    private readonly ILeadAssignmentRepository leadAssignmentRepository;
    private readonly IReservationRepository reservationRepository;

    public CloseLeadCommandHandler(
        ILeadAssignmentRepository leadAssignmentRepository,
        IReservationRepository reservationRepository)
    {
        this.leadAssignmentRepository = leadAssignmentRepository;
        this.reservationRepository = reservationRepository;
    }

    public async Task<Result<object>> HandleAsync(
        CloseLeadCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.LeadAssignmentId <= 0 || command.ConsultantProfileId <= 0)
            return Result<object>.Failure("شناسه لید معتبر نیست");

        if (!Enum.IsDefined(command.Reason))
            return Result<object>.Failure("دلیل بستن لید معتبر نیست");

        var description = command.Description?.Trim();
        if (command.Reason == LeadClosureReason.Other && string.IsNullOrWhiteSpace(description))
            return Result<object>.Failure("برای گزینه سایر، توضیح الزامی است");

        if (description?.Length > 500)
            return Result<object>.Failure("توضیحات بستن لید نباید بیشتر از ۵۰۰ کاراکتر باشد");

        var lead = await leadAssignmentRepository.GetByIdAndConsultantAsync(
            command.LeadAssignmentId,
            command.ConsultantProfileId);
        if (lead == null)
            return Result<object>.Failure("لید متعلق به این مشاور نیست یا یافت نشد");

        if (lead.LeadAssignmentState == LeadAssignmentState.ClosedByConsultant)
            return Result<object>.Failure("این لید قبلاً بسته شده است");

        if (!lead.ReportSubmittedAt.HasValue)
            return Result<object>.Failure("پیش از بستن لید، حداقل یک گزارش تماس ثبت کنید");

        var hasActiveReservation = await reservationRepository.GetAll()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && !x.IsCanceled &&
                           x.LeadAssignmentId == lead.Id, cancellationToken);
        if (hasActiveReservation)
            return Result<object>.Failure("لیدی که رزرو فعال دارد قابل بستن نیست");

        lead.LeadAssignmentState = LeadAssignmentState.ClosedByConsultant;
        lead.ClosedByConsultantAt = DateTime.UtcNow;
        lead.ClosureReason = command.Reason;
        lead.ClosureDescription = description;
        lead.NotificationSent = true;
        leadAssignmentRepository.Update(lead);
        await leadAssignmentRepository.SaveChange();

        return Result<object>.Success(new
        {
            leadAssignmentId = lead.Id,
            consultantProfileId = lead.ConsultantProfileId,
            state = lead.LeadAssignmentState,
            closedAt = lead.ClosedByConsultantAt,
            reason = lead.ClosureReason
        }, "پیگیری این لید بسته شد");
    }
}
