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
            var pageSize = new[] { 10, 20, 50 }.Contains(query.PageSize) ? query.PageSize : 20;

            var reservations = reservationRepository.GetAll().AsNoTracking();

            if (!query.IncludeCanceled)
                reservations = reservations.Where(x => !x.IsCanceled);

            if (query.ConsultantProfileId.HasValue)
                reservations = reservations.Where(x => x.ConsultantProfileId == query.ConsultantProfileId.Value);

            reservations = reservations.ApplyReservationAtFilter(query.Date, query.From, query.To);

            if (query.ReservationRequestStatus.HasValue)
                reservations = reservations.Where(x => x.ReservationRequestStatus == query.ReservationRequestStatus);
            if (query.VisitResultStatus.HasValue)
                reservations = reservations.Where(x => x.VisitResultStatus == query.VisitResultStatus);
            if (query.IsConfirmedWithPatient.HasValue)
                reservations = reservations.Where(x => x.IsConfirmedWithPatient == query.IsConfirmedWithPatient);
            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var term = query.SearchText.Trim();
                reservations = reservations.Where(x => x.LeadAssignment.UserName.Contains(term) ||
                    x.LeadAssignment.PhoneNumber.Contains(term) ||
                    (x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName).Contains(term));
            }
            if (!string.IsNullOrWhiteSpace(query.ConsultantName))
            {
                var consultant = query.ConsultantName.Trim();
                reservations = reservations.Where(x => (x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName).Contains(consultant));
            }
            if (query.ReservationDate.HasValue)
            {
                var zone = ResolveTimeZone(query.TimeZone);
                var localStart = query.ReservationDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
                var start = TimeZoneInfo.ConvertTimeToUtc(localStart, zone);
                var end = TimeZoneInfo.ConvertTimeToUtc(localStart.AddDays(1), zone);
                reservations = reservations.Where(x => x.ReservationAt >= start && x.ReservationAt < end);
            }
            if (query.FollowUpDueOn.HasValue)
            {
                var zone = ResolveTimeZone(query.TimeZone);
                var localStart = query.FollowUpDueOn.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
                var start = TimeZoneInfo.ConvertTimeToUtc(localStart, zone);
                var end = TimeZoneInfo.ConvertTimeToUtc(localStart.AddDays(1), zone);
                reservations = reservations.Where(x => x.FollowUps.Any(f => !f.IsDeleted && f.Status == FollowUpStatus.Pending &&
                    ((f.ScheduledAt >= start && f.ScheduledAt < end) || (f.ReminderAt >= start && f.ReminderAt < end))));
            }

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
            var ascending = string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            reservations = (query.SortBy?.ToLowerInvariant(), ascending) switch
            {
                ("reservationat", true) => reservations.OrderBy(x => x.ReservationAt).ThenBy(x => x.Id),
                ("reservationat", false) => reservations.OrderByDescending(x => x.ReservationAt).ThenByDescending(x => x.Id),
                ("reservationrequeststatus", true) => reservations.OrderBy(x => x.ReservationRequestStatus).ThenBy(x => x.Id),
                ("reservationrequeststatus", false) => reservations.OrderByDescending(x => x.ReservationRequestStatus).ThenByDescending(x => x.Id),
                ("requestcreatedat", true) => reservations.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
                _ => reservations.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            };
            var items = await reservations
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
                    ReservationAt = x.ReservationAt,
                    InitialReservationAt = x.InitialReservationAt,
                    RequestCreatedAt = x.CreatedAt,
                    LastActivityAt = x.LastActivityAt,
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
                    ,ReservationRequestStatus = x.ReservationRequestStatus
                    ,VisitResultStatus = x.VisitResultStatus
                    ,IsConfirmedWithPatient = x.IsConfirmedWithPatient
                    ,ConfirmedWithPatientAt = x.ConfirmedWithPatientAt
                    ,IsWaitingForConsultantTimeConfirmation = x.ReservationTimeChanges.Any(c => c.Status == ReservationTimeChangeStatus.PendingConsultantConfirmation)
                    ,SecretaryTimeChangeNote = x.ReservationTimeChanges.OrderByDescending(c => c.CreatedAt).Select(c => c.Note).FirstOrDefault()
                    ,SecretaryChangedReservationAt = x.ReservationTimeChanges.OrderByDescending(c => c.CreatedAt).Select(c => (DateTime?)c.CreatedAt).FirstOrDefault()
                    ,CallCount = x.ContactLogs.Count(c => !c.IsDeleted)
                    ,LastContactResult = x.ContactLogs.Where(c => !c.IsDeleted).OrderByDescending(c => c.CreatedAt).Select(c => c.Result.ToString()).FirstOrDefault()
                    ,LastFollowUpAt = x.FollowUps.Where(f => !f.IsDeleted).OrderByDescending(f => f.CreatedAt).Select(f => (DateTime?)f.ScheduledAt).FirstOrDefault()
                    ,RejectionReason = x.RejectionReason
                    ,CancellationReason = x.CancellationReason
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

        private static TimeZoneInfo ResolveTimeZone(string id)
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(id) ? "Asia/Tehran" : id); }
            catch (TimeZoneNotFoundException) when (id == "Asia/Tehran") { return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
        }
    }
}
