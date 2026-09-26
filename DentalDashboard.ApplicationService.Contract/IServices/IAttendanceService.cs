namespace DentalDashboard.ApplicationService.Contract.IServices;

public interface IAttendanceService
{
    Task RecordCheckInAsync(
        long consultantProfileId,
        DateTime occurredAt,
        CancellationToken cancellationToken = default);

    Task RecordCheckOutAsync(
        long consultantProfileId,
        DateTime occurredAt,
        CancellationToken cancellationToken = default);
}
