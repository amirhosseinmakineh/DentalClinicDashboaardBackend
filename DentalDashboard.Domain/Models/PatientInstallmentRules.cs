using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;

namespace DentalDashboard.Domain.Models;

public static class PatientInstallmentRules
{
    public static string? PaymentError(PatientFinancialCase c, PatientInstallment row, decimal collected)
    {
        if (c.IsDeleted || c.Status == PatientFinancialCaseStatus.Cancelled || row.IsDeleted || row.PatientFinancialCaseId != c.Id)
            return "پرونده یا قسط فعال نیست.";
        if (row.PaidAtUtc.HasValue) return "این قسط قبلاً پرداخت شده است.";
        if (row.Amount <= 0 || collected < 0 || c.PrePaymentAmount + c.DepositAmount + collected + row.Amount > c.TotalAmount)
            return "ثبت این قسط از مانده واقعی پرونده بیشتر است.";
        return null;
    }
}
