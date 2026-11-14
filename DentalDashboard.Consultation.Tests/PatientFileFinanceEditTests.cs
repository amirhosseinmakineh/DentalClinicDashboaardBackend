using DentalDashboard.Controllers;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Utilities.Time;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class PatientFileFinanceEditTests
{
    private static readonly DateOnly Day = new(2026, 11, 14);
    private static PatientFinancialCase Case() => new() {
        Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), TotalAmount = 1000, PrePaymentAmount = 1000,
        PaymentMethod = "نقدی", CreatedByUserId = Guid.NewGuid()
    };
    private static CompletePatientFileRequest Cash(PatientFinancialCase c) => new(c.PatientId, "cash", 1200, 1200, new() { [1] = 2 }, [], []);
    private static EditPatientFileFinanceRequest Edit(PatientFinancialCase c, CompletePatientFileRequest r) => new(PatientFileFinanceEditRules.EditHash(c), r);

    [Fact]
    public void CashCorrectionKeepsSameCaseAndAuditIdentityAndRequiresNewApprovals()
    {
        var c = Case(); var id = c.Id; var creator = c.CreatedByUserId;
        var w = new PatientFileWorkflow { FinancialCase = c, ConsultationCompleted = true,
            TreatmentConfirmedAtUtc = DateTime.UtcNow, FinanceConfirmedAtUtc = DateTime.UtcNow,
            ProfileConfirmedAtUtc = DateTime.UtcNow, ApprovedAtUtc = DateTime.UtcNow,
            ApprovedFinanceHash = PatientFileWorkflowRules.FinanceHash(c) };
        var r = Cash(c);
        Assert.Null(PatientFileFinanceEditRules.Validate(c, Edit(c, r)));
        PatientFileFinanceEditRules.Apply(w, r, Day);
        Assert.Equal(id, c.Id); Assert.Equal(creator, c.CreatedByUserId);
        Assert.Equal(1200, c.TotalAmount); Assert.Equal(1200, c.PrePaymentAmount);
        Assert.NotNull(c.UpdatedAt); Assert.True(w.ConsultationCompleted); Assert.NotNull(w.TreatmentConfirmedAtUtc);
        Assert.Null(w.FinanceConfirmedAtUtc); Assert.Null(w.ProfileConfirmedAtUtc);
        Assert.Null(w.ApprovedAtUtc); Assert.Null(w.ApprovedFinanceHash);
        Assert.False(PatientFileWorkflowRules.IsApproved(w));
    }

    [Fact]
    public void PendingDocumentsAreUpdatedInPlaceAndRemovedOnesAreSoftDeleted()
    {
        var c = Case(); c.PrePaymentAmount = 100; c.PaymentMethod = "اقساط";
        c.Cheques.Add(new PatientCheque { Id = 11, Amount = 500, SayadNumber = "old", OwnerName = "owner" });
        c.Cheques.Add(new PatientCheque { Id = 12, Amount = 400, SayadNumber = "remove", OwnerName = "owner" });
        var old = c.Cheques.First(); var removed = c.Cheques.Last();
        var r = Cash(c) with { PaymentMode = "installments", TotalAmount = 1100, CashReceived = 100,
            Cheques = [new(600, "corrected", "صاحب چک", Day.AddDays(-3), true, 11)],
            Notes = [new(400, "new-note", Day.AddDays(5))] };
        Assert.Null(PatientFileFinanceEditRules.Validate(c, Edit(c, r)));
        PatientFileFinanceEditRules.Apply(new() { FinancialCase = c }, r, Day);
        Assert.Same(old, c.Cheques.Single(x => !x.IsDeleted)); Assert.Equal(600, old.Amount);
        Assert.Equal("corrected", old.SayadNumber);
        Assert.Equal(Day.AddDays(-3), PatientFileFinanceEditRules.DueDay(old.DueDate));
        Assert.True(removed.IsDeleted); Assert.NotNull(removed.DeletedAt);
        Assert.Equal(400, Assert.Single(c.PromissoryNotes).Amount);
        Assert.Empty(c.Transactions);
    }

    [Fact]
    public void StaleFormsForeignIdsAndPartialCashCannotOverwriteFinance()
    {
        var c = Case(); var r = Cash(c); var request = Edit(c, r);
        c.TotalAmount += 1;
        Assert.NotNull(PatientFileFinanceEditRules.Validate(c, request));
        Assert.NotNull(PatientFileFinanceEditRules.Validate(c, Edit(c, r with { PatientId = Guid.NewGuid() })));
        Assert.NotNull(PatientFileFinanceEditRules.Validate(c, Edit(c, r with { CashReceived = 100 })));
        r = r with { PaymentMode = "installments", CashReceived = 200,
            Cheques = [new(1000, "123", "owner", Day, true, 999)] };
        Assert.NotNull(PatientFileFinanceEditRules.Validate(c, Edit(c, r)));
    }

    [Fact]
    public void ProcessedDocumentsAndTransactionsCannotBeRewrittenBySimpleCorrection()
    {
        var c = Case(); var cheque = new PatientCheque { Status = PatientChequeStatus.Paid };
        c.Cheques.Add(cheque); Assert.NotNull(PatientFileFinanceEditRules.BlockReason(c));
        cheque.Status = PatientChequeStatus.Unpaid; Assert.NotNull(PatientFileFinanceEditRules.BlockReason(c));
        cheque.Status = PatientChequeStatus.Pending; cheque.IsGuarantee = true;
        Assert.Null(PatientFileFinanceEditRules.BlockReason(c));
        c.Cheques.Clear(); c.Transactions.Add(new()); Assert.NotNull(PatientFileFinanceEditRules.BlockReason(c));
        c.Transactions.Clear(); c.Debts.Add(new()); Assert.NotNull(PatientFileFinanceEditRules.BlockReason(c));
    }

    [Fact]
    public void SqlUnspecifiedUtcDatesKeepTheirOriginalIranDueDay()
    {
        var utc = IranTimeHelper.GetIranDayRangeAsUtc(Day).StartUtc;
        Assert.Equal(13, utc.Day);
        Assert.Equal(Day, PatientFileFinanceEditRules.DueDay(DateTime.SpecifyKind(utc, DateTimeKind.Unspecified)));
    }
}
