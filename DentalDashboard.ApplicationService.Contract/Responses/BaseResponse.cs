namespace DentalDashboard.ApplicationService.Contract.Responses
{
    public record BaseResponse<TKey> where TKey : struct
    {
        public TKey Id { get; set; }
    }
}
