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
                    PatientName = x.LeadAssignment != null ? x.LeadAssignment.UserName : string.Empty,
                    PatientPhoneNumber = x.LeadAssignment != null ? x.LeadAssignment.PhoneNumber : string.Empty,
                    SecondaryPhoneNumber = x.LeadAssignment != null ? x.LeadAssignment.SecondaryPhoneNumber : null,
                    PatientCity = x.LeadAssignment != null ? (x.LeadAssignment.PatientCity ?? string.Empty) : string.Empty,
                    PatientRegion = x.LeadAssignment != null ? x.LeadAssignment.PatientRegion : null,
                    BusinessName = x.LeadAssignment != null ? x.LeadAssignment.BusinessName : null,
                    AttendanceProbabilityPercent = x.LeadAssignment != null ? x.LeadAssignment.AttendanceProbabilityPercent : null,
                    AttendancePrediction = x.AttendancePrediction,
                    AttendanceConfirmationStatus = x.AttendanceConfirmationStatus,
                    IsDueForConsultantConfirmation = true,
                    Description = x.Description,
                    IsCanceled = x.IsCanceled
                })
                .ToListAsync(cancellationToken);
        }
    }
}
