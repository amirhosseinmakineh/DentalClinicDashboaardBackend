using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.IRepositories
{
    public interface IPatientProfileRepository : IBaseRepository<long, PatientProfile>
    {
    }
}
