using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Reservation
{
    public class GetConsultantReservationsQueryHandler : IQueryHandler<GetConsultantReservationsQuery, PaginatedResult<ReservationItemResponse>>
    {
        private readonly IReservationRepository reservationRepository;

        public GetConsultantReservationsQueryHandler(IReservationRepository reservationRepository)
        {
            this.reservationRepository = reservationRepository;
        }

        public async Task<PaginatedResult<ReservationItemResponse>> HandleAsync(GetConsultantReservationsQuery query, CancellationToken cancellationToken = default)
        {
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : Math.Min(query.PageSize, 100);
            var now = DateTime.Now;

            var reservations = reservationRepository.GetAll()
                .Where(x => x.ConsultantProfileId == query.ConsultantProfileId);

            if (!query.IncludeCanceled)
                reservations = reservations.Where(x => !x.IsCanceled);

            if (query.From.HasValue)
                reservations = reservations.Where(x => x.ReservationAt >= query.From.Value);

            if (query.To.HasValue)
                reservations = reservations.Where(x => x.ReservationAt <= query.To.Value);

            if (query.AttendanceConfirmationStatus.HasValue)
                reservations = reservations.Where(x => x.AttendanceConfirmationStatus == query.AttendanceConfirmationStatus.Value);

            if (query.OnlySecretaryReviewed)
                reservations = reservations.Where(x => x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryApproved ||
                                                       x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryRejected);

            if (query.OnlyDueForConsultantConfirmation)
                reservations = reservations.Where(x => x.ReservationAt <= now &&
                                                       x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.PendingConsultantConfirmation);

            if (!string.IsNullOrWhiteSpace(query.PatientName))
            {
                var patientName = query.PatientName.Trim();
                reservations = reservations.Where(x => x.LeadAssignment.UserName.Contains(patientName));
            }

            if (!string.IsNullOrWhiteSpace(query.PatientPhoneNumber))
            {
                var patientPhoneNumber = query.PatientPhoneNumber.Trim();
                reservations = reservations.Where(x => x.LeadAssignment.PhoneNumber.Contains(patientPhoneNumber) ||
                                                       (x.LeadAssignment.SecondaryPhoneNumber != null &&
                                                        x.LeadAssignment.SecondaryPhoneNumber.Contains(patientPhoneNumber)));
            }

            var totalCount = await reservations.CountAsync(cancellationToken);
            var items = await reservations
                .OrderByDescending(x => x.ReservationAt)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
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
                    ConsultantAttendanceConfirmedAt = x.ConsultantAttendanceConfirmedAt,
                    ConsultantSaysPatientAttended = x.ConsultantSaysPatientAttended,
                    ConsultantAttendanceNote = x.ConsultantAttendanceNote,
                    SecretaryReviewedAt = x.SecretaryReviewedAt,
                    SecretaryUserId = x.SecretaryUserId,
                    SecretaryApprovedConsultantConfirmation = x.SecretaryApprovedConsultantConfirmation,
                    SecretaryReviewNote = x.SecretaryReviewNote,
                    IsAttendanceScoreApplied = x.IsAttendanceScoreApplied,
                    AttendanceScoreValue = x.AttendanceScoreValue,
                    AttendanceScoreAppliedAt = x.AttendanceScoreAppliedAt,
                    IsDueForConsultantConfirmation = x.ReservationAt <= now &&
                                                       x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.PendingConsultantConfirmation,
                    Description = x.Description,
                    IsCanceled = x.IsCanceled
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<ReservationItemResponse>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
