namespace DentalDashboard.ApplicationService.Contract.IServices;

public interface IPushNotificationService
{
    Task<bool> SendAsync(
        Guid userId,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
