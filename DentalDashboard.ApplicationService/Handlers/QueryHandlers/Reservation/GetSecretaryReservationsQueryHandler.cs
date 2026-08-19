using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.ApplicationService.Handlers.Helpers;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;
using DentalDashboard.ApplicationService.Contract.IServices;

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

        public async Task<PaginatedResult<SecretaryReservationItemResponse>> HandleAsync(GetSecretaryReservationsQuery query, CancellationToken cancellationToken = default)
        {
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : Math.Min(query.PageSize, 100);

            var reservations = reservationRepository.GetAll()
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            var access = await secretaryAccessService.GetAccessAsync(query.SecretaryUserId, cancellationToken);
            if (!access.IsSecretary)
                reservations = reservations.Where(_ => false);
            else if (!access.HasFullAccess)
            {
                var allowedDays = access.AllowedDays
                    .Select(x => (int)x)
                    .ToList();

                var reservationIds = await reservations
                    .Select(x => new
                    {
                        x.Id,
                        Day = x.ReservationAt.DayOfWeek
                    })
                    .ToListAsync(cancellationToken);

                var allowedReservationIds = reservationIds
                    .Where(x => allowedDays.Contains((int)x.Day))
                    .Select(x => x.Id)
                    .ToList();

                reservations = reservations.Where(x =>
                    allowedReservationIds.Contains(x.Id));
            }
            var consultantProfileId = query.ConsultantProfileId ?? query.ConsultantId;
            if (consultantProfileId.HasValue)
                reservations = reservations.Where(x => x.ConsultantProfileId == consultantProfileId.Value);

            if (!query.IncludeCanceled)
                reservations = reservations.Where(x => !x.IsCanceled);

            reservations = query.SortDirection?.ToLower() switch
            {
                "asc" => reservations.OrderBy(x => x.ReservationAt),
                "desc" => reservations.OrderByDescending(x => x.ReservationAt),
                _ => reservations.OrderBy(x => x.ReservationAt)
            };

            if (!string.IsNullOrEmpty(query.ConsultantName))
            {
                reservations = reservations
                    .Where(x =>
                        x.ConsultantProfile.User.FirstName.Contains(query.ConsultantName) ||
                        x.ConsultantProfile.User.LastName.Contains(query.ConsultantName) ||
                        (x.ConsultantProfile.User.FirstName + " " +
                         x.ConsultantProfile.User.LastName)
                            .Contains(query.ConsultantName));
            }
            var requestedSearch = !string.IsNullOrWhiteSpace(query.Search)
                ? query.Search
                : query.SearchText;
            if (!string.IsNullOrWhiteSpace(requestedSearch))
            {
                var searchText = requestedSearch.Trim();

                reservations = reservations
                    .Where(x =>
                        x.LeadAssignment.UserName.Contains(searchText) ||
                        x.LeadAssignment.PhoneNumber.Contains(searchText) ||
                        (x.LeadAssignment.SecondaryPhoneNumber != null &&
                         x.LeadAssignment.SecondaryPhoneNumber.Contains(searchText)));
            }

            reservations = reservations.ApplyReservationAtFilter(
                query.Date,
                query.FromDate ?? query.From,
                query.ToDate ?? query.To);

            if (query.SecretaryAnnouncementStatus.HasValue)
            {
                reservations = reservations.Where(x =>
                    x.SecretaryAnnouncementStatus == query.SecretaryAnnouncementStatus.Value);
            }

            var reservationStatus = query.ReservationStatus ?? query.AttendanceConfirmationStatus;
            if (reservationStatus.HasValue)
            {
                reservations = reservations.Where(x =>
                    x.AttendanceConfirmationStatus ==
                    reservationStatus.Value);
            }

            if (query.OnlyWaitingForSecretaryReview)
            {
                reservations = reservations.Where(x =>
                    x.SecretaryReviewedAt == null &&
                    (
                        x.AttendanceConfirmationStatus ==
                            ReservationAttendanceConfirmationStatus.ConsultantConfirmedPresent
                        ||
                        x.AttendanceConfirmationStatus ==
                            ReservationAttendanceConfirmationStatus.ConsultantConfirmedAbsent
                    ));
            }

            if (query.OnlyConsultantAttendanceConfirmed)
            {
                reservations = reservations.Where(x =>
                    x.ConsultantAttendanceConfirmedAt != null ||
                    x.AttendanceConfirmationStatus ==
                        ReservationAttendanceConfirmationStatus.ConsultantConfirmedPresent ||
                    x.AttendanceConfirmationStatus ==
                        ReservationAttendanceConfirmationStatus.ConsultantConfirmedAbsent);
            }

            var totalCount = await reservations.CountAsync(cancellationToken);
            var pagedReservations = reservations
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
            var users = userRepository.GetAll().AsNoTracking();

            var items = await (
                from x in pagedReservations
                join secretaryUser in users
                    on x.SecretaryAnnouncementUserId equals (Guid?)secretaryUser.Id into secretaryUsers
                from secretaryUser in secretaryUsers.DefaultIfEmpty()
                select new SecretaryReservationItemResponse
                {
                    Id = x.Id,
                    ReservationId = x.Id,
                    LeadAssignmentId = x.LeadAssignmentId,
                    ConsultantProfileId = x.ConsultantProfileId,
                    ConsultantUserId = x.ConsultantProfile.UserId,
                    ConsultantFullName = x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName,
                    PatientUserId = x.PatientUserId,
                    RequiresPatientProfile = !x.PatientUserId.HasValue,
                    ReservationAt = x.ReservationAt,
                    AppointmentDateTime = x.ReservationAt,
                    CreatedAt = x.CreatedAt,
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
                    SecretaryAnnouncementStatus = x.SecretaryAnnouncementStatus,
                    SecretaryAnnouncement = x.SecretaryAnnouncement,
                    SecretaryAnnouncementUpdatedAt = x.SecretaryAnnouncementUpdatedAt,
                    SecretaryAnnouncementUserId = x.SecretaryAnnouncementUserId,
                    SecretaryAnnouncementUserName = secretaryUser == null
                        ? null
                        : secretaryUser.FirstName + " " + secretaryUser.LastName,
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
