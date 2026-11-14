using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Commands;
using DentalDashboard.ApplicationService.Secretary.Accountant.PatientFinance.Handlers;
using DentalDashboard.Controllers;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class PromissoryNoteGuaranteeTests
{
    [Theory]
    [InlineData(300, 150, 150, true)]
    [InlineData(300, 0, 150, false)]
    [InlineData(100, 150, 150, false)]
    [InlineData(300, 150, 100, false)]
    [InlineData(300, 50, 150, true)]
    public void GuaranteeCollectionRequiresExistingDebtAndCannotExceedSecurityOrTreatmentBalance(
        decimal guarantee, decimal debt, decimal remaining, bool expected) =>
        Assert.Equal(expected, PatientGuaranteeCollectionRules.CanCollect(guarantee, debt, remaining));

    private static readonly DateOnly Day = new(2026, 11, 14);
    private static CompletePatientFileRequest Request(decimal amount = 300_000_000) => new(Guid.NewGuid(),
        "installments", 150_000_000, 0, new() { [1] = 1 }, [], [new(amount, "123", Day.AddMonths(1))],
        "۱۰ قسط ماهانه ۱۵ میلیون تومان؛ سفته بابت ضمانت");

    [Fact]
    public void GuaranteeChequeCoversTreatmentWithoutCreatingPayment()
    {
        var request = Request() with { Notes = [], Cheques = [new(300_000_000, "g", "owner", Day, true, IsGuarantee: true)] };
        Assert.Null(PatientCompletionValidation.Validate(request, Day));
        var c = new PatientFinancialCase { TotalAmount = request.TotalAmount, PaymentMethod = "اقساط" };
        var w = new PatientFileWorkflow { FinancialCase = c };
        PatientFileFinanceEditRules.Apply(w, request, Day);
        Assert.True(Assert.Single(c.Cheques).IsGuarantee);
        Assert.True(PatientFinanceCoverage.CanConfirm(c));
        Assert.Null(PatientFileFinanceEditRules.BlockReason(c));
        Assert.Empty(c.Transactions); Assert.Empty(c.Debts);
        c.Cheques.Single().IsGuarantee = false;
        Assert.False(PatientFinanceCoverage.CanConfirm(c));
    }

    [Theory]
    [InlineData(PatientChequeStatus.Paid)]
    [InlineData(PatientChequeStatus.Unpaid)]
    public async Task GuaranteeChequeCannotCreateNominalPaymentOrDebt(PatientChequeStatus status)
    {
        var cheque = new PatientCheque { Id = 7, Amount = 300_000_000, IsGuarantee = true };
        var repo = PatientFileServiceResultTests.RepositoryDouble.Create<IPatientFinanceRepository>(method =>
            method.Name == "get_Cheques"
                ? new PatientFileServiceResultTests.AsyncQuery<PatientCheque>(new[] { cheque }.AsQueryable())
                : throw new InvalidOperationException("Unexpected financial side effect: " + method.Name));
        var uow = PatientFileServiceResultTests.RepositoryDouble.Create<IUnitOfWork>(_ => Task.CompletedTask);
        var result = await new UpdatePatientChequeStatusCommandHandler(repo, uow).HandleAsync(new() {
            ChequeId = 7, Status = status, ActorUserId = Guid.NewGuid()
        });
        Assert.False(result.IsSuccess); Assert.Contains("ضمانت", result.Message);
        Assert.Equal(PatientChequeStatus.Pending, cheque.Status);
    }

    [Fact]
    public void DoubleValueGuaranteeIsAcceptedButOverpaymentAndInsufficientSecurityAreRejected()
    {
        Assert.Null(PatientCompletionValidation.Validate(Request(), Day));
        Assert.NotNull(PatientCompletionValidation.Validate(Request(149_000_000), Day));
        Assert.NotNull(PatientCompletionValidation.Validate(Request() with {
            Cheques = [new(151_000_000, "cheque", "owner", Day, true)]
        }, Day));
        Assert.NotNull(PatientCompletionValidation.Validate(Request() with {
            Notes = [new(300_000_000, "123", Day, IsGuarantee: false)]
        }, Day));
        Assert.Null(PatientCompletionValidation.Validate(Request() with {
            CashReceived = 20_000_000, Cheques = [new(30_000_000, "cheque", "owner", Day, true)]
        }, Day));
    }

    [Fact]
    public void GuaranteesPermitApprovalWithoutIncreasingPaidAmountOrPatientLiability()
    {
        var c = new PatientFinancialCase { TotalAmount = 150_000_000, PaymentMethod = "اقساط" };
        var note = new PatientPromissoryNote { Id = 1, Amount = 300_000_000, IsGuarantee = true };
        c.PromissoryNotes.Add(note);
        var w = new PatientFileWorkflow { FinancialCase = c, ConsultationCompleted = true,
            TreatmentConfirmedAtUtc = DateTime.UtcNow, FinanceConfirmedAtUtc = DateTime.UtcNow,
            ProfileConfirmedAtUtc = DateTime.UtcNow };
        Assert.True(PatientFinanceCoverage.CanConfirm(c)); Assert.True(PatientFileWorkflowRules.CanApprove(w));
        Assert.Equal(150_000_000, c.TotalAmount); Assert.Equal(0, c.PrePaymentAmount);
        Assert.Empty(c.Transactions); Assert.Empty(c.Debts);
        var hash = PatientFileWorkflowRules.FinanceHash(c);
        note.IsGuarantee = false;
        Assert.False(PatientFinanceCoverage.CanConfirm(c)); Assert.NotEqual(hash, PatientFileWorkflowRules.FinanceHash(c));
    }

    [Theory]
    [InlineData(PatientPromissoryNoteStatus.Paid)]
    [InlineData(PatientPromissoryNoteStatus.Unpaid)]
    public async Task GuaranteeCannotBeConvertedIntoANominalPaymentOrDebt(PatientPromissoryNoteStatus status)
    {
        var note = new PatientPromissoryNote { Id = 7, Amount = 300_000_000, IsGuarantee = true };
        var repo = PatientFileServiceResultTests.RepositoryDouble.Create<IPatientFinanceRepository>(method =>
            method.Name == "get_PromissoryNotes"
                ? new PatientFileServiceResultTests.AsyncQuery<PatientPromissoryNote>(new[] { note }.AsQueryable())
                : throw new InvalidOperationException("Guarantees must not create a transaction or debt: " + method.Name));
        var uow = PatientFileServiceResultTests.RepositoryDouble.Create<IUnitOfWork>(_ => Task.CompletedTask);
        var result = await new UpdatePatientPromissoryNoteStatusCommandHandler(repo, uow).HandleAsync(new() {
            PromissoryNoteId = 7, Status = status, ActorUserId = Guid.NewGuid()
        });
        Assert.False(result.IsSuccess); Assert.Contains("ضمانت", result.Message);
        Assert.Equal(PatientPromissoryNoteStatus.Pending, note.Status);
    }

    [Fact]
    public void DescriptionRoundTripsThroughCorrectionAndDetectsConcurrentDescriptionEdits()
    {
        var request = Request();
        var c = new PatientFinancialCase { PatientId = request.PatientId, TotalAmount = 150_000_000 };
        var beforeApprovalHash = PatientFileWorkflowRules.FinanceHash(c);
        var beforeEditHash = PatientFileFinanceEditRules.EditHash(c);
        c.Notes = "changed elsewhere";
        Assert.Equal(beforeApprovalHash, PatientFileWorkflowRules.FinanceHash(c));
        Assert.NotEqual(beforeEditHash, PatientFileFinanceEditRules.EditHash(c));
        var w = new PatientFileWorkflow { FinancialCase = c };
        PatientFileFinanceEditRules.Apply(w, request, Day);
        Assert.Equal(request.InstallmentDescription, c.Notes);
        Assert.True(Assert.Single(c.PromissoryNotes).IsGuarantee);
        Assert.Equal(300_000_000, Assert.Single(c.PromissoryNotes).Amount);
        Assert.NotNull(PatientCompletionValidation.Validate(request with { InstallmentDescription = new string('x', 4001) }, Day));
    }

    [Fact]
    public void ExistingPaymentNotesRetainTheirBehaviorAndDoNotBecomeGuarantees()
    {
        var note = new PatientPromissoryNote { Amount = 150_000_000 };
        Assert.False(note.IsGuarantee);
        var c = new PatientFinancialCase { TotalAmount = 150_000_000, PaymentMethod = "اقساط" };
        c.PromissoryNotes.Add(note); Assert.True(PatientFinanceCoverage.CanConfirm(c));
        note.Amount = 300_000_000; Assert.False(PatientFinanceCoverage.CanConfirm(c));
    }
}
