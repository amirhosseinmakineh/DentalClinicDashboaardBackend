using System.Text.Json.Serialization;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Queries;

public sealed class GetActiveSecretarySaleServicesQuery : IQuery<IReadOnlyList<SecretarySaleServiceDto>> { }
