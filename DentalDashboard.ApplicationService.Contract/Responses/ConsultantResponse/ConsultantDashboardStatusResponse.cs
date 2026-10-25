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
        public int TotalReservationsCount { get; init; }
        public int TotalReservedPatientsCount { get; init; }
        public int TodayCallsCount { get; init; }
        public int DailyLimit { get; init; }
        public int TodayPickupCount { get; init; }
        public int RemainingDailyCapacity { get; init; }
        public int PendingReportCount { get; init; }
        public int UncalledWithoutReportCount { get; init; }
        public bool IsNewLeadBlocked { get; init; }
        public bool ShouldShowWorkloadNotification { get; init; }
        public string? WorkloadNotificationMessage { get; init; }
    }
}
