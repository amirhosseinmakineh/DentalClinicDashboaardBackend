using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.IRepositories;
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
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            var now = DateTime.Now;

            var reservations = reservationRepository.GetAll()
                .Where(x => x.ConsultantProfileId == query.ConsultantProfileId);

            if (!query.IncludeCanceled)
                reservations = reservations.Where(x => !x.IsCanceled);

            if (query.From.HasValue)
                reservations = reservations.Where(x => x.ReservationAt >= query.From.Value);

            if (query.To.HasValue)
                reservations = reservations.Where(x => x.ReservationAt <= query.To.Value);

            if (query.OnlySecretaryReviewed == true)
            {
                reservations = reservations.Where(x =>
                    x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryApproved ||
                    x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryRejected);
            }

            var totalCount = await reservations.CountAsync(cancellationToken);
            var items = await reservations
                .OrderBy(x => x.ReservationAt)
                .ThenBy(x => x.Id)
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
                    PatientName = x.LeadAssignment != null ? x.LeadAssignment.UserName : string.Empty,
                    PatientPhoneNumber = x.LeadAssignment != null ? x.LeadAssignment.PhoneNumber : string.Empty,
                    SecondaryPhoneNumber = x.LeadAssignment != null ? x.LeadAssignment.SecondaryPhoneNumber : null,
                    PatientCity = x.LeadAssignment != null ? (x.LeadAssignment.PatientCity ?? string.Empty) : string.Empty,
                    PatientRegion = x.LeadAssignment != null ? x.LeadAssignment.PatientRegion : null,
                    BusinessName = x.LeadAssignment != null ? x.LeadAssignment.BusinessName : null,
                    AttendanceProbabilityPercent = x.LeadAssignment != null ? x.LeadAssignment.AttendanceProbabilityPercent : null,
                    AttendancePrediction = x.AttendancePrediction,
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
