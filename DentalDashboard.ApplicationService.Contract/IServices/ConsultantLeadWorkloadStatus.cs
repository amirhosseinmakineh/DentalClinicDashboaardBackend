namespace DentalDashboard.ApplicationService.Contract.IServices;

public sealed record ConsultantLeadWorkloadStatus
{
    public const int MaximumAllowedFollowUps = 10;

    public int UncalledWithoutReportCount { get; init; }
    public int FollowUpCount { get; init; }
    public bool HasUncalledWithoutReport => UncalledWithoutReportCount > 0;
    public bool HasTooManyFollowUps => FollowUpCount > MaximumAllowedFollowUps;
    public bool BlocksNewLeads => HasUncalledWithoutReport || HasTooManyFollowUps;

    public string? BlockMessage => BlocksNewLeads
        ? $"شما {UncalledWithoutReportCount} شماره بدون گزارش و {FollowUpCount} مورد در حالت پیگیری دارید. تا زمان تعیین تکلیف این موارد، شماره جدیدی به شما اختصاص داده نمی‌شود."
        : null;
}
