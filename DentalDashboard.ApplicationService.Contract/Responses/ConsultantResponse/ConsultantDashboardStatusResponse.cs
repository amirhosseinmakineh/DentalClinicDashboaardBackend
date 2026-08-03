namespace DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse
{
    public record ConsultantDashboardStatusResponse
    {
        public long ProfileId { get; init; }
        public bool IsAvailable { get; init; }
        public bool IsOnline { get; init; }
        public DateTime? LastOnlineAt { get; init; }
        public DateTime? LastOfflineAt { get; init; }
        public bool CanGoOnline { get; init; }
        public string? OnlineStatusBlockReason { get; init; }
        public int TodayReservationsCount { get; init; }
        public int UncalledWithoutReportCount { get; init; }
        public int PendingReportCount { get; init; }
        public int FollowUpCount { get; init; }
        public int MaximumAllowedFollowUps { get; init; }
        public bool IsNewLeadBlocked { get; init; }
        public bool ShouldShowWorkloadNotification { get; init; }
        public string? WorkloadNotificationMessage { get; init; }
    }
}
