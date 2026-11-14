using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;

namespace DentalDashboard.Domain.Models;

public static class PatientFinanceCoverage
{
    public static bool IsCovered(decimal total, decimal paid, decimal paymentDocuments, decimal guarantees) =>
        total > 0 && paid >= 0 && paymentDocuments >= 0 && guarantees >= 0 &&
        paid + paymentDocuments <= total && paid + paymentDocuments + guarantees >= total;

    public static bool CanConfirm(PatientFinancialCase c)
    {
        var paid = c.PrePaymentAmount + c.DepositAmount + c.Transactions.Where(x => !x.IsDeleted).Sum(x => x.Amount);
        var documents = c.Cheques.Where(x => !x.IsDeleted && !x.IsGuarantee && x.Status == PatientChequeStatus.Pending).Sum(x => x.Amount) +
            c.PromissoryNotes.Where(x => !x.IsDeleted && !x.IsGuarantee && x.Status == PatientPromissoryNoteStatus.Pending).Sum(x => x.Amount);
        var guarantees = c.Cheques.Where(x => !x.IsDeleted && x.IsGuarantee && x.Status == PatientChequeStatus.Pending).Sum(x => x.Amount) + c.PromissoryNotes.Where(x => !x.IsDeleted && x.IsGuarantee && x.Status == PatientPromissoryNoteStatus.Pending).Sum(x => x.Amount);
        return (c.PaymentMethod != "نقدی" || paid == c.TotalAmount) && IsCovered(c.TotalAmount, paid, documents, guarantees) &&
            !c.Debts.Any(x => !x.IsDeleted && x.Status == PatientDebtStatus.Unpaid);
    }
}
