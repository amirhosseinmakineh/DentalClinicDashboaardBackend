namespace DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;

public sealed class PatientInstallment
{
    public long Id { get; set; }
    public Guid PatientFinancialCaseId { get; set; }
    public PatientFinancialCase FinancialCase { get; set; } = default!;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public Guid? PaidByUserId { get; set; }
    public bool IsDeleted { get; set; }
}
