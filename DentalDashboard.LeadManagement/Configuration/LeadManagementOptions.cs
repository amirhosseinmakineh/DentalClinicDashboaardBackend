namespace DentalDashboard.LeadManagement.Configuration;
public sealed class LeadManagementOptions
{
    public int IntervalSeconds { get; set; } = 10;
    public int AdminSheetBatchSize { get; set; } = 200;
}
