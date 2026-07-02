using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Reservation
{
    public class GetDueReservationConfirmationsQueryHandler : IQueryHandler<GetDueReservationConfirmationsQuery, List<ReservationItemResponse>>
    {
        private readonly IReservationRepository reservationRepository;

        public GetDueReservationConfirmationsQueryHandler(IReservationRepository reservationRepository)
        {
            this.reservationRepository = reservationRepository;
        }

        public async Task<List<ReservationItemResponse>> HandleAsync(GetDueReservationConfirmationsQuery query, CancellationToken cancellationToken = default)
        {
            var now = query.Now ?? DateTime.Now;

            return await reservationRepository.GetAll()
                .Include(x => x.LeadAssignment)
                .Where(x => x.ConsultantProfileId == query.ConsultantProfileId &&
                            !x.IsCanceled &&
                            x.ReservationAt <= now &&
                            x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.PendingConsultantConfirmation)
                .OrderBy(x => x.ReservationAt)
                .Select(x => new ReservationItemResponse
                {
                    Id = x.Id,
                    LeadAssignmentId = x.LeadAssignmentId,
                    ConsultantProfileId = x.ConsultantProfileId,
                    PatientUserId = x.PatientUserId,
                    RequiresPatientProfile = !x.PatientUserId.HasValue,
                    ReservationAt = x.ReservationAt,
                    PatientName = x.LeadAssignment.UserName,
                    PatientPhoneNumber = x.LeadAssignment.PhoneNumber,
                    SecondaryPhoneNumber = x.LeadAssignment.SecondaryPhoneNumber,
                    PatientCity = x.LeadAssignment.PatientCity ?? string.Empty,
                    PatientRegion = x.LeadAssignment.PatientRegion,
                    BusinessName = x.LeadAssignment.BusinessName,
                    AttendanceProbabilityPercent = x.LeadAssignment.AttendanceProbabilityPercent,
                    AttendanceConfirmationStatus = x.AttendanceConfirmationStatus,
                    IsDueForConsultantConfirmation = true,
                    Description = x.Description,
                    IsCanceled = x.IsCanceled
                })
                .ToListAsync(cancellationToken);
        }
    }
}
