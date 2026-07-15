using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.ApplicationService.Handlers.Helpers;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Reservation
{
    public class GetSecretaryReservationsQueryHandler : IQueryHandler<GetSecretaryReservationsQuery, PaginatedResult<SecretaryReservationItemResponse>>
    {
        private readonly IReservationRepository reservationRepository;

        public GetSecretaryReservationsQueryHandler(IReservationRepository reservationRepository)
        {
            this.reservationRepository = reservationRepository;
        }

        public async Task<PaginatedResult<SecretaryReservationItemResponse>> HandleAsync(GetSecretaryReservationsQuery query, CancellationToken cancellationToken = default)
        {
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : Math.Min(query.PageSize, 100);

            var reservations = reservationRepository.GetAll().AsNoTracking();

            if (!query.IncludeCanceled)
                reservations = reservations.Where(x => !x.IsCanceled);

            if (query.ConsultantProfileId.HasValue)
                reservations = reservations.Where(x => x.ConsultantProfileId == query.ConsultantProfileId.Value);

            reservations = reservations.ApplyReservationAtFilter(query.Date, query.From, query.To);

            if (query.AttendanceConfirmationStatus.HasValue)
                reservations = reservations.Where(x => x.AttendanceConfirmationStatus == query.AttendanceConfirmationStatus.Value);

            if (query.OnlyWaitingForSecretaryReview)
                reservations = reservations.Where(x => x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.ConsultantConfirmedPresent ||
                                                       x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.ConsultantConfirmedAbsent);

            if (query.OnlyConsultantAttendanceConfirmed)
                reservations = reservations.Where(x =>
                    x.ConsultantAttendanceConfirmedAt != null ||
                    x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.ConsultantConfirmedPresent ||
                    x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.ConsultantConfirmedAbsent);

            var totalCount = await reservations.CountAsync(cancellationToken);
            var items = await reservations
                .OrderByDescending(x => x.ReservationAt)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SecretaryReservationItemResponse
                {
                    Id = x.Id,
                    LeadAssignmentId = x.LeadAssignmentId,
                    ConsultantProfileId = x.ConsultantProfileId,
                    ConsultantUserId = x.ConsultantProfile.UserId,
                    ConsultantFullName = x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName,
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
                    IsWaitingForSecretaryReview = x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.ConsultantConfirmedPresent ||
                                                   x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.ConsultantConfirmedAbsent,
                    SecretaryReviewedAt = x.SecretaryReviewedAt,
                    SecretaryUserId = x.SecretaryUserId,
                    SecretaryApprovedConsultantConfirmation = x.SecretaryApprovedConsultantConfirmation,
                    SecretaryReviewNote = x.SecretaryReviewNote,
                    IsAttendanceScoreApplied = x.IsAttendanceScoreApplied,
                    AttendanceScoreValue = x.AttendanceScoreValue,
                    AttendanceScoreAppliedAt = x.AttendanceScoreAppliedAt,
                    Description = x.Description,
                    IsCanceled = x.IsCanceled
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<SecretaryReservationItemResponse>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
