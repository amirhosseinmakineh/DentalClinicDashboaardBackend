using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Services;

internal static class SuccessfulPatientAttribution
{
    public static Task<int> CountAsync(
        IQueryable<Reservation> reservations,
        long consultantProfileId,
        DateTime periodStartedAt,
        DateTime periodEndedAt,
        CancellationToken cancellationToken) =>
        reservations.AsNoTracking()
            .Where(x => x.ConsultantProfileId == consultantProfileId &&
                        !x.IsDeleted &&
                        !x.IsCanceled &&
                        x.PatientUserId.HasValue &&
                        x.SecretaryReviewedAt.HasValue &&
                        x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryApproved &&
                        x.SecretaryReviewedAt.Value >= periodStartedAt &&
                        x.SecretaryReviewedAt.Value < periodEndedAt)
            .Select(x => x.PatientUserId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);
}
