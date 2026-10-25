using DentalDashboard.ApplicationService.Contract.Requests.FollowUp.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.FollowUp;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.FollowUp;

public sealed class GetSecretaryFollowUpsQueryHandler(
    IReservationRepository reservations)
    : IQueryHandler<
        GetSecretaryFollowUpsQuery,
        PaginatedResult<SecretaryFollowUpResponse>>
{
    public async Task<PaginatedResult<SecretaryFollowUpResponse>> HandleAsync(
        GetSecretaryFollowUpsQuery q,
        CancellationToken ct = default)
    {
        var page = Math.Max(q.Page, 1);
        var size = Math.Clamp(q.PageSize, 1, 100);

        var query = reservations
            .GetAll()
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.SecretaryAnnouncementUserId == q.SecretaryUserId &&
                x.SecretaryAnnouncementUpdatedAt != null);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();

            query = query.Where(x =>
                x.LeadAssignment.UserName.Contains(s) ||
                x.LeadAssignment.PhoneNumber.Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.SecretaryAnnouncementUpdatedAt)
            .Skip((page - 1) * size)
            .Take(size)
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
                ContactResult = x.SecretaryAnnouncement,
                CreatedAt = x.SecretaryAnnouncementUpdatedAt!.Value
            })
            .ToListAsync(ct);

        return new PaginatedResult<SecretaryFollowUpResponse>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total
        };
    }
}
