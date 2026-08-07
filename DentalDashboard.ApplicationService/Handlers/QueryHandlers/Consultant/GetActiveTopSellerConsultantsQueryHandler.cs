using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Consultant;

public sealed class GetActiveTopSellerConsultantsQueryHandler :
    IQueryHandler<GetActiveTopSellerConsultantsQuery, IReadOnlyList<ActiveTopSellerConsultantResponse>>
{
    private readonly IConsultantProfileRepository consultants;
    private readonly ILeadAssignmentRepository leads;
    private readonly IReservationRepository reservations;

    public GetActiveTopSellerConsultantsQueryHandler(IConsultantProfileRepository consultants,
        ILeadAssignmentRepository leads, IReservationRepository reservations)
    {
        this.consultants = consultants; this.leads = leads; this.reservations = reservations;
    }

    public async Task<IReadOnlyList<ActiveTopSellerConsultantResponse>> HandleAsync(
        GetActiveTopSellerConsultantsQuery query, CancellationToken cancellationToken = default)
    {
        var result = new List<ActiveTopSellerConsultantResponse>();
        foreach (var consultant in await consultants.GetActiveTopSellerConsultantsAsync())
        {
            var assigned = await leads.GetTodayAssignmentCountAsync(consultant.Id, burned: false, cancellationToken);
            var startUtc = consultant.TopSellerStartedAt!.Value;
            var periodEnd = startUtc.AddDays(7);
            var successful = await reservations.GetAll().AsNoTracking()
                .Where(x => !x.IsDeleted && !x.IsCanceled && x.ConsultantProfileId == consultant.Id &&
                            x.LeadAssignment.AssignedAt >= startUtc && x.LeadAssignment.AssignedAt < periodEnd &&
                            x.ConsultantSaysPatientAttended == true &&
                            x.SecretaryApprovedConsultantConfirmation == true &&
                            x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryApproved)
                .Select(x => x.LeadAssignmentId).Distinct().CountAsync(cancellationToken);
            result.Add(new(consultant.Id, startUtc, IranTimeHelper.ToIranLocalTime(startUtc),
                assigned, successful, consultant.User.IsActive, consultant.IsAvailable,
                consultant.IsOnline, consultant.TopSellerRewardLevel));
        }
        return result;
    }
}
