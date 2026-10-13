using DentalDashboard.ApplicationService.Contract.IServices;

namespace DentalDashboard.Services;

public sealed class FileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    private const string Folder = "uploads/guarantee-documents";

    private string UploadsRoot =>
        OperatingSystem.IsWindows()
            ? Path.Combine(env.ContentRootPath, "uploads")
            : "/tmp/uploads";

    public async Task<string> SaveAsync(Stream stream, string extension, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(UploadsRoot, "guarantee-documents");
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(directory, fileName);

        await using var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(file, cancellationToken);

        return $"/{Folder}/{fileName}";
    }

    private string PatientDocumentRoot =>
        Environment.GetEnvironmentVariable("PATIENT_DOCUMENTS_STORAGE_ROOT")
        ?? Path.Combine(env.ContentRootPath, "uploads", "patient-documents");

    public async Task<string> SavePatientDocumentAsync(
        Stream stream,
        string extension,
        CancellationToken cancellationToken = default)
    {
        var root = PatientDocumentRoot;
        Directory.CreateDirectory(root);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(root, fileName);

        await using var output = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);
        await stream.CopyToAsync(output, cancellationToken);
        return fileName;
    }

    public Stream? OpenPatientDocument(string path)
    {
        var fullPath = ResolvePatientDocumentPath(path);
        return fullPath is not null && File.Exists(fullPath)
            ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;
    }

    public void DeletePatientDocument(string? path)
    {
        var fullPath = ResolvePatientDocumentPath(path);
        if (fullPath is not null && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private string? ResolvePatientDocumentPath(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            Path.GetFileName(name) != name ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                name,
                @"^[a-f0-9]{32}\\.(jpg|jpeg|png)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            return null;
        }

        return Path.Combine(PatientDocumentRoot, name);
    }

    public void Delete(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.StartsWith($"/{Folder}/"))
            return;

        var fileName = Path.GetFileName(relativePath);
        var fullPath = Path.Combine(UploadsRoot, "guarantee-documents", fileName);

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
