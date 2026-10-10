namespace DentalDashboard.ApplicationService.Contract.IServices;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream stream, string extension, CancellationToken cancellationToken = default);
    void Delete(string? relativePath);
}
