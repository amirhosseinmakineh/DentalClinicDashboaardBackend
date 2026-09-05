using System.ComponentModel.DataAnnotations;

namespace DentalDashboard.Accounting.Domain.Enums;

public enum FinancialTransactionType
{
    [Display(Name = "ورودی")]
    Income = 1,
    [Display(Name = "خروجی")]
    Expense = 2
}
