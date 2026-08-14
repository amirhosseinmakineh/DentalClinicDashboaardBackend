using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Repository
{
    public class ConsultantProfileRepository : BaseRepository<long, ConsultantProfile>, IConsultantProfileRepository
    {
        public ConsultantProfileRepository(DentalContext context) : base(context)
        {
        }

        public  Task<List<ConsultantProfile>> GetAvailableAndOnnlineSellerConsultant()
        {
            return  GetAll()
                .Where(x=> 
                x.IsAvailable == true &&
                x.IsOnline == true &&
                x.ConsultantRole == ConsultantRole.Seller && x.IsCompleteProfile == true)
                .ToListAsync();

        }

        public Task<List<ConsultantProfile>> GetAvailableAndOnnlineTestConsultant()
        {
            var fiveDaysAgo = DateTime.UtcNow.AddDays(-5);

            return GetAll()
                .Where(x =>
                    x.IsAvailable &&
                    x.IsOnline &&
                    x.IsCompleteProfile &&
                    x.ConsultantRole == ConsultantRole.Test &&
                    x.RoleStartedAt.HasValue &&
                    x.RoleStartedAt.Value > fiveDaysAgo)
                .ToListAsync();
        }

        public Task<List<ConsultantProfile>> GetAvailableAndOnnlineTopSellerConsultant()
        {
            return GetAll()
                .Where(x =>
                x.IsAvailable == true &&
                x.IsOnline == true &&
                x.ConsultantRole == ConsultantRole.TopSeller
                && x.IsCompleteProfile == true)
                .ToListAsync();
        }

        public Task<List<ConsultantProfile>> GetAvailableConsultantsAsync()
        {
            return GetAll()
                .Where(x => !x.IsDeleted &&
                            x.IsCompleteProfile &&
                            x.IsAvailable &&
                            !x.IsOnline)
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public Task<List<ConsultantProfile>> GetOnlineConsultantsReadyForRealTimeAsync()
        {
            return GetAll()
                .Where(x => !x.IsDeleted &&
                            x.IsCompleteProfile &&
                            x.IsAvailable &&
                            x.IsOnline &&
                            !x.CallAssignments.Any(l => l.AssignmentType == LeadAssignmentType.RealTime &&
                                                        l.LeadAssignmentState == LeadAssignmentState.Assigned &&
                                                        l.ReportSubmittedAt == null))
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public Task<bool> HasOnlineConsultantAsync()
        {
            return GetAll()
                .AnyAsync(x => !x.IsDeleted &&
                               x.IsCompleteProfile &&
                               x.IsAvailable &&
                               x.IsOnline);
        }
    }
}
