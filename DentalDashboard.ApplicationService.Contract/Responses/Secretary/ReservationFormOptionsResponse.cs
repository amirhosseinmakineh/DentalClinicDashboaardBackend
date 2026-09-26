namespace DentalDashboard.ApplicationService.Contract.Responses.Secretary;

public sealed class ReservationFormOptionsResponse
{
    public IReadOnlyCollection<ReservationPatientOptionResponse> Patients { get; init; } = [];
    public IReadOnlyCollection<ReservationConsultantOptionResponse> Consultants { get; init; } = [];
}

public sealed class ReservationPatientOptionResponse
{
    public long LeadAssignmentId { get; init; }
    public long ConsultantProfileId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
}

public sealed class ReservationConsultantOptionResponse
{
    public long ProfileId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
}
