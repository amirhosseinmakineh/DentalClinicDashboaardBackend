using DentalDashboard.ApplicationService.Contract.Requests.FollowUp.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.FollowUp;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.FollowUp;

public sealed class SearchSecretaryFollowUpPatientsQueryHandler(
    ILeadAssignmentRepository leads)
    : IQueryHandler<
        SearchSecretaryFollowUpPatientsQuery,
        PaginatedResult<SecretaryPatientSearchResponse>>
{
    public async Task<PaginatedResult<SecretaryPatientSearchResponse>> HandleAsync(
        SearchSecretaryFollowUpPatientsQuery q,
        CancellationToken ct = default)
    {
        var page = Math.Max(q.Page, 1);
        var size = Math.Clamp(q.PageSize, 1, 100);

        var query = leads
            .GetAll()
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();

            query = query.Where(x =>
                x.UserName.Contains(s) ||
                x.PhoneNumber.Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.UserName)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new SecretaryPatientSearchResponse
            {
                PatientId = x.Id,
                PatientName = x.UserName,
                PhoneNumber = x.PhoneNumber
            })
            .ToListAsync(ct);

        return new PaginatedResult<SecretaryPatientSearchResponse>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total
        };
    }
}

public sealed class GetSecretaryFollowUpByIdQueryHandler(
    IReservationRepository reservations)
    : IQueryHandler<
        GetSecretaryFollowUpByIdQuery,
        SecretaryFollowUpResponse?>
{
    public Task<SecretaryFollowUpResponse?> HandleAsync(
        GetSecretaryFollowUpByIdQuery q,
        CancellationToken ct = default)
    {
        return reservations
            .GetAll()
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.Id == q.Id &&
                x.SecretaryAnnouncementUserId == q.SecretaryUserId &&
                x.SecretaryAnnouncementUpdatedAt != null)
            .Select(x => new SecretaryFollowUpResponse
            {
                Id = x.Id,
                PatientId = x.LeadAssignmentId,
                PatientName = x.LeadAssignment.UserName,
                PhoneNumber = x.LeadAssignment.PhoneNumber,
                ConsultantName =
                    x.ConsultantProfile.User.FirstName +
                    " " +
                    x.ConsultantProfile.User.LastName,
                ReservationDate = x.ReservationAt.Date,
                ReservationTime = x.ReservationAt.ToString("HH:mm"),
                Contacted = x.SecretaryFollowUpContacted ?? false,
                ContactResult = x.SecretaryAnnouncement
            })
            .FirstOrDefaultAsync(ct);
    }
}
