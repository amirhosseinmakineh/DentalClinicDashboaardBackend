using DentalDashboard.Accounting.Integration.PatientFiles.Services;
using DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.PatientFiles;

public sealed class GetPatientFileByIdQueryHandler(
    IPatientFileRepository patientFileRepository,
    IPatientFileFinanceService patientFileFinanceService)
    : IQueryHandler<GetPatientFileByIdQuery, Result<PatientFileDto>>
{
    public async Task<Result<PatientFileDto>> HandleAsync(
        GetPatientFileByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        var patientFile = await patientFileRepository.PatientFiles
            .AsNoTracking()
            .Where(item => item.Id == request.Id)
            .Select(item => new PatientFileDto(
                item.Id,
                item.PatientReferenceId,
                item.FileNumber,
                item.FirstName,
                item.LastName,
                item.PhoneNumber,
                item.Description,
                item.SourceType,
                item.CreatedAt,
                null))
            .SingleOrDefaultAsync(cancellationToken);

        if (patientFile is null)
        {
            return Result<PatientFileDto>.Failure("پرونده بیمار یافت نشد");
        }

        var enrichedPatientFile = (await patientFileFinanceService.AttachFinanceAsync(
            [patientFile],
            cancellationToken))[0];

        return Result<PatientFileDto>.Success(enrichedPatientFile);
    }
}
