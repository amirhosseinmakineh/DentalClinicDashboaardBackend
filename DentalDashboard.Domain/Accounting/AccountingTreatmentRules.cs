namespace DentalDashboard.Domain.Accounting;

public static class AccountingTreatmentRules
{
    public static bool CanSetTotal(decimal total, decimal collected, decimal openPaymentCommitments) =>
        total > 0 && collected >= 0 && openPaymentCommitments >= 0 && collected + openPaymentCommitments <= total;
}
