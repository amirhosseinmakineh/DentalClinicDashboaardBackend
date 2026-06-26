namespace DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse
{
    public record ConsultantDashboardStatusResponse
    {
        public long ProfileId { get; init; }
        public bool IsAvailable { get; init; }
        public bool IsOnline { get; init; }
        public DateTime? LastOnlineAt { get; init; }
        public DateTime? LastOfflineAt { get; init; }
        public int PendingOfflineLeadCount { get; init; }
        public int CurrentScore { get; init; }
        public bool CanGoOnline { get; init; }
        public string? OnlineStatusBlockReason { get; init; }
    }
}
