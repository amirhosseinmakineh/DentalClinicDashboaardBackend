namespace DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;

public enum SecretarySaleStatus
{
    PendingAdminApproval = 1,
    Approved = 2,
    Rejected = 3
}

public enum SecretaryWalletTransactionType
{
    SaleReward = 1,
    Withdrawal = 2,
    ManualCredit = 3,
    ManualDebit = 4
}
