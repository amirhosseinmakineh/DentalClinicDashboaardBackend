using System.Text;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.PatientReferrals;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public sealed record AdminAccountingSummary(
    decimal ApprovedSalesAmount,
    decimal ApprovedSecretaryRewards,
    decimal PendingSecretaryRewards,
    decimal ApprovedConsultantRewards,
    decimal PendingConsultantRewards,
    decimal PaidReferralRewards,
    decimal PendingReferralRewards,
    int PendingSecretarySalesCount,
    int PendingConsultantRewardsCount,
    int PendingReferralRewardsCount);

public sealed record AdminStaffAccountingRow(
    Guid UserId,
    string FullName,
    string Role,
    int ApprovedItemsCount,
    int PendingItemsCount,
    decimal GrossAmount,
    decimal ApprovedRewardAmount,
    decimal PendingRewardAmount,
    DateTime? LastActivityAt);

public sealed record AdminAccountingReport(
    AdminAccountingSummary Summary,
    IReadOnlyList<AdminStaffAccountingRow> Staff);

public sealed class AdminAccountingReportService(DentalContext context)
{
    public async Task<AdminAccountingReport> GetAsync(
        DateTime? fromDate,
        DateTime? toDate,
        string? search,
        CancellationToken cancellationToken)
    {
        var from = fromDate?.Date;
        var until = toDate?.Date.AddDays(1);

        var sales = context.SecretarySales.AsNoTracking();
        if (from.HasValue) sales = sales.Where(x => x.CreatedAt >= from.Value);
        if (until.HasValue) sales = sales.Where(x => x.CreatedAt < until.Value);

        var consultantReservations = context.Reservations.AsNoTracking().Where(x =>
            !x.IsDeleted && !x.IsCanceled && x.OwnerType != ReservationOwnerType.Secretary &&
            (x.ConsultantSaysPatientAttended == true || x.SecretaryReviewedAt != null));
        if (from.HasValue) consultantReservations = consultantReservations.Where(x => x.CreatedAt >= from.Value);
        if (until.HasValue) consultantReservations = consultantReservations.Where(x => x.CreatedAt < until.Value);

        var referrals = context.PatientReferrals.AsNoTracking();
        if (from.HasValue) referrals = referrals.Where(x => x.CreatedAt >= from.Value);
        if (until.HasValue) referrals = referrals.Where(x => x.CreatedAt < until.Value);

        var secretaryRows = await sales
            .GroupBy(x => new { x.SecretaryUserId, x.SecretaryUser.FirstName, x.SecretaryUser.LastName })
            .Select(group => new AdminStaffAccountingRow(
                group.Key.SecretaryUserId,
                (group.Key.FirstName + " " + group.Key.LastName).Trim(),
                "Secretary",
                group.Count(x => x.Status == SecretarySaleStatus.Approved),
                group.Count(x => x.Status == SecretarySaleStatus.PendingAdminApproval),
                group.Where(x => x.Status == SecretarySaleStatus.Approved).Sum(x => (decimal?)x.SalePrice) ?? 0,
                group.Where(x => x.Status == SecretarySaleStatus.Approved).Sum(x => (decimal?)x.SecretaryReward) ?? 0,
                group.Where(x => x.Status == SecretarySaleStatus.PendingAdminApproval).Sum(x => (decimal?)x.SecretaryReward) ?? 0,
                group.Max(x => (DateTime?)x.CreatedAt)))
            .ToListAsync(cancellationToken);

        var consultantRows = await consultantReservations
            .GroupBy(x => new
            {
                x.ConsultantProfile.UserId,
                x.ConsultantProfile.User.FirstName,
                x.ConsultantProfile.User.LastName
            })
            .Select(group => new AdminStaffAccountingRow(
                group.Key.UserId,
                (group.Key.FirstName + " " + group.Key.LastName).Trim(),
                "Consultant",
                group.Count(x => x.ConsultantRewardApprovedByAdmin == true),
                group.Count(x => x.ConsultantRewardApprovedByAdmin == null && x.ConsultantRewardEligibleAt != null),
                0,
                group.Where(x => x.ConsultantRewardApprovedByAdmin == true)
                    .Sum(x => (decimal?)(x.ConsultantRewardAmount ?? x.PatientCount * ConsultantRewardService.RewardPerPatient)) ?? 0,
                group.Where(x => x.ConsultantRewardApprovedByAdmin == null && x.ConsultantRewardEligibleAt != null)
                    .Sum(x => (decimal?)(x.ConsultantRewardAmount ?? x.PatientCount * ConsultantRewardService.RewardPerPatient)) ?? 0,
                group.Max(x => (DateTime?)x.CreatedAt)))
            .ToListAsync(cancellationToken);

        IEnumerable<AdminStaffAccountingRow> staff = secretaryRows.Concat(consultantRows);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            staff = staff.Where(x => x.FullName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var summary = new AdminAccountingSummary(
            await sales.Where(x => x.Status == SecretarySaleStatus.Approved).SumAsync(x => (decimal?)x.SalePrice, cancellationToken) ?? 0,
            await sales.Where(x => x.Status == SecretarySaleStatus.Approved).SumAsync(x => (decimal?)x.SecretaryReward, cancellationToken) ?? 0,
            await sales.Where(x => x.Status == SecretarySaleStatus.PendingAdminApproval).SumAsync(x => (decimal?)x.SecretaryReward, cancellationToken) ?? 0,
            await consultantReservations.Where(x => x.ConsultantRewardApprovedByAdmin == true)
                .SumAsync(x => (decimal?)(x.ConsultantRewardAmount ?? x.PatientCount * ConsultantRewardService.RewardPerPatient), cancellationToken) ?? 0,
            await consultantReservations.Where(x => x.ConsultantRewardApprovedByAdmin == null && x.ConsultantRewardEligibleAt != null)
                .SumAsync(x => (decimal?)(x.ConsultantRewardAmount ?? x.PatientCount * ConsultantRewardService.RewardPerPatient), cancellationToken) ?? 0,
            await referrals.Where(x => x.Status == PatientReferralStatus.ApprovedRewarded).SumAsync(x => (decimal?)x.RewardAmount, cancellationToken) ?? 0,
            await referrals.Where(x => x.Status == PatientReferralStatus.ReservedPendingAdminApproval).SumAsync(x => (decimal?)x.RewardAmount, cancellationToken) ?? 0,
            await sales.CountAsync(x => x.Status == SecretarySaleStatus.PendingAdminApproval, cancellationToken),
            await consultantReservations.CountAsync(x => x.ConsultantRewardApprovedByAdmin == null && x.ConsultantRewardEligibleAt != null, cancellationToken),
            await referrals.CountAsync(x => x.Status == PatientReferralStatus.ReservedPendingAdminApproval, cancellationToken));

        return new AdminAccountingReport(summary, staff
            .OrderByDescending(x => x.ApprovedRewardAmount + x.PendingRewardAmount)
            .ThenBy(x => x.FullName)
            .ToList());
    }

    public static byte[] ExportCsv(AdminAccountingReport report)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Role,FullName,ApprovedItems,PendingItems,GrossAmount,ApprovedReward,PendingReward,LastActivityAt");
        foreach (var row in report.Staff)
        {
            csv.Append(Escape(row.Role)).Append(',')
                .Append(Escape(row.FullName)).Append(',')
                .Append(row.ApprovedItemsCount).Append(',')
                .Append(row.PendingItemsCount).Append(',')
                .Append(row.GrossAmount).Append(',')
                .Append(row.ApprovedRewardAmount).Append(',')
                .Append(row.PendingRewardAmount).Append(',')
                .Append(row.LastActivityAt?.ToString("O") ?? string.Empty)
                .AppendLine();
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv.ToString());
    }

    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
