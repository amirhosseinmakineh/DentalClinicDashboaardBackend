using DentalDashboard.ApplicationService.Contract.Responses.SecretaryResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using System.Text.Json.Serialization;

namespace DentalDashboard.ApplicationService.Contract.Requests.Secretary.Queries;

public class GetSecretaryDashboardSummaryQuery : IQuery<SecretaryDashboardSummaryResponse>
{
    [JsonIgnore]
    public Guid SecretaryUserId { get; set; }
}
