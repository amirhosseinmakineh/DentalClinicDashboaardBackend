using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.ApplicationService.Handlers.Helpers;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Utilities.Convertor;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Reservation
{
    public class GetSecretaryReservationsQueryHandler : IQueryHandler<GetSecretaryReservationsQuery, PaginatedResult<SecretaryReservationItemResponse>>
    {
        private readonly IReservationRepository reservationRepository;
        private readonly IUserRepository userRepository;
        private readonly ISecretaryAccessService secretaryAccessService;

        public GetSecretaryReservationsQueryHandler(
            IReservationRepository reservationRepository,
            IUserRepository userRepository,
            ISecretaryAccessService secretaryAccessService)
        {
            this.reservationRepository = reservationRepository;
            this.userRepository = userRepository;
            this.secretaryAccessService = secretaryAccessService;
        }

        public async Task<PaginatedResult<SecretaryReservationItemResponse>> HandleAsync(
            GetSecretaryReservationsQuery query,
            CancellationToken cancellationToken = default)
        {
            var pageNumber = Math.Max(query.PageNumber, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var access = await secretaryAccessService.GetAccessAsync(query.SecretaryUserId,cancellationToken);

            if (!access.IsSecretary)
            {
                return EmptyResult(pageNumber, pageSize);
            }

            var reservations = reservationRepository.GetAll()
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            // ------------------------------------------------------------
            // Basic filters
            // ------------------------------------------------------------

            if (!query.IncludeCanceled)
            {
                reservations = reservations.Where(x => !x.IsCanceled);
            }

            if (query.ConsultantProfileId.HasValue)
            {
                var consultantProfileId = query.ConsultantProfileId.Value;

                reservations = reservations.Where(x =>
                    x.ConsultantProfileId == consultantProfileId);
            }

            if (query.ReservationType.HasValue)
            {
                var reservationType = query.ReservationType.Value;

                reservations = reservations.Where(x =>
                    x.ReservationType == reservationType);
            }

            // ------------------------------------------------------------
            // Patient search
            // ------------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();

                reservations = reservations.Where(x =>
                    x.LeadAssignment.UserName.Contains(search) ||
                    x.LeadAssignment.PhoneNumber.Contains(search) ||
                    (
                        x.LeadAssignment.SecondaryPhoneNumber != null &&
                        x.LeadAssignment.SecondaryPhoneNumber.Contains(search)
                    ));
            }

            // ------------------------------------------------------------
            // Consultant search
            // ------------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(query.ConsultantName))
            {
                var consultantName = query.ConsultantName.Trim();

                reservations = reservations.Where(x =>
                    x.ConsultantProfile.User.FirstName.Contains(consultantName) ||
                    x.ConsultantProfile.User.LastName.Contains(consultantName) ||
                    (
                        x.ConsultantProfile.User.FirstName + " " +
                        x.ConsultantProfile.User.LastName
                    ).Contains(consultantName));
            }

            // ------------------------------------------------------------
            // CreatedAt date range
            // ------------------------------------------------------------

            if (query.FromDate.HasValue)
            {
                var fromDate = query.FromDate.Value;

                reservations = reservations.Where(x =>
                    x.ReservationAt >= fromDate);
            }

            if (query.ToDate.HasValue)
            {
                var toDate = query.ToDate.Value;

                reservations = reservations.Where(x =>
                    x.ReservationAt <= toDate);
            }

            // ------------------------------------------------------------
            // Secretary announcement status
            // ------------------------------------------------------------

            if (query.SecretaryAnnouncementStatus.HasValue)
            {
                var status = query.SecretaryAnnouncementStatus.Value;

                reservations = reservations.Where(x =>
                    x.SecretaryAnnouncementStatus == status);
            }

            // ------------------------------------------------------------
            // Attendance status
            // ------------------------------------------------------------

            if (query.AttendanceStatus.HasValue)
            {
                var status = query.AttendanceStatus.Value;

                reservations = reservations.Where(x =>
                    x.AttendanceConfirmationStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(query.ReservationStatus))
            {
                reservations = query.ReservationStatus.Trim().ToLowerInvariant() switch
                {
                    "pending" => reservations.Where(x =>
                        !x.IsCanceled &&
                        x.SecretaryReviewedAt == null),

                    "confirmed" => reservations.Where(x =>
                        !x.IsCanceled &&
                        x.SecretaryReviewedAt != null &&
                        x.SecretaryApprovedConsultantConfirmation == true),

                    "cancelled" => reservations.Where(x =>
                        x.IsCanceled),

                    _ => reservations
                };
            }

            // ------------------------------------------------------------
            // Count BEFORE paging
            // ------------------------------------------------------------

            var totalCount = await reservations.CountAsync(cancellationToken);

            if (totalCount == 0)
            {
                return EmptyResult(pageNumber, pageSize);
            }

            // ------------------------------------------------------------
            // Sorting
            // ------------------------------------------------------------

            reservations = query.SortDirection?.ToLowerInvariant() switch
            {
                "desc" => reservations
                    .OrderByDescending(x => x.ReservationAt)
                    .ThenByDescending(x => x.Id),

                _ => reservations
                    .OrderBy(x => x.ReservationAt)
                    .ThenBy(x => x.Id)
            };

            // ------------------------------------------------------------
            // Paging + Projection
            // ------------------------------------------------------------

            var items = await reservations
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SecretaryReservationItemResponse
                {
                    Id = x.Id,
                    ReservationId = x.Id,

                    LeadAssignmentId = x.LeadAssignmentId,

                    ConsultantProfileId = x.ConsultantProfileId,
                    ConsultantUserId = x.ConsultantProfile.UserId,

                    ConsultantFullName =
                        x.ConsultantProfile.User.FirstName + " " +
                        x.ConsultantProfile.User.LastName,

                    PatientUserId = x.PatientUserId,
                    RequiresPatientProfile = !x.PatientUserId.HasValue,

                    ReservationAt = x.ReservationAt,
                    AppointmentDateTime = x.ReservationAt,
                    CreatedAt = x.CreatedAt,

                    ReservationType = x.ReservationType,

                    PatientReceivedService = x.PatientReceivedService,

                    PatientName = x.LeadAssignment.UserName,
                    PatientPhoneNumber = x.LeadAssignment.PhoneNumber,
                    SecondaryPhoneNumber =
                        x.LeadAssignment.SecondaryPhoneNumber,

                    PatientCity =
                        x.LeadAssignment.PatientCity ?? string.Empty,

                    PatientRegion =
                        x.LeadAssignment.PatientRegion,

                    BusinessName =
                        x.LeadAssignment.BusinessName,

                    AttendanceProbabilityPercent =
                        x.LeadAssignment.AttendanceProbabilityPercent,

                    AttendanceConfirmationStatus =
                        x.AttendanceConfirmationStatus,

                    ConsultantAttendanceConfirmedAt =
                        x.ConsultantAttendanceConfirmedAt,

                    ConsultantSaysPatientAttended =
                        x.ConsultantSaysPatientAttended,

                    ConsultantAttendanceNote =
                        x.ConsultantAttendanceNote,

                    IsWaitingForSecretaryReview =
                        x.SecretaryReviewedAt == null &&
                        (
                            x.AttendanceConfirmationStatus ==
                                ReservationAttendanceConfirmationStatus
                                    .ConsultantConfirmedPresent
                            ||
                            x.AttendanceConfirmationStatus ==
                                ReservationAttendanceConfirmationStatus
                                    .ConsultantConfirmedAbsent
                        ),

                    SecretaryReviewedAt =
                        x.SecretaryReviewedAt,

                    SecretaryUserId =
                        x.SecretaryUserId,

                    SecretaryApprovedConsultantConfirmation =
                        x.SecretaryApprovedConsultantConfirmation,

                    SecretaryReviewNote =
                        x.SecretaryReviewNote,

                    SecretaryAnnouncementStatus =
                        x.SecretaryAnnouncementStatus,

                    SecretaryAnnouncement =
                        x.SecretaryAnnouncement,

                    SecretaryAnnouncementUpdatedAt =
                        x.SecretaryAnnouncementUpdatedAt,

                    SecretaryAnnouncementUserId =
                        x.SecretaryAnnouncementUserId,

                    IsAttendanceScoreApplied =
                        x.IsAttendanceScoreApplied,

                    AttendanceScoreValue =
                        x.AttendanceScoreValue,

                    AttendanceScoreAppliedAt =
                        x.AttendanceScoreAppliedAt,

                    Description =
                        x.Description,

                    IsCanceled =
                        x.IsCanceled,

                    DentalServices =
                        x.DentalServices
                })
                .ToListAsync(cancellationToken);

            // ------------------------------------------------------------
            // Non-database formatting
            // ------------------------------------------------------------

            foreach (var item in items)
            {
                item.ReservationAtPersian =
                    item.ReservationAt.ToPersianDateTimeString();

                item.CreatedAtPersian =
                    item.CreatedAt.ToPersianDateTimeString();

                item.SecretaryReviewedAtPersian =
                    item.SecretaryReviewedAt?.ToPersianDateTimeString();
            }

            return new PaginatedResult<SecretaryReservationItemResponse>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        private static PaginatedResult<SecretaryReservationItemResponse> EmptyResult(
            int pageNumber,
            int pageSize)
        {
            return new PaginatedResult<SecretaryReservationItemResponse>
            {
                Items = [],
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
