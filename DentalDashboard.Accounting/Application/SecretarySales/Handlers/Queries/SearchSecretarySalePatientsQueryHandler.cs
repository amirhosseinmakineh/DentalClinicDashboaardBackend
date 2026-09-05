using DentalDashboard.Accounting.Contracts.SecretarySales;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Pagination;
using DentalDashboard.Accounting.Contracts.SecretarySales.Queries;
using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.SecretarySales.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.SecretarySales.Handlers;

public sealed class SearchSecretarySalePatientsQueryHandler(
    ISecretarySalesRepository repository)
    : IQueryHandler<SearchSecretarySalePatientsQuery, PaginatedResult<SecretarySalePatientDto>>
{
    public async Task<PaginatedResult<SecretarySalePatientDto>> HandleAsync(
        SearchSecretarySalePatientsQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = repository.Users
            .AsNoTracking()
            .Where(user =>
                user.IsActive &&
                !user.IsDeleted &&
                user.UserRoles.Any(userRole =>
                    !userRole.IsDeleted &&
                    userRole.Role.RoleName == "Patient"));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = query.Search.Trim();
            source = source.Where(patient =>
                patient.FirstName.Contains(searchTerm) ||
                patient.LastName.Contains(searchTerm) ||
                patient.PhoneNumber.Contains(searchTerm) ||
                (patient.FirstName + " " + patient.LastName).Contains(searchTerm));
        }

        return await source
            .OrderBy(patient => patient.LastName)
            .ThenBy(patient => patient.FirstName)
            .Select(patient => new SecretarySalePatientDto(
                patient.Id,
                patient.FirstName,
                patient.LastName,
                patient.PhoneNumber))
            .ToPaginatedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}
