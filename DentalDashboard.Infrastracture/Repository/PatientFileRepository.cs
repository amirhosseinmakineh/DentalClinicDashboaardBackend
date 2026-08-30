using System.Data;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DentalDashboard.Infrastracture.Repository;

public sealed class PatientFileRepository(DentalContext context) : IPatientFileRepository
{
    public IQueryable<PatientFile> PatientFiles => context.PatientFiles;
    public IQueryable<LeadAssignment> Patients => context.LeadAssignments;
    public IQueryable<Reservation> Reservations => context.Reservations;

    public Task AddAsync(PatientFile entity, CancellationToken cancellationToken) =>
        context.PatientFiles.AddAsync(entity, cancellationToken).AsTask();

    public Task AddRangeAsync(IEnumerable<PatientFile> entities, CancellationToken cancellationToken) =>
        context.PatientFiles.AddRangeAsync(entities, cancellationToken);

    public async Task<long> GetNextFileNumberWithLockAsync(CancellationToken cancellationToken)
    {
        // The caller owns a transaction. UPDLOCK + HOLDLOCK serializes allocation,
        // while the unique index remains the final consistency guard.
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT ISNULL(MAX([FileNumber]), 0) + 1 FROM [PatientFiles] WITH (UPDLOCK, HOLDLOCK)";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value);
    }
}
