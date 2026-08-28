using DentalDashboard.ApplicationService.Contract.Requests.FollowUp.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.FollowUp;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.FollowUp;

public sealed class GetSecretaryFollowUpPatientInfoQueryHandler(
    IReservationRepository reservations)
    : IQueryHandler<
        GetSecretaryFollowUpPatientInfoQuery,
        PatientFollowUpInfoResponse?>
{
    public Task<PatientFollowUpInfoResponse?> HandleAsync(
        GetSecretaryFollowUpPatientInfoQuery q,
        CancellationToken ct = default)
    {
        return reservations
            .GetAll()
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                !x.IsCanceled)
            .OrderByDescending(x => x.ReservationAt)
            .Select(x => new PatientFollowUpInfoResponse
            {
                PatientId = x.LeadAssignmentId,
                PatientName = x.LeadAssignment.UserName,
                PhoneNumber = x.LeadAssignment.PhoneNumber,
                ConsultantId = x.ConsultantProfileId,
                ConsultantName =
                    x.ConsultantProfile.User.FirstName +
                    " " +
                    x.ConsultantProfile.User.LastName,
                ReservationId = x.Id,
                ReservationDate = x.ReservationAt.Date,
                ReservationTime = x.ReservationAt.ToString("HH:mm")
            })
            .FirstOrDefaultAsync(ct);
    }
}
