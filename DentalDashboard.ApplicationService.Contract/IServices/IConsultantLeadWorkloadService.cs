namespace DentalDashboard.ApplicationService.Contract.IServices;

public interface IConsultantLeadWorkloadService
{
    Task<ConsultantLeadWorkloadStatus> GetStatusAsync(
        long consultantProfileId,
        CancellationToken cancellationToken = default);
}
