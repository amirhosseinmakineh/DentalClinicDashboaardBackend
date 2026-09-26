using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;

namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface IPickupService
    {
        Task<PickupLeadResult> PickupLeadAsync(
            long leadAssignmentId,
            long consultantProfileId,
            CancellationToken cancellationToken);
    }
}
