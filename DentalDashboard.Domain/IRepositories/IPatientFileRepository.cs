using DentalDashboard.Domain.Models;

namespace DentalDashboard.Domain.IRepositories;

public interface IPatientFileRepository
{
    IQueryable<PatientFile> PatientFiles { get; }
    IQueryable<LeadAssignment> Patients { get; }
    IQueryable<Reservation> Reservations { get; }
    Task AddAsync(PatientFile entity, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<PatientFile> entities, CancellationToken cancellationToken);
    Task<long> GetNextFileNumberWithLockAsync(CancellationToken cancellationToken);
}
