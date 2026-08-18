namespace DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;

public class SecretaryDashboardSummaryResponse
{
    public int NeedCall { get; set; }
    public int Confirmed { get; set; }
    public int NoAnswer { get; set; }
    public int Cancelled { get; set; }
}
