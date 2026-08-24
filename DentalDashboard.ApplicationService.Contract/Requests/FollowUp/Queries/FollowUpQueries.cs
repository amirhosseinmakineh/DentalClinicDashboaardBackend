using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.FollowUp;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using System.Text.Json.Serialization;

namespace DentalDashboard.ApplicationService.Contract.Requests.FollowUp.Queries;

public sealed class SearchSecretaryFollowUpPatientsQuery
    : IQuery<PaginatedResult<SecretaryPatientSearchResponse>>
{
    public string? Search { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public sealed class GetSecretaryFollowUpPatientInfoQuery
    : IQuery<PatientFollowUpInfoResponse?>
{
    public long PatientId { get; set; }
}

public sealed class GetSecretaryFollowUpsQuery
    : IQuery<PaginatedResult<SecretaryFollowUpResponse>>
{
    public string? Search { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    [JsonIgnore]
    public Guid SecretaryUserId { get; set; }
}

public sealed class GetSecretaryFollowUpByIdQuery
    : IQuery<SecretaryFollowUpResponse?>
{
    public long Id { get; set; }

    [JsonIgnore]
    public Guid SecretaryUserId { get; set; }
}

public sealed class GetConsultantFollowUpsQuery
    : IQuery<PaginatedResult<ConsultantFollowUpResponse>>
{
    public string? Search { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    [JsonIgnore]
    public long ConsultantProfileId { get; set; }
}