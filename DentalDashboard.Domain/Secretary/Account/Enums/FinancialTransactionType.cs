using System.ComponentModel.DataAnnotations;

namespace DentalDashboard.Domain.Secretary.Account.Enums;

public enum FinancialTransactionType
{
    [Display(Name = "ورودی")]
    Income = 1,
    [Display(Name = "خروجی")]
    Expense = 2
}

public enum PaymentMethod
{
    [Display(Name = "نقدی")]
    Cash = 1,
    [Display(Name = "کارتخوان")]
    Pos = 2,
    [Display(Name = "کارت به کارت")]
    CardToCard = 3,
    [Display(Name = "واریز بانکی")]
    BankTransfer = 4,
    [Display(Name = "سایر")]
    Other = 5
}
