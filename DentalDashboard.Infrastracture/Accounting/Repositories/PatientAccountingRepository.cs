using DentalDashboard.Domain.Accounting;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Accounting.Repositories;

public sealed class PatientAccountingRepository(DentalContext db) : AccountingReadRepository
{
    public override bool Supports(AccountingSection section) => section is AccountingSection.Patients or AccountingSection.Cheques
        or AccountingSection.Notes or AccountingSection.Installments or AccountingSection.Debts or AccountingSection.PatientPayments;
    protected override IQueryable<AccountingRow> Rows(AccountingSection section) => section switch {
        AccountingSection.Patients => db.PatientFinancialCases.AsNoTracking().Select(x => new AccountingRow {
            Id = x.Id.ToString(), Person = x.Patient.FirstName + " " + x.Patient.LastName, Description = x.Patient.PhoneNumber,
            Amount = x.TotalAmount, Status = ((int)x.Status).ToString(), Date = x.CreatedAt, CaseId = x.Id,
            PaidAmount = x.PrePaymentAmount + x.DepositAmount + (x.Transactions.Sum(t => (decimal?)t.Amount) ?? 0),
            DetailsJson = x.ServiceUnitCountsJson, UserId = x.PatientId ,
                IsGuarantee = false, Number = null, Owner = null }),
        AccountingSection.Cheques => db.PatientCheques.AsNoTracking().Where(x => !x.FinancialCase.IsDeleted)
            .Select(x => new AccountingRow { Id = x.Id.ToString(), Person = x.FinancialCase.Patient.FirstName + " " + x.FinancialCase.Patient.LastName,
                Description = x.FinancialCase.Patient.PhoneNumber, Amount = x.Amount, Status = ((int)x.Status).ToString(), Date = x.DueDate,
                CaseId = x.PatientFinancialCaseId, IsGuarantee = x.IsGuarantee, Number = x.SayadNumber, Owner = x.OwnerName, UserId = x.FinancialCase.PatientId ,
                PaidAmount = null, DetailsJson = null }),
        AccountingSection.Notes => db.PatientPromissoryNotes.AsNoTracking().Where(x => !x.FinancialCase.IsDeleted)
            .Select(x => new AccountingRow { Id = x.Id.ToString(), Person = x.FinancialCase.Patient.FirstName + " " + x.FinancialCase.Patient.LastName,
                Description = x.FinancialCase.Patient.PhoneNumber, Amount = x.Amount, Status = ((int)x.Status).ToString(), Date = x.DueDate,
                CaseId = x.PatientFinancialCaseId, IsGuarantee = x.IsGuarantee, Number = x.SerialNumber, UserId = x.FinancialCase.PatientId ,
                Owner = null, PaidAmount = null, DetailsJson = null }),
        AccountingSection.Installments => db.PatientInstallments.AsNoTracking().Where(x => !x.IsDeleted && !x.FinancialCase.IsDeleted &&
            x.FinancialCase.Status != PatientFinancialCaseStatus.Cancelled).Select(x => new AccountingRow {
                Id = x.Id.ToString(), Person = x.FinancialCase.Patient.FirstName + " " + x.FinancialCase.Patient.LastName,
                Description = x.FinancialCase.Patient.PhoneNumber, Amount = x.Amount, Status = x.PaidAtUtc == null ? "Pending" : "Paid",
                Date = x.DueDate, CaseId = x.PatientFinancialCaseId, UserId = x.FinancialCase.PatientId ,
                IsGuarantee = false, Number = null, Owner = null, PaidAmount = null, DetailsJson = null }),
        AccountingSection.Debts => db.PatientDebts.AsNoTracking().Where(x => !x.FinancialCase.IsDeleted)
            .Select(x => new AccountingRow { Id = x.Id.ToString(), Person = x.FinancialCase.Patient.FirstName + " " + x.FinancialCase.Patient.LastName,
                Description = x.SourceType == PatientDebtSourceType.Cheque ? "بدهی چک" : "بدهی سفته", Amount = x.Amount, Status = ((int)x.Status).ToString(), Date = x.DueDate,
                CaseId = x.PatientFinancialCaseId, Number = x.SourceId.ToString(), UserId = x.FinancialCase.PatientId ,
                IsGuarantee = false, Owner = null, PaidAmount = null, DetailsJson = null }),
        AccountingSection.PatientPayments => db.PatientFinancialTransactions.AsNoTracking().Where(x => !x.FinancialCase.IsDeleted)
            .Select(x => new AccountingRow { Id = x.Id.ToString(), Person = x.FinancialCase.Patient.FirstName + " " + x.FinancialCase.Patient.LastName,
                Description = x.SourceType == PatientFinancialTransactionSourceType.Cheque ? "وصول چک" : x.SourceType == PatientFinancialTransactionSourceType.PromissoryNote ? "وصول سفته" : x.SourceType == PatientFinancialTransactionSourceType.Installment ? "پرداخت قسط" : "پرداخت درمان", Amount = x.Amount, Status = "Paid", Date = x.CreatedAt,
                CaseId = x.PatientFinancialCaseId, Number = x.SourceId.ToString(), UserId = x.FinancialCase.PatientId ,
                IsGuarantee = false, Owner = null, PaidAmount = null, DetailsJson = null }),
        _ => throw new ArgumentOutOfRangeException(nameof(section))
    };
    public override async Task<IReadOnlyList<AccountingMetric>> MetricsAsync(AccountingFilter filter, CancellationToken ct)
    {
        var cases = Filter(Rows(AccountingSection.Patients), filter).Where(x => x.Status != "3");
        var collected = await cases.SumAsync(x => x.PaidAmount, ct) ?? 0;
        var total = await cases.SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        return [new("TreatmentValue", total, await cases.CountAsync(ct)), new("PatientCollected", collected, 0),
            new("TreatmentRemaining", await cases.SumAsync(x => (decimal?)(x.Amount > (x.PaidAmount ?? 0) ? x.Amount - (x.PaidAmount ?? 0) : 0), ct) ?? 0, 0),
            await Metric(AccountingSection.Debts, new() { FromDate = filter.FromDate, ToDate = filter.ToDate, Status = "1" }, "OpenDebt", ct)];
    }
}
