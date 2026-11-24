using System.Text.Json;
using System.Text.Json.Nodes;
using DentalDashboard.Domain.Models;
using System.Data;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public sealed record ConsultantRewardItem(
    long ReservationId, long ConsultantProfileId, Guid ConsultantUserId,
    string ConsultantName, string PatientName, string? PatientPhoneNumber,
    int PatientCount, decimal RewardAmount, string Status,
    string SecretaryApprovalStatus, DateTime? SecretaryReviewedAt, DateTime? AdminReviewedAt, string? RewardCalculationJson = null);

public sealed record ConsultantWalletReport(
    decimal Balance, decimal TotalApprovedRewards, int ApprovedPatientCount,
    int PendingPatientCount, IReadOnlyList<ConsultantRewardItem> Items);

public sealed class ConsultantRewardService(DentalContext context)
{
    public const decimal RewardPerPatient = ConsultantServiceRewardRule.LegacyRewardPerPatient;

    public async Task<ConsultantWalletReport> GetConsultantWalletAsync(
        Guid consultantUserId, CancellationToken cancellationToken)
    {
        var items = await context.Reservations.IgnoreQueryFilters().AsNoTracking()
            .Where(x => (x.OwnerType != ReservationOwnerType.Secretary || x.ServiceRewardSnapshotJson != null) &&
                (x.RewardConsultantProfileId.HasValue ? x.RewardConsultantProfile!.UserId : x.ConsultantProfile.UserId) == consultantUserId &&
                (x.ConsultantRewardApprovedByAdmin == true ||
                 (!x.IsDeleted && !x.IsCanceled && x.ConsultantRewardEligibleAt != null &&
                  x.SecretaryApprovedConsultantConfirmation == true)))
            .OrderByDescending(x => x.SecretaryReviewedAt)
            .Select(x => new ConsultantRewardItem(
                x.Id, x.RewardConsultantProfileId ?? x.ConsultantProfileId,
                x.RewardConsultantProfileId.HasValue ? x.RewardConsultantProfile!.UserId : x.ConsultantProfile.UserId,
                x.RewardConsultantProfileId.HasValue ? (x.RewardConsultantProfile!.User.FirstName + " " + x.RewardConsultantProfile.User.LastName).Trim() :
                    (x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName).Trim(),
                x.PatientUser == null ? x.LeadAssignment.UserName :
                    (x.PatientUser.FirstName + " " + x.PatientUser.LastName).Trim(),
                x.PatientUser == null ? x.LeadAssignment.PhoneNumber : x.PatientUser.PhoneNumber,
                x.PatientCount, x.ConsultantRewardAmount ?? x.PatientCount * RewardPerPatient,
                x.ConsultantRewardApprovedByAdmin == true ? "Approved" :
                    x.ConsultantRewardApprovedByAdmin == false ? "Rejected" : "Pending", "Approved",
                x.SecretaryReviewedAt, x.ConsultantRewardReviewedAt, x.ServiceRewardSnapshotJson))
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
            !x.IsDeleted && !x.IsCanceled && (x.OwnerType != ReservationOwnerType.Secretary || x.ServiceRewardSnapshotJson != null) &&
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
                x.Id, x.RewardConsultantProfileId ?? x.ConsultantProfileId,
                x.RewardConsultantProfileId.HasValue ? x.RewardConsultantProfile!.UserId : x.ConsultantProfile.UserId,
                x.RewardConsultantProfileId.HasValue ? (x.RewardConsultantProfile!.User.FirstName + " " + x.RewardConsultantProfile.User.LastName).Trim() :
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
                x.SecretaryReviewedAt, x.ConsultantRewardReviewedAt, x.ServiceRewardSnapshotJson))
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string Message)> ReviewAsync(
        long reservationId, Guid adminUserId, bool approved,
        CancellationToken cancellationToken, string? rejectionReason = null)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var reservation = await context.Reservations.FirstOrDefaultAsync(
            x => x.Id == reservationId, cancellationToken);
        if (reservation is null || reservation.IsDeleted || reservation.IsCanceled)
            return (false, "رزرو معتبر یافت نشد.");
        if (reservation.OwnerType == ReservationOwnerType.Secretary && reservation.ServiceRewardSnapshotJson == null)
            return (false, "این رزرو توسط مشاور ثبت نشده است.");
        if (reservation.AttendanceConfirmationStatus != ReservationAttendanceConfirmationStatus.SecretaryApproved ||
            reservation.SecretaryApprovedConsultantConfirmation != true ||
            reservation.ConsultantRewardEligibleAt is null)
            return (false, "ابتدا منشی باید حضور بیمار را تأیید کند.");
        if (reservation.ConsultantRewardApprovedByAdmin.HasValue)
            return (false, "پاداش این رزرو قبلاً توسط ادمین بررسی شده است.");

        decimal amount;
        if (reservation.ServiceRewardSnapshotJson is { } snapshotJson) {
            ServiceRewardSnapshot? snapshot;
            try { snapshot = JsonSerializer.Deserialize<ServiceRewardSnapshot>(snapshotJson); }
            catch (JsonException) { return (false, "ریز محاسبه پاداش معتبر نیست؛ با مدیر سیستم بررسی کنید."); }
            if (snapshot is null || snapshot.ConsultantProfileId != reservation.RewardConsultantProfileId)
                return (false, "دریافت‌کننده پاداش با ریز محاسبه تطابق ندارد.");
            amount = snapshot.Total;
            if (!approved) {
                if (string.IsNullOrWhiteSpace(rejectionReason) || rejectionReason.Trim().Length > 1000)
                    return (false, "دلیل رد پاداش را با حداکثر ۱۰۰۰ کاراکتر بنویسید.");
                var document = JsonNode.Parse(snapshotJson)!;
                document["AdminReviewNote"] = rejectionReason.Trim();
                reservation.ServiceRewardSnapshotJson = document.ToJsonString();
            }
        } else amount = Math.Max(1, reservation.PatientCount) * RewardPerPatient;
        reservation.ConsultantRewardApprovedByAdmin = approved;
        reservation.ConsultantRewardAmount = approved ? amount : 0;
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
