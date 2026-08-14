namespace DentalDashboard.ApplicationService.Contract.IServices;

public interface IConsultantRoleEvaluationService
{
    Task EvaluateDueConsultantsAsync(CancellationToken cancellationToken = default);
    Task<ConsultantRoleEvaluationStatus> GetStatusAsync(long consultantProfileId, CancellationToken cancellationToken = default);
}
