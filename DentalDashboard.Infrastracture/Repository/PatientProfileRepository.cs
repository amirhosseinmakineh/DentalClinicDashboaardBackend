using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Repository
{
    public class PatientProfileRepository : BaseRepository<long, PatientProfile> , IPatientProfileRepository
    {
        public PatientProfileRepository(DentalContext context) : base(context)
        {
        }
    }

}
