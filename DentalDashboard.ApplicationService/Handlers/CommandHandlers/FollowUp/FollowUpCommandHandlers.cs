using DentalDashboard.ApplicationService.Contract.Requests.FollowUp.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.FollowUp;

internal static class FollowUpValidation
{
    public static string? Normalize(string? value) => value?.Trim();
    public static bool IsValid(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 2000;
}

public sealed class CreateSecretaryFollowUpCommandHandler(IReservationRepository reservations)
    : ICommandHandler<CreateSecretaryFollowUpCommand>
{
    public async Task<Result> HandleAsync(CreateSecretaryFollowUpCommand command, CancellationToken ct = default)
    {
        var contactResult = FollowUpValidation.Normalize(command.ContactResult);
        if (command.SecretaryUserId == Guid.Empty || command.PatientId <= 0)
            return Result.Failure("درخواست معتبر نیست");
        if (!FollowUpValidation.IsValid(contactResult))
            return Result.Failure("نتیجه پیگیری اجباری و حداکثر ۲۰۰۰ کاراکتر است");

        // The reservation and consultant are resolved server-side from the patient's current assignment.
        var reservation = await reservations.GetAll()
            .Where(x => !x.IsDeleted && !x.IsCanceled && x.LeadAssignmentId == command.PatientId &&
                        !x.LeadAssignment.IsDeleted && x.LeadAssignment.ConsultantProfileId == x.ConsultantProfileId)
            .OrderByDescending(x => x.ReservationAt)
            .FirstOrDefaultAsync(ct);
        if (reservation is null) return Result.Failure("رزرو و تخصیص فعال مرتبط یافت نشد");
        if (reservation.SecretaryFollowUpCreatedAt.HasValue && !reservation.SecretaryFollowUpDeletedAt.HasValue)
            return Result.Failure("برای این رزرو قبلاً پیگیری ثبت شده است");

        var now = DateTime.UtcNow;
        reservation.SecretaryFollowUpContacted = command.Contacted;
        reservation.SecretaryAnnouncement = contactResult;
        reservation.SecretaryAnnouncementUserId = command.SecretaryUserId;
        reservation.SecretaryFollowUpCreatedAt = now;
        reservation.SecretaryFollowUpDeletedAt = null;
        reservation.SecretaryAnnouncementUpdatedAt = now;
        reservation.UpdatedAt = now;
        reservations.Update(reservation);
        await reservations.SaveChange();
        return Result.Success("پیگیری با موفقیت ثبت شد");
    }
}

public sealed class UpdateSecretaryFollowUpCommandHandler(IReservationRepository reservations)
    : ICommandHandler<UpdateSecretaryFollowUpCommand>
{
    public async Task<Result> HandleAsync(UpdateSecretaryFollowUpCommand command, CancellationToken ct = default)
    {
        var contactResult = FollowUpValidation.Normalize(command.ContactResult);
        if (command.Id <= 0 || command.SecretaryUserId == Guid.Empty || !FollowUpValidation.IsValid(contactResult))
            return Result.Failure("نتیجه پیگیری اجباری و حداکثر ۲۰۰۰ کاراکتر است");
        var reservation = await reservations.GetAll().FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == command.Id &&
            x.SecretaryAnnouncementUserId == command.SecretaryUserId && x.SecretaryFollowUpCreatedAt != null &&
            x.SecretaryFollowUpDeletedAt == null, ct);
        if (reservation is null) return Result.Failure("پیگیری یافت نشد");
        reservation.SecretaryFollowUpContacted = command.Contacted;
        reservation.SecretaryAnnouncement = contactResult;
        reservation.SecretaryAnnouncementUpdatedAt = DateTime.UtcNow;
        reservation.UpdatedAt = DateTime.UtcNow;
        reservations.Update(reservation);
        await reservations.SaveChange();
        return Result.Success("پیگیری با موفقیت ویرایش شد");
    }
}

public sealed class DeleteSecretaryFollowUpCommandHandler(IReservationRepository reservations)
    : ICommandHandler<DeleteSecretaryFollowUpCommand>
{
    public async Task<Result> HandleAsync(DeleteSecretaryFollowUpCommand command, CancellationToken ct = default)
    {
        if (command.Id <= 0 || command.SecretaryUserId == Guid.Empty) return Result.Failure("درخواست معتبر نیست");
        var reservation = await reservations.GetAll().FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == command.Id &&
            x.SecretaryAnnouncementUserId == command.SecretaryUserId && x.SecretaryFollowUpCreatedAt != null &&
            x.SecretaryFollowUpDeletedAt == null, ct);
        if (reservation is null) return Result.Failure("پیگیری یافت نشد");
        reservation.SecretaryFollowUpDeletedAt = DateTime.UtcNow;
        reservation.UpdatedAt = DateTime.UtcNow;
        reservations.Update(reservation);
        await reservations.SaveChange();
        return Result.Success("پیگیری با موفقیت حذف شد");
    }
}
