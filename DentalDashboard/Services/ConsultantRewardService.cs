using System.Data;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public sealed record ConsultantRewardItem(
    long ReservationId, long ConsultantProfileId, Guid ConsultantUserId,
    string ConsultantName, string PatientName, string? PatientPhoneNumber,
    int PatientCount, decimal RewardAmount, string Status,
    string SecretaryApprovalStatus, DateTime? SecretaryReviewedAt, DateTime? AdminReviewedAt);

public sealed record ConsultantWalletReport(
    decimal Balance, decimal TotalApprovedRewards, int ApprovedPatientCount,
    int PendingPatientCount, IReadOnlyList<ConsultantRewardItem> Items);

public sealed class ConsultantRewardService(DentalContext context)
{
    public const decimal RewardPerPatient = 1_000_000m;

    public async Task<ConsultantWalletReport> GetConsultantWalletAsync(
        Guid consultantUserId, CancellationToken cancellationToken)
    {
        var items = await context.Reservations.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.OwnerType != ReservationOwnerType.Secretary &&
                x.ConsultantProfile.UserId == consultantUserId &&
                (x.ConsultantRewardApprovedByAdmin == true ||
                 (!x.IsDeleted && !x.IsCanceled && x.ConsultantRewardEligibleAt != null &&
                  x.SecretaryApprovedConsultantConfirmation == true)))
            .OrderByDescending(x => x.SecretaryReviewedAt)
            .Select(x => new ConsultantRewardItem(
                x.Id, x.ConsultantProfileId, x.ConsultantProfile.UserId,
                (x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName).Trim(),
                x.PatientUser == null ? x.LeadAssignment.UserName :
                    (x.PatientUser.FirstName + " " + x.PatientUser.LastName).Trim(),
                x.PatientUser == null ? x.LeadAssignment.PhoneNumber : x.PatientUser.PhoneNumber,
                x.PatientCount, x.ConsultantRewardAmount ?? x.PatientCount * RewardPerPatient,
                x.ConsultantRewardApprovedByAdmin == true ? "Approved" :
                    x.ConsultantRewardApprovedByAdmin == false ? "Rejected" : "Pending", "Approved",
                x.SecretaryReviewedAt, x.ConsultantRewardReviewedAt))
            .ToListAsync(cancellationToken);

        return new ConsultantWalletReport(
            items.Where(x => x.Status == "Approved").Sum(x => x.RewardAmount),
            items.Where(x => x.Status == "Approved").Sum(x => x.RewardAmount),
            items.Where(x => x.Status == "Approved").Sum(x => x.PatientCount),
            items.Where(x => x.Status == "Pending").Sum(x => x.PatientCount), items);
    }

    public async Task<IReadOnlyList<ConsultantRewardItem>> GetAdminReportAsync(
        string? status, CancellationToken cancellationToken)
    {
        var query = context.Reservations.AsNoTracking().Where(x =>
            !x.IsDeleted && !x.IsCanceled && x.OwnerType != ReservationOwnerType.Secretary &&
            (x.ConsultantSaysPatientAttended == true || x.SecretaryReviewedAt != null));
        query = status switch
        {
            "Approved" => query.Where(x => x.ConsultantRewardApprovedByAdmin == true),
            "Rejected" => query.Where(x => x.ConsultantRewardApprovedByAdmin == false),
            "Pending" => query.Where(x => x.ConsultantRewardApprovedByAdmin == null &&
                                         x.ConsultantRewardEligibleAt != null),
            _ => query
        };

        return await query.OrderByDescending(x => x.SecretaryReviewedAt)
            .Select(x => new ConsultantRewardItem(
                x.Id, x.ConsultantProfileId, x.ConsultantProfile.UserId,
                (x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName).Trim(),
                x.PatientUser == null ? x.LeadAssignment.UserName :
                    (x.PatientUser.FirstName + " " + x.PatientUser.LastName).Trim(),
                x.PatientUser == null ? x.LeadAssignment.PhoneNumber : x.PatientUser.PhoneNumber,
                x.PatientCount, x.ConsultantRewardAmount ?? x.PatientCount * RewardPerPatient,
                x.ConsultantRewardApprovedByAdmin == true ? "Approved" :
                    x.ConsultantRewardApprovedByAdmin == false ? "Rejected" :
                    x.ConsultantRewardEligibleAt != null ? "Pending" :
                    x.SecretaryApprovedConsultantConfirmation == true ? "LegacyNotEligible" : "WaitingSecretary",
                x.SecretaryApprovedConsultantConfirmation == true ? "Approved" :
                    x.SecretaryApprovedConsultantConfirmation == false ? "Rejected" : "Waiting",
                x.SecretaryReviewedAt, x.ConsultantRewardReviewedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string Message)> ReviewAsync(
        long reservationId, Guid adminUserId, bool approved,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var reservation = await context.Reservations.FirstOrDefaultAsync(
            x => x.Id == reservationId, cancellationToken);
        if (reservation is null || reservation.IsDeleted || reservation.IsCanceled)
            return (false, "رزرو معتبر یافت نشد.");
        if (reservation.OwnerType == ReservationOwnerType.Secretary)
            return (false, "این رزرو توسط مشاور ثبت نشده است.");
        if (reservation.AttendanceConfirmationStatus != ReservationAttendanceConfirmationStatus.SecretaryApproved ||
            reservation.SecretaryApprovedConsultantConfirmation != true ||
            reservation.ConsultantRewardEligibleAt is null)
            return (false, "ابتدا منشی باید حضور بیمار را تأیید کند.");
        if (reservation.ConsultantRewardApprovedByAdmin.HasValue)
            return (false, "پاداش این رزرو قبلاً توسط ادمین بررسی شده است.");

        reservation.ConsultantRewardApprovedByAdmin = approved;
        reservation.ConsultantRewardAmount = approved
            ? Math.Max(1, reservation.PatientCount) * RewardPerPatient : 0;
        reservation.ConsultantRewardReviewedByAdminId = adminUserId;
        reservation.ConsultantRewardReviewedAt = DateTime.UtcNow;
        reservation.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, approved
            ? "پاداش تأیید و کیف پول مشاور شارژ شد."
            : "پاداش رزرو رد شد.");
    }
}
