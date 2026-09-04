using DentalDashboard.Accounting.Integration.PatientFiles.Services;
using DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.PatientFiles;

public sealed class GetPatientFilesQueryHandler(
    IPatientFileRepository patientFileRepository,
    IPatientFileFinanceService patientFileFinanceService)
    : IQueryHandler<GetPatientFilesQuery, Result<PatientFilePageResponse>>
{
    public async Task<Result<PatientFilePageResponse>> HandleAsync(
        GetPatientFilesQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100)
        {
            return Result<PatientFilePageResponse>.Failure(
                "مقادیر صفحه‌بندی معتبر نیستند");
        }

        var patientFilesQuery = patientFileRepository.PatientFiles.AsNoTracking();

        if (request.FileNumber.HasValue)
        {
            patientFilesQuery = patientFilesQuery.Where(
                patientFile => patientFile.FileNumber == request.FileNumber);
        }

        if (request.SourceType.HasValue)
        {
            patientFilesQuery = patientFilesQuery.Where(
                patientFile => patientFile.SourceType == request.SourceType);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();
            var isNumericSearch = long.TryParse(searchTerm, out var fileNumber);

            patientFilesQuery = patientFilesQuery.Where(patientFile =>
                patientFile.FirstName.Contains(searchTerm) ||
                patientFile.LastName.Contains(searchTerm) ||
                (patientFile.FirstName + " " + patientFile.LastName).Contains(searchTerm) ||
                patientFile.PhoneNumber.Contains(searchTerm) ||
                (isNumericSearch && patientFile.FileNumber == fileNumber));
        }

        var totalCount = await patientFilesQuery.CountAsync(cancellationToken);
        var patientFiles = await patientFilesQuery
            .OrderByDescending(patientFile => patientFile.FileNumber)
            .ThenByDescending(patientFile => patientFile.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(patientFile => new PatientFileDto(
                patientFile.Id,
                patientFile.PatientReferenceId,
                patientFile.FileNumber,
                patientFile.FirstName,
                patientFile.LastName,
                patientFile.PhoneNumber,
                patientFile.Description,
                patientFile.SourceType,
                patientFile.CreatedAt,
                null))
            .ToListAsync(cancellationToken);

        patientFiles = await patientFileFinanceService.AttachFinanceAsync(
            patientFiles,
            cancellationToken);

        return Result<PatientFilePageResponse>.Success(
            new(patientFiles, request.Page, request.PageSize, totalCount));
    }
}
