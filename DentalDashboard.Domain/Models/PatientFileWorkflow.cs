using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;

namespace DentalDashboard.Domain.Models;

/// <summary>Explicit consultation, finance receipt and service-release milestones.</summary>
public sealed class PatientFileWorkflow
{
    public long PatientFileId { get; set; }
    public PatientFile PatientFile { get; set; } = default!;
    public bool ConsultationCompleted { get; set; }
    public Guid? FinancialCaseId { get; set; }
    public PatientFinancialCase? FinancialCase { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedFinanceHash { get; set; }

    public void ClearApproval()
    {
        ApprovedAtUtc = null;
        ApprovedByUserId = null;
        ApprovedFinanceHash = null;
    }
}

public static class PatientFileWorkflowRules
{
    public static bool CanApprove(PatientFileWorkflow? workflow)
    {
        if (workflow?.ConsultationCompleted != true || !HasFinance(workflow)) return false;
        var c = workflow.FinancialCase!;
        var paid = c.PrePaymentAmount + c.DepositAmount + c.Transactions.Where(x => !x.IsDeleted).Sum(x => x.Amount);
        var pending = c.Cheques.Where(x => !x.IsDeleted && !x.IsGuarantee && x.Status == PatientChequeStatus.Pending).Sum(x => x.Amount) +
            c.PromissoryNotes.Where(x => !x.IsDeleted && x.Status == PatientPromissoryNoteStatus.Pending).Sum(x => x.Amount);
        return c.TotalAmount > 0 && (c.PaymentMethod != "نقدی" || paid == c.TotalAmount) &&
            paid + pending == c.TotalAmount && !c.Debts.Any(x => !x.IsDeleted && x.Status == PatientDebtStatus.Unpaid);
    }

    public static bool HasFinance(PatientFileWorkflow? workflow) =>
        workflow?.FinancialCase is { Status: not PatientFinancialCaseStatus.Cancelled, IsDeleted: false };

    public static bool IsApproved(PatientFileWorkflow? workflow) =>
        workflow is { ConsultationCompleted: true, ApprovedAtUtc: not null } &&
        CanApprove(workflow) && workflow.ApprovedFinanceHash == FinanceHash(workflow.FinancialCase!);

    // A review applies to the exact financial data reviewed, including later changes
    // made through the existing accounting screen. No stale release is displayed.
    public static string FinanceHash(PatientFinancialCase c)
    {
        var json = JsonSerializer.Serialize(new {
            c.Id, c.TotalAmount, c.PrePaymentAmount, c.DepositAmount, c.PaymentMethod,
            c.AgreementType, c.Status, c.ServiceUnitCountsJson,
            Services = c.Services.OrderBy(x => x),
            Cheques = c.Cheques.Where(x => !x.IsDeleted).OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Amount, x.SayadNumber, x.OwnerName, x.DueDate, x.Status, x.IsGuarantee }),
            Notes = c.PromissoryNotes.Where(x => !x.IsDeleted).OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Amount, x.SerialNumber, x.DueDate, x.Status }),
            Debts = c.Debts.Where(x => !x.IsDeleted).OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Amount, x.Status }),
            Transactions = c.Transactions.Where(x => !x.IsDeleted).OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Amount, x.SourceType, x.SourceId })
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}
