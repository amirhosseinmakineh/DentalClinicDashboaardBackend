using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Repository
{
    public class LeadAssignmentRepository : BaseRepository<long, LeadAssignment> , ILeadAssignmentRepository
    {
        public LeadAssignmentRepository(DentalContext context) : base(context)
        {
        }
    }

}
