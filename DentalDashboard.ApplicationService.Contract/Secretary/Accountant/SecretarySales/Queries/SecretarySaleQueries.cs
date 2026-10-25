using System.Text.Json.Serialization;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Queries;

public sealed class GetSecretarySaleServicesQuery : IQuery<PaginatedResult<SecretarySaleServiceDto>>
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public sealed class GetActiveSecretarySaleServicesQuery : IQuery<IReadOnlyList<SecretarySaleServiceDto>> { }

public sealed class SearchSecretarySalePatientsQuery : IQuery<PaginatedResult<SecretarySalePatientDto>>
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class GetAdminSecretarySalesQuery : IQuery<PaginatedResult<SecretarySaleDto>>
{
    public string? Search { get; set; }
    public Guid? SecretaryUserId { get; set; }
    public Guid? PatientUserId { get; set; }
    public long? ServiceId { get; set; }
    public SecretarySaleStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public sealed class GetSecretarySalesQuery : IQuery<PaginatedResult<SecretarySaleDto>>
{
    [JsonIgnore] public Guid SecretaryUserId { get; set; }
    public string? Search { get; set; }
    public SecretarySaleStatus? Status { get; set; }
    public long? ServiceId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public sealed class GetSecretaryWalletQuery : IQuery<SecretaryWalletDto>
{
    public required Guid SecretaryUserId { get; init; }
}

public sealed class GetSecretaryWalletTransactionsQuery : IQuery<PaginatedResult<SecretaryWalletTransactionDto>>
{
    [JsonIgnore] public Guid SecretaryUserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public SecretaryWalletTransactionType? TransactionType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
