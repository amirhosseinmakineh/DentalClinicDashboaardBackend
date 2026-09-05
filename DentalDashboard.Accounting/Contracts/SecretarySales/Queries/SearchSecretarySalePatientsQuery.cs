using System.Text.Json.Serialization;
using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.SecretarySales.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Accounting.Contracts.SecretarySales;

namespace DentalDashboard.Accounting.Contracts.SecretarySales.Queries;

public sealed class SearchSecretarySalePatientsQuery : IQuery<PaginatedResult<SecretarySalePatientDto>>
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
