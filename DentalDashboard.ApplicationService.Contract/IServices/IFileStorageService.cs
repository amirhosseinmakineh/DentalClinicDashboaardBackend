namespace DentalDashboard.ApplicationService.Contract.IServices;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream stream, string extension, CancellationToken cancellationToken = default);
    void Delete(string? relativePath);
    Task<string> SavePatientDocumentAsync(
        Stream stream,
        string extension,
        CancellationToken cancellationToken = default);
    Stream? OpenPatientDocument(string path);
    void DeletePatientDocument(string? path);
}
