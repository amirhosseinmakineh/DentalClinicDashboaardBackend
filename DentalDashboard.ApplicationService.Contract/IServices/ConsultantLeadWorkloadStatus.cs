namespace DentalDashboard.ApplicationService.Contract.IServices;

public sealed record ConsultantLeadWorkloadStatus
{
    public const int MaximumAllowedFollowUps = 10;

    public int PendingReportCount { get; init; }
    public int UncalledWithoutReportCount { get; init; }
    public int FollowUpCount { get; init; }
    public bool HasPendingReport => PendingReportCount > 0;
    public bool HasTooManyFollowUps => FollowUpCount > MaximumAllowedFollowUps;
    public bool BlocksNewLeads => HasPendingReport || HasTooManyFollowUps;

    public string? BlockMessage => BlocksNewLeads
        ? $"شما {PendingReportCount} شماره بدون گزارش و {FollowUpCount} مورد در حالت پیگیری دارید. تا زمان تعیین تکلیف این موارد، شماره جدیدی به شما اختصاص داده نمی‌شود."
        : null;
}
