using System.Text.Json.Serialization;
using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.SecretarySales.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Accounting.Contracts.SecretarySales;

namespace DentalDashboard.Accounting.Contracts.SecretarySales.Queries;

public sealed class GetActiveSecretarySaleServicesQuery : IQuery<IReadOnlyList<SecretarySaleServiceDto>> { }
