using DentalDashboard.Controllers;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class PatientFileWorkflowTests
{
    private static readonly DateOnly Today = new(2026, 11, 13);
    private static CompletePatientFileRequest Cash(decimal received = 1000) =>
        new(Guid.NewGuid(), "cash", 1000, received, new() { [1] = 2 }, [], []);

    [Fact]
    public void CashRequiresFullReceiptAndNoInstallmentDocuments()
    {
        Assert.Null(PatientCompletionValidation.Validate(Cash(), Today));
        Assert.NotNull(PatientCompletionValidation.Validate(Cash(999), Today));
        Assert.NotNull(PatientCompletionValidation.Validate(Cash() with {
            Cheques = [new(100, "123", "صاحب چک", null, false)]
        }, Today));
    }

    [Fact]
    public void MixedChequesAndNotesAreAcceptedButRequireExactCoverageAndDueDates()
    {
        var request = Cash(100) with {
            PaymentMode = "installments",
            Cheques = [new(200, "111", "صاحب چک", null, false), new(300, "222", "صاحب چک", Today.AddMonths(1), true)],
            Notes = [new(400, "333", Today.AddMonths(2))]
        };
        Assert.Null(PatientCompletionValidation.Validate(request, Today));
        Assert.NotNull(PatientCompletionValidation.Validate(request with { TotalAmount = 1001 }, Today));
        Assert.NotNull(PatientCompletionValidation.Validate(request with { Notes = [new(400, "333", null)] }, Today));
        Assert.NotNull(PatientCompletionValidation.Validate(request with {
            Cheques = [new(500, "111", "صاحب چک", Today.AddDays(-1), true)]
        }, Today));
    }

    [Fact]
    public void DuplicateDocumentsAndMalformedServicesCannotBeSaved()
    {
        var duplicate = Cash(0) with {
            PaymentMode = "installments",
            Cheques = [new(500, "123", "صاحب چک", null, false), new(500, "123", "صاحب چک", null, false)]
        };
        Assert.NotNull(PatientCompletionValidation.Validate(duplicate, Today));
        Assert.NotNull(PatientCompletionValidation.Validate(Cash() with { ServiceUnitCounts = new() { [99] = 1 } }, Today));
        Assert.NotNull(PatientCompletionValidation.Validate(Cash() with { ServiceUnitCounts = new() { [1] = 0 } }, Today));
    }

    [Fact]
    public void EmptyConsultationFieldsAreAllowed()
    {
        var treatment = new PatientPaperTreatment(null, null, false, null, null, null);
        Assert.Equal(0, treatment.RemainingAmount);
        var request = new PatientPaperRecordRequest(new(), [], System.Text.Json.JsonSerializer.SerializeToElement(new { }), true);
        Assert.True(request.ConsultationCompleted);
        Assert.Empty(request.Treatments);
    }

    [Fact]
    public void PermissionRequiresConsultationAndFinanceAndIsRevokedByFinancialChanges()
    {
        var c = new PatientFinancialCase { Id = Guid.NewGuid(), TotalAmount = 1000, PrePaymentAmount = 1000 };
        var workflow = new PatientFileWorkflow { ConsultationCompleted = true, FinancialCase = c, FinancialCaseId = c.Id,
            TreatmentConfirmedAtUtc = DateTime.UtcNow, FinanceConfirmedAtUtc = DateTime.UtcNow, ProfileConfirmedAtUtc = DateTime.UtcNow,
            ApprovedAtUtc = DateTime.UtcNow, ApprovedByUserId = Guid.NewGuid() };
        workflow.ApprovedFinanceHash = PatientFileWorkflowRules.FinanceHash(c);
        Assert.True(PatientFileWorkflowRules.IsApproved(workflow));
        c.TotalAmount = 2000;
        Assert.False(PatientFileWorkflowRules.IsApproved(workflow));
        c.TotalAmount = 1000;
        workflow.ConsultationCompleted = false;
        Assert.False(PatientFileWorkflowRules.IsApproved(workflow));
        workflow.ConsultationCompleted = true;
        c.Status = PatientFinancialCaseStatus.Cancelled;
        Assert.False(PatientFileWorkflowRules.IsApproved(workflow));
        Assert.False(PatientFileWorkflowRules.IsApproved(null));
    }

    [Fact]
    public void ServiceReleaseRequiresFullCoverageWithoutAnUnpaidDebt()
    {
        var c = new PatientFinancialCase { TotalAmount = 1000, PrePaymentAmount = 100, PaymentMethod = "اقساط" };
        c.Cheques.Add(new PatientCheque { Amount = 500, Status = PatientChequeStatus.Pending });
        c.PromissoryNotes.Add(new PatientPromissoryNote { Amount = 400, Status = PatientPromissoryNoteStatus.Pending });
        var workflow = new PatientFileWorkflow { ConsultationCompleted = true, FinancialCase = c,
            TreatmentConfirmedAtUtc = DateTime.UtcNow, FinanceConfirmedAtUtc = DateTime.UtcNow, ProfileConfirmedAtUtc = DateTime.UtcNow };
        Assert.True(PatientFileWorkflowRules.CanApprove(workflow));
        c.PaymentMethod = "نقدی";
        Assert.False(PatientFileWorkflowRules.CanApprove(workflow));
        c.PaymentMethod = "اقساط";
        c.Debts.Add(new PatientDebt { Amount = 100, Status = PatientDebtStatus.Unpaid });
        Assert.False(PatientFileWorkflowRules.CanApprove(workflow));
        c.Debts.Clear();
        c.Cheques.First().IsGuarantee = true;
        Assert.True(PatientFileWorkflowRules.CanApprove(workflow));
    }

    [Fact]
    public void WorkflowHasOneRowPerFileAndAUniqueFinancialCaseLink()
    {
        using var db = new DentalContextDesignTimeFactory().CreateDbContext([]);
        var entity = db.Model.FindEntityType(typeof(PatientFileWorkflow))!;
        Assert.Equal(nameof(PatientFileWorkflow.PatientFileId), Assert.Single(entity.FindPrimaryKey()!.Properties).Name);
        Assert.Contains(entity.GetIndexes(), x => x.IsUnique && x.Properties.Any(p => p.Name == "FinancialCaseId"));
        Assert.All(entity.GetForeignKeys(), x => Assert.Equal(DeleteBehavior.Restrict, x.DeleteBehavior));
    }

    [Fact]
    public void MigrationOnlyAddsWorkflowAndNeverRecreatesExistingPaperRecords()
    {
        var migration = new DentalDashboard.Infrastracture.Migrations.AddPatientFileWorkflow();
        var table = Assert.Single(migration.UpOperations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation>());
        Assert.Equal("PatientFileWorkflows", table.Name);
        Assert.DoesNotContain(migration.UpOperations, x => x is Microsoft.EntityFrameworkCore.Migrations.Operations.DropColumnOperation);
        Assert.Equal("PatientFileWorkflows", Assert.Single(migration.DownOperations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.DropTableOperation>()).Name);
    }
}
