namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface ILeadAssignmentLimitService
    {
        Task<bool> CanPickupLeadAsync(long consultantProfileId);
    }
}
