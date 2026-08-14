using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;
using DentalDashboard.Domain.RolePolicies;

namespace DentalDashboard.Infrastracture.Repository
{
    public class ConsultantProfileRepository : BaseRepository<long, ConsultantProfile>, IConsultantProfileRepository
    {
        private readonly IConsultantRolePolicyProvider policyProvider;

        public ConsultantProfileRepository(DentalContext context, IConsultantRolePolicyProvider policyProvider) : base(context)
        {
            this.policyProvider = policyProvider;
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
            var receptionPeriod = policyProvider.Get(ConsultantRole.Test).LeadReceptionPeriod!.Value;
            var oldestEligibleStart = DateTime.UtcNow - receptionPeriod;

            return GetAll()
                .Where(x =>
                    x.IsAvailable &&
                    x.IsOnline &&
                    x.IsCompleteProfile &&
                    x.ConsultantRole == ConsultantRole.Test &&
                    x.RoleStartedAt.HasValue &&
                    x.RoleStartedAt.Value > oldestEligibleStart)
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
