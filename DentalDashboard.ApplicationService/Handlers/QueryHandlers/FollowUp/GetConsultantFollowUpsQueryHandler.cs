using DentalDashboard.ApplicationService.Contract.Requests.FollowUp.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.FollowUp;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.FollowUp;

public sealed class GetConsultantFollowUpsQueryHandler(
    IReservationRepository reservations,
    IUserRepository users)
    : IQueryHandler<
        GetConsultantFollowUpsQuery,
        PaginatedResult<ConsultantFollowUpResponse>>
{
    public async Task<PaginatedResult<ConsultantFollowUpResponse>> HandleAsync(
        GetConsultantFollowUpsQuery q,
        CancellationToken ct = default)
    {
        var page = Math.Max(q.Page, 1);
        var size = Math.Clamp(q.PageSize, 1, 100);

        var query = reservations
            .GetAll()
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.ConsultantProfileId == q.ConsultantProfileId &&
                x.SecretaryAnnouncementUserId != null &&
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
            .Select(x => new ConsultantFollowUpResponse
            {
                Id = x.Id,
                PatientId = x.LeadAssignmentId,
                PatientName = x.LeadAssignment.UserName,
                PhoneNumber = x.LeadAssignment.PhoneNumber,
                ReservationDate = x.ReservationAt.Date,
                ReservationTime = x.ReservationAt.ToString("HH:mm"),
                SecretaryName = users
                    .GetAll()
                    .Where(u => u.Id == x.SecretaryAnnouncementUserId)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault() ?? string.Empty,
                Contacted = x.SecretaryFollowUpContacted ?? false,
                ContactResult = x.SecretaryAnnouncement,
                CreatedAt = x.SecretaryAnnouncementUpdatedAt!.Value
            })
            .ToListAsync(ct);

        return new PaginatedResult<ConsultantFollowUpResponse>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total
        };
    }
}