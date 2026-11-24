using System.Text.Json;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Accounting.Repositories;

public sealed class StaffAccountingRepository(DentalContext db) : AccountingReadRepository
{
    public override bool Supports(AccountingSection section) => section is AccountingSection.SecretarySales or AccountingSection.ConsultantRewards
        or AccountingSection.ConsultantWallets or AccountingSection.Referrals or AccountingSection.SecretaryWallets or AccountingSection.PatientWallets
        or AccountingSection.SecretaryWalletTransactions or AccountingSection.PatientWalletTransactions;
    protected override IQueryable<AccountingRow> Rows(AccountingSection section) => section switch {
        AccountingSection.SecretarySales => db.SecretarySales.AsNoTracking().Select(x => new AccountingRow {
            Id = x.Id.ToString(), Person = x.SecretaryUser.FirstName + " " + x.SecretaryUser.LastName,
            Description = x.Service.Title + " / " + x.PatientUser.FirstName + " " + x.PatientUser.LastName,
            Amount = x.SecretaryReward, Status = ((int)x.Status).ToString(), Date = x.CreatedAt, PaidAmount = x.SalePrice, UserId = x.SecretaryUserId ,
                CaseId = null, IsGuarantee = false, Number = null, Owner = null, DetailsJson = null }),
        AccountingSection.ConsultantRewards => db.Reservations.IgnoreQueryFilters().AsNoTracking().Where(x => !x.IsDeleted && !x.IsCanceled &&
            (x.OwnerType != ReservationOwnerType.Secretary || x.ServiceRewardSnapshotJson != null) && x.ConsultantRewardEligibleAt != null)
            .Select(x => new AccountingRow { Id = x.Id.ToString(),
                Person = x.RewardConsultantProfileId.HasValue ? x.RewardConsultantProfile!.User.FirstName + " " + x.RewardConsultantProfile.User.LastName :
                    x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName,
                Description = x.LeadAssignment.UserName ?? "", Amount = x.ConsultantRewardAmount ?? x.PatientCount * ConsultantServiceRewardRule.LegacyRewardPerPatient,
                Status = x.ConsultantRewardApprovedByAdmin == true ? "Approved" : x.ConsultantRewardApprovedByAdmin == false ? "Rejected" : "Pending",
                Date = x.ConsultantRewardEligibleAt, DetailsJson = x.ServiceRewardSnapshotJson, LegacyRewardAmount = x.PatientCount * ConsultantServiceRewardRule.LegacyRewardPerPatient,
                UserId = x.RewardConsultantProfileId.HasValue ? x.RewardConsultantProfile!.UserId : x.ConsultantProfile.UserId ,
                CaseId = null, IsGuarantee = false, Number = null, Owner = null, PaidAmount = null }),
        AccountingSection.ConsultantWallets => db.Reservations.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.ConsultantRewardApprovedByAdmin == true && (x.OwnerType != ReservationOwnerType.Secretary || x.ServiceRewardSnapshotJson != null))
            .GroupBy(x => new {
                UserId = x.RewardConsultantProfileId.HasValue ? x.RewardConsultantProfile!.UserId : x.ConsultantProfile.UserId,
                Name = x.RewardConsultantProfileId.HasValue ? x.RewardConsultantProfile!.User.FirstName + " " + x.RewardConsultantProfile.User.LastName : x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName
            }).Select(g => new AccountingRow {
                Id = g.Key.UserId.ToString(), UserId = g.Key.UserId, Person = g.Key.Name,
                Description = "مجموع پاداش‌های تأییدشده؛ تسویه بانکی نیست", Amount = g.Sum(x => x.ConsultantRewardAmount ?? x.PatientCount * ConsultantServiceRewardRule.LegacyRewardPerPatient),
                Status = "Balance", Date = g.Max(x => x.ConsultantRewardReviewedAt), CaseId = null,
                IsGuarantee = false, Number = null, Owner = null, PaidAmount = null, DetailsJson = null
            }),
        AccountingSection.Referrals => db.PatientReferrals.AsNoTracking().Select(x => new AccountingRow {
            Id = x.Id.ToString(), Person = x.ReferrerPatientUser.FirstName + " " + x.ReferrerPatientUser.LastName,
            Description = x.ReferredFirstName + " " + x.ReferredLastName + " / " + x.ReferredPhoneNumber,
            Amount = x.RewardAmount, Status = ((int)x.Status).ToString(), Date = x.CreatedAt, Owner = x.RejectionReason, UserId = x.ReferrerPatientUserId ,
                CaseId = null, IsGuarantee = false, Number = null, PaidAmount = null, DetailsJson = null }),
        AccountingSection.SecretaryWallets => db.SecretaryWallets.AsNoTracking().Select(x => new AccountingRow {
            Id = x.Id.ToString(), Person = x.SecretaryUser.FirstName + " " + x.SecretaryUser.LastName,
            Description = x.SecretaryUser.PhoneNumber, Amount = x.Balance, Status = "Balance", Date = x.UpdatedAt ?? x.CreatedAt, UserId = x.SecretaryUserId ,
                CaseId = null, IsGuarantee = false, Number = null, Owner = null, PaidAmount = null, DetailsJson = null }),
        AccountingSection.PatientWallets => db.PatientWallets.AsNoTracking().Select(x => new AccountingRow {
            Id = x.Id.ToString(), Person = x.PatientUser.FirstName + " " + x.PatientUser.LastName,
            Description = x.PatientUser.PhoneNumber, Amount = x.Balance, Status = "Balance", Date = x.UpdatedAt ?? x.CreatedAt, UserId = x.PatientUserId ,
                CaseId = null, IsGuarantee = false, Number = null, Owner = null, PaidAmount = null, DetailsJson = null }),
        AccountingSection.SecretaryWalletTransactions => db.SecretaryWalletTransactions.AsNoTracking().Select(x => new AccountingRow {
            Id = x.Id.ToString(), Person = x.SecretaryUser.FirstName + " " + x.SecretaryUser.LastName,
            Description = x.Description, Amount = x.Amount, Status = ((int)x.TransactionType).ToString(), Date = x.CreatedAt,
            Number = x.SecretarySaleId == null ? null : x.SecretarySaleId.ToString(), UserId = x.SecretaryUserId ,
                CaseId = null, IsGuarantee = false, Owner = null, PaidAmount = null, DetailsJson = null }),
        AccountingSection.PatientWalletTransactions => db.PatientWalletTransactions.AsNoTracking().Select(x => new AccountingRow {
            Id = x.Id.ToString(), Person = x.PatientUser.FirstName + " " + x.PatientUser.LastName,
            Description = x.Description, Amount = x.Amount, Status = ((int)x.TransactionType).ToString(), Date = x.CreatedAt,
            Number = x.PatientReferralId.ToString(), UserId = x.PatientUserId ,
                CaseId = null, IsGuarantee = false, Owner = null, PaidAmount = null, DetailsJson = null }),
        _ => throw new ArgumentOutOfRangeException(nameof(section))
    };
    private static decimal QuotedAmount(AccountingRow row)
    {
        if (row.DetailsJson is null) return row.LegacyRewardAmount;
        try {
            using var snapshot = JsonDocument.Parse(row.DetailsJson);
            return snapshot.RootElement.TryGetProperty("Total", out var total) && total.TryGetDecimal(out var amount) ? amount : row.Amount;
        } catch (JsonException) { return row.Amount; }
    }
    protected override void Enrich(IReadOnlyList<AccountingRow> items)
    {
        foreach (var row in items.Where(x => x.Status == "Rejected")) row.Amount = QuotedAmount(row);
    }
    public override async Task<IReadOnlyList<AccountingMetric>> MetricsAsync(AccountingFilter filter, CancellationToken ct)
    {
        var result = new List<AccountingMetric>();
        foreach (var (section, prefix, statuses) in new[] {
            (AccountingSection.SecretarySales, "Secretary", new[] { "1", "2", "3" }),
            (AccountingSection.ConsultantRewards, "Consultant", new[] { "Pending", "Approved", "Rejected" }),
            (AccountingSection.Referrals, "Referral", new[] { "3", "4", "5" })
        }) {
            for (var i = 0; i < statuses.Length; i++) {
                var currentFilter = new AccountingFilter { FromDate = filter.FromDate, ToDate = filter.ToDate, Status = statuses[i] };
                var key = prefix + new[] { "Pending", "Approved", "Rejected" }[i];
                if (section == AccountingSection.ConsultantRewards && statuses[i] == "Rejected") {
                    var quotes = await Filter(Rows(section), currentFilter).Select(x => new AccountingRow {
                        DetailsJson = x.DetailsJson, Amount = x.Amount, LegacyRewardAmount = x.LegacyRewardAmount
                    }).ToListAsync(ct);
                    result.Add(new(key, quotes.Sum(QuotedAmount), quotes.Count));
                } else result.Add(await Metric(section, currentFilter, key, ct));
            }
        }
        return result;
    }
}
