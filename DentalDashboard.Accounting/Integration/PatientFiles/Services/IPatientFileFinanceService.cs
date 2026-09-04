using DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;

namespace DentalDashboard.Accounting.Integration.PatientFiles.Services;

public interface IPatientFileFinanceService
{
    Task<List<PatientFileDto>> AttachFinanceAsync(
        IReadOnlyList<PatientFileDto> patientFiles,
        CancellationToken cancellationToken);
}
