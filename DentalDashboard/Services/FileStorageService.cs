using DentalDashboard.ApplicationService.Contract.IServices;

namespace DentalDashboard.Services;

public sealed class FileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    private const string Folder = "uploads/guarantee-documents";

    public async Task<string> SaveAsync(Stream stream, string extension, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(env.WebRootPath, Folder);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(directory, fileName);

        await using var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(file, cancellationToken);

        return $"/{Folder}/{fileName}";
    }

    public void Delete(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.StartsWith($"/{Folder}/"))
            return;

        var fullPath = Path.Combine(env.WebRootPath, relativePath.TrimStart('/'));

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
