namespace DentalDashboard.Domain.Accounting;

public static class GuaranteeCollectionRules
{
    public static bool CanCollect(decimal guarantee, decimal overdueDebt, decimal remaining) =>
        overdueDebt > 0 && overdueDebt <= guarantee && overdueDebt <= remaining;
}
