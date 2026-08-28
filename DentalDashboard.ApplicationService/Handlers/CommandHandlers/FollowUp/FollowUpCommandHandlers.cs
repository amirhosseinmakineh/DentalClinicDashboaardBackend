using DentalDashboard.ApplicationService.Contract.Requests.FollowUp.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.FollowUp;

public sealed class CreateSecretaryFollowUpCommandHandler(
    IReservationRepository reservations)
    : ICommandHandler<CreateSecretaryFollowUpCommand>
{
    public async Task<Result> HandleAsync(
        CreateSecretaryFollowUpCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.SecretaryUserId == Guid.Empty)
            return Result.Failure("کاربر جاری معتبر نیست");

        if (command.PatientId <= 0)
            return Result.Failure("بیمار معتبر نیست");

        var contactResult = command.ContactResult?.Trim();

        if (contactResult?.Length > 1000)
            return Result.Failure("نتیجه تماس نباید بیشتر از ۱۰۰۰ کاراکتر باشد");

        var reservation = await reservations
            .GetAll()
            .Where(x =>
                !x.IsDeleted &&
                !x.IsCanceled &&
                x.LeadAssignmentId == command.PatientId)
            .OrderByDescending(x => x.ReservationAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (reservation == null)
            return Result.Failure("رزرو مرتبط یافت نشد");

        reservation.SecretaryFollowUpContacted = command.Contacted;
        reservation.SecretaryAnnouncement =
            string.IsNullOrWhiteSpace(contactResult)
                ? null
                : contactResult;

        reservation.SecretaryAnnouncementUserId = command.SecretaryUserId;
        reservation.SecretaryAnnouncementUpdatedAt = DateTime.UtcNow;
        reservation.UpdatedAt = DateTime.UtcNow;

        reservations.Update(reservation);
        await reservations.SaveChange();

        return Result.Success("پیگیری با موفقیت ثبت شد");
    }
}

public sealed class UpdateSecretaryFollowUpCommandHandler(
    IReservationRepository reservations)
    : ICommandHandler<UpdateSecretaryFollowUpCommand>
{
    public async Task<Result> HandleAsync(
        UpdateSecretaryFollowUpCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Id <= 0 || command.SecretaryUserId == Guid.Empty)
            return Result.Failure("درخواست معتبر نیست");

        var contactResult = command.ContactResult?.Trim();

        if (contactResult?.Length > 1000)
            return Result.Failure("نتیجه تماس نباید بیشتر از ۱۰۰۰ کاراکتر باشد");

        var reservation = await reservations
            .GetAll()
            .FirstOrDefaultAsync(
                x =>
                    !x.IsDeleted &&
                    x.Id == command.Id &&
                    x.SecretaryAnnouncementUserId == command.SecretaryUserId &&
                    x.SecretaryAnnouncementUpdatedAt != null,
                cancellationToken);

        if (reservation == null)
            return Result.Failure("پیگیری یافت نشد");

        reservation.SecretaryFollowUpContacted = command.Contacted;
        reservation.SecretaryAnnouncement =
            string.IsNullOrWhiteSpace(contactResult)
                ? null
                : contactResult;

        reservation.SecretaryAnnouncementUpdatedAt = DateTime.UtcNow;
        reservation.UpdatedAt = DateTime.UtcNow;

        reservations.Update(reservation);
        await reservations.SaveChange();

        return Result.Success("پیگیری با موفقیت ویرایش شد");
    }
}

public sealed class DeleteSecretaryFollowUpCommandHandler(
    IReservationRepository reservations)
    : ICommandHandler<DeleteSecretaryFollowUpCommand>
{
    public async Task<Result> HandleAsync(
        DeleteSecretaryFollowUpCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Id <= 0 || command.SecretaryUserId == Guid.Empty)
            return Result.Failure("درخواست معتبر نیست");

        var reservation = await reservations
            .GetAll()
            .FirstOrDefaultAsync(
                x =>
                    !x.IsDeleted &&
                    x.Id == command.Id &&
                    x.SecretaryAnnouncementUserId == command.SecretaryUserId &&
                    x.SecretaryAnnouncementUpdatedAt != null,
                cancellationToken);

        if (reservation == null)
            return Result.Failure("پیگیری یافت نشد");

        reservation.SecretaryFollowUpContacted = null;
        reservation.SecretaryAnnouncement = null;
        reservation.SecretaryAnnouncementStatus = null;
        reservation.SecretaryAnnouncementUserId = null;
        reservation.SecretaryAnnouncementUpdatedAt = null;
        reservation.UpdatedAt = DateTime.UtcNow;

        reservations.Update(reservation);
        await reservations.SaveChange();

        return Result.Success("پیگیری با موفقیت حذف شد");
    }
}
