using DentalDashboard.Controllers;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class PatientInstallmentManagementTests
{
    private static readonly DateOnly Today = new(2026, 11, 23);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InstallmentTableIsValidWithOrWithoutSecurity(bool withGuarantee)
    {
        var request = new CompletePatientFileRequest(Guid.NewGuid(), "installments", 150, 0,
            new() { [1] = 1 }, [], withGuarantee ? [new(300, "guarantee", Today)] : [],
            Installments: [new(50, Today.ToDateTime(TimeOnly.MinValue)), new(100, Today.AddMonths(1).ToDateTime(TimeOnly.MinValue))]);
        Assert.Null(PatientCompletionValidation.Validate(request, Today));
        var c = new PatientFinancialCase { TotalAmount = 150, PaymentMethod = "اقساط" };
        c.Installments.Add(new() { Amount = 50 }); c.Installments.Add(new() { Amount = 100 });
        Assert.True(PatientFinanceCoverage.CanConfirm(c));
        var workflow = new PatientFileWorkflow { FinancialCase = c, ConsultationCompleted = true,
            TreatmentConfirmedAtUtc = DateTime.UtcNow, FinanceConfirmedAtUtc = DateTime.UtcNow,
            ProfileConfirmedAtUtc = DateTime.UtcNow };
        Assert.True(PatientFileWorkflowRules.CanApprove(workflow));
        Assert.Empty(c.Transactions);
    }

    [Fact]
    public void FiltersIncludeBothCalendarDaysAndSeparatePaidOverdueAndCurrentRows()
    {
        var day = Today.ToDateTime(TimeOnly.MinValue);
        var rows = new[] {
            Row(1, day.AddDays(-2)), Row(2, day.AddTicks(-1)), Row(3, day),
            Row(4, day.AddDays(1).AddTicks(-1)), Row(5, day.AddDays(1)), Row(6, day.AddDays(-1))
        };
        rows[5].PaidAtUtc = DateTime.UtcNow;
        var range = new AdminInstallmentsFilter { FromDate = Today.AddDays(-1), ToDate = Today };
        Assert.Equal(new long[] { 2, 3, 4, 6 }, PatientInstallmentReportRules.Filter(rows.AsQueryable(), range, Today).Select(x => x.Id));
        range.Status = "overdue";
        Assert.Equal(2, Assert.Single(PatientInstallmentReportRules.Filter(rows.AsQueryable(), range, Today)).Id);
        range.Status = "pending";
        Assert.Equal(new long[] { 3, 4 }, PatientInstallmentReportRules.Filter(rows.AsQueryable(), range, Today).Select(x => x.Id));
        range.Status = "paid";
        Assert.Equal(6, Assert.Single(PatientInstallmentReportRules.Filter(rows.AsQueryable(), range, Today)).Id);
    }

    [Fact]
    public void DeletedSchedulesAndCancelledCasesNeverAppear()
    {
        var rows = Enumerable.Range(1, 4).Select(i => Row(i, Today.ToDateTime(TimeOnly.MinValue))).ToArray();
        rows[0].IsDeleted = true; rows[1].FinancialCase.IsDeleted = true;
        rows[2].FinancialCase.Status = PatientFinancialCaseStatus.Cancelled;
        Assert.Equal(4, Assert.Single(PatientInstallmentReportRules.Filter(rows.AsQueryable(), new(), Today)).Id);
    }

    [Fact]
    public void RecordingPaymentRespectsCashDepositAndPriorReceiptsAndRejectsDuplicates()
    {
        var c = new PatientFinancialCase { Id = Guid.NewGuid(), TotalAmount = 150, PrePaymentAmount = 20, DepositAmount = 10 };
        var row = new PatientInstallment { PatientFinancialCaseId = c.Id, Amount = 50 };
        Assert.Null(PatientInstallmentRules.PaymentError(c, row, 70));
        Assert.NotNull(PatientInstallmentRules.PaymentError(c, row, 71));
        row.PaidAtUtc = DateTime.UtcNow;
        Assert.NotNull(PatientInstallmentRules.PaymentError(c, row, 0));
        row.PaidAtUtc = null; c.Status = PatientFinancialCaseStatus.Cancelled;
        Assert.NotNull(PatientInstallmentRules.PaymentError(c, row, 0));
        c.Status = PatientFinancialCaseStatus.Active; row.IsDeleted = true;
        Assert.NotNull(PatientInstallmentRules.PaymentError(c, row, 0));
    }

    [Fact]
    public void LegacyInstallmentOnlyCasesReopenAsInstallmentsInsteadOfCash()
    {
        var c = new PatientFinancialCase { PaymentMethod = null };
        Assert.Equal("cash", PatientFileFinanceEditRules.PaymentMode(c));
        var row = new PatientInstallment { Amount = 150, DueDate = Today.ToDateTime(TimeOnly.MinValue) };
        c.Installments.Add(row);
        Assert.Equal("installments", PatientFileFinanceEditRules.PaymentMode(c));
        row.IsDeleted = true;
        Assert.Equal("cash", PatientFileFinanceEditRules.PaymentMode(c));
        c.PaymentMethod = "اقساط";
        Assert.Equal("installments", PatientFileFinanceEditRules.PaymentMode(c));
    }

    [Fact]
    public void ChangingInstallmentDatesInvalidatesTheReviewedFinanceSnapshot()
    {
        var c = new PatientFinancialCase { TotalAmount = 150 };
        var legacy = PatientFileFinanceEditRules.EditHash(c);
        var row = new PatientInstallment { Id = 1, Amount = 150, DueDate = Today.ToDateTime(TimeOnly.MinValue) };
        c.Installments.Add(row);
        var reviewed = PatientFileFinanceEditRules.EditHash(c);
        Assert.NotEqual(legacy, reviewed);
        row.DueDate = row.DueDate.AddMonths(1);
        Assert.NotEqual(reviewed, PatientFileFinanceEditRules.EditHash(c));
        row.IsDeleted = true;
        Assert.Equal(legacy, PatientFileFinanceEditRules.EditHash(c));
    }

    [Fact]
    public void AdminInstallmentFiltersTranslateToSql()
    {
        using var db = new DentalContextDesignTimeFactory().CreateDbContext([]);
        var sql = PatientInstallmentReportRules.Filter(db.PatientInstallments,
            new() { FromDate = Today, ToDate = Today.AddMonths(1), Status = "unpaid" }, Today).ToQueryString();
        Assert.Contains("PaidAtUtc", sql); Assert.Contains("DueDate", sql);
        Assert.Contains(">=", sql); Assert.Contains("<", sql);
    }

    private static PatientInstallment Row(long id, DateTime date) => new() {
        Id = id, Amount = 50, DueDate = date, FinancialCase = new()
    };
}
