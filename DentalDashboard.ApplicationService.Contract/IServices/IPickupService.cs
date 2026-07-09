namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface IPickupService
    {
        Task<bool> PickupLeadAsync(long leadAssignmentId,long consultantProfileId,CancellationToken cancellationToken);
    }
}
