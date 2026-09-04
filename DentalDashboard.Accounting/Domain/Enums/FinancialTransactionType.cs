using System.ComponentModel.DataAnnotations;

namespace DentalDashboard.Domain.Secretary.Accountant.Enums;

public enum FinancialTransactionType
{
    [Display(Name = "ورودی")]
    Income = 1,
    [Display(Name = "خروجی")]
    Expense = 2
}
