using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
namespace DentalDashboard.ApplicationService.Services;

public class PickUpService : IPickupService
{
    private readonly ILeadAssignmentRepository leadAssignmentRepository;
    private readonly IConsultantProfileRepository consultantProfileRepository;
    private readonly IUnitOfWork unitOfWork;

    public PickUpService(
        ILeadAssignmentRepository leadAssignmentRepository,
        IConsultantProfileRepository consultantProfileRepository,
        IUnitOfWork unitOfWork)
    {
        this.leadAssignmentRepository = leadAssignmentRepository;
        this.consultantProfileRepository = consultantProfileRepository;
        this.unitOfWork = unitOfWork;
    }


    public async Task<bool> PickupLeadAsync(
        long leadAssignmentId,
        long consultantProfileId,
        CancellationToken cancellationToken)
    {
        // 1- بررسی ظرفیت روزانه مشاور
        var todayPickupCount = await leadAssignmentRepository
            .GetTodayPickupCountAsync(consultantProfileId);


        if (todayPickupCount >= 10)
        {
            return false;
        }


        // 2- تلاش برای Pickup اتمیک
        // فقط یک نفر می‌تواند موفق شود
        var pickedUp = await leadAssignmentRepository
            .TryPickupLeadAsync(
                leadAssignmentId,
                consultantProfileId,
                cancellationToken);


        if (!pickedUp)
        {
            // یعنی شخص دیگری قبلاً گرفته
            return false;
        }


        // 3- Offline کردن مشاور
        var consultant = await consultantProfileRepository
            .GetByIdAsync(consultantProfileId);


        if (consultant != null)
        {
            consultant.IsOnline = false;
            consultant.LastOfflineAt = DateTime.UtcNow;
        }


        // 4- ذخیره نهایی
        await unitOfWork.SaveChangesAsync();


        return true;
    }
}