namespace DentalDashboard.ApplicationService.Contract.Responses.LeadResponse
{
    public record RecordLeadCallInitiatedResponse
    {
        public long LeadAssignmentId { get; init; }
        public long ConsultantProfileId { get; init; }
        public DateTime CallInitiatedAt { get; init; }
    }
}
