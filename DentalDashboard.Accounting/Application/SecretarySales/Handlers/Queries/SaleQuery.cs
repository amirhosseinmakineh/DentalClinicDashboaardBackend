using DentalDashboard.Accounting.Contracts.SecretarySales;
using DentalDashboard.Accounting.Contracts.SecretarySales.Queries;
using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.SecretarySales.Enums;
using DentalDashboard.Accounting.Domain.SecretarySales.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.SecretarySales.Handlers;

internal static class SaleQuery
{
    public static IQueryable<SecretarySaleDto> Apply(
        ISecretarySalesRepository repository,
        string? search,
        Guid? secretaryId,
        Guid? patientId,
        long? serviceId,
        SecretarySaleStatus? status,
        DateTime? from,
        DateTime? to)
    {
        var source = repository.Sales.AsNoTracking();

        if (secretaryId.HasValue)
        {
            source = source.Where(sale => sale.SecretaryUserId == secretaryId.Value);
        }

        if (patientId.HasValue)
        {
            source = source.Where(sale => sale.PatientUserId == patientId.Value);
        }

        if (serviceId.HasValue)
        {
            source = source.Where(sale => sale.ServiceId == serviceId.Value);
        }

        if (status.HasValue)
        {
            source = source.Where(sale => sale.Status == status.Value);
        }

        if (from.HasValue)
        {
            source = source.Where(sale => sale.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            var exclusiveEndDate = to.Value.Date.AddDays(1);
            source = source.Where(sale => sale.CreatedAt < exclusiveEndDate);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();
            source = source.Where(sale =>
                sale.SecretaryUser.FirstName.Contains(searchTerm) ||
                sale.SecretaryUser.LastName.Contains(searchTerm) ||
                sale.PatientUser.FirstName.Contains(searchTerm) ||
                sale.PatientUser.LastName.Contains(searchTerm) ||
                sale.PatientUser.PhoneNumber.Contains(searchTerm) ||
                sale.Service.Title.Contains(searchTerm));
        }

        return source
            .OrderByDescending(sale => sale.CreatedAt)
            .Select(sale => new SecretarySaleDto(
                sale.Id,
                sale.SecretaryUserId,
                sale.SecretaryUser.FirstName + " " + sale.SecretaryUser.LastName,
                sale.PatientUserId,
                sale.PatientUser.FirstName + " " + sale.PatientUser.LastName,
                sale.PatientUser.PhoneNumber,
                sale.ServiceId,
                sale.Service.Title,
                sale.SalePrice,
                sale.SecretaryReward,
                sale.Status,
                sale.CreatedAt,
                sale.ReviewedAt));
    }
}
