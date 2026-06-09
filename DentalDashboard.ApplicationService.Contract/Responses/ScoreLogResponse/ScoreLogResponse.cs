namespace DentalDashboard.ApplicationService.Contract.Responses.ScoreLogResponse
{
    public record ScoreLogResponse : BaseResponse<long>
    {
        public ScoreType ScoreType { get; set; }

        public int ScoreValue { get; set; }

        public string? Description { get; set; }
    }
}
