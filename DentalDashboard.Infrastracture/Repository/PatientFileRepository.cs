using System.Data;
using System.Globalization;
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

    public async Task<long> GetNextFileNumberWithLockAsync(
        DateOnly attendanceDate,
        CancellationToken cancellationToken)
    {
        var gregorianDate = attendanceDate.ToDateTime(TimeOnly.MinValue);
        var persianCalendar = new PersianCalendar();
        var datePrefix =
            (long)persianCalendar.GetYear(gregorianDate) * 1_000_000L +
            persianCalendar.GetMonth(gregorianDate) * 10_000L +
            persianCalendar.GetDayOfMonth(gregorianDate) * 100L;
        var firstNumberOfDay = datePrefix + 1;
        var lastNumberOfDay = datePrefix + 99;

        // The caller owns a transaction. The range lock serializes allocation
        // for this attendance date while the unique index is the final guard.
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = @"
SELECT ISNULL(MAX([FileNumber]), @datePrefix) + 1
FROM [PatientFiles] WITH (UPDLOCK, HOLDLOCK)
WHERE [FileNumber] BETWEEN @firstNumberOfDay AND @lastNumberOfDay";
        var datePrefixParameter = command.CreateParameter();
        datePrefixParameter.ParameterName = "@datePrefix";
        datePrefixParameter.Value = datePrefix;
        command.Parameters.Add(datePrefixParameter);
        var firstNumberParameter = command.CreateParameter();
        firstNumberParameter.ParameterName = "@firstNumberOfDay";
        firstNumberParameter.Value = firstNumberOfDay;
        command.Parameters.Add(firstNumberParameter);
        var lastNumberParameter = command.CreateParameter();
        lastNumberParameter.ParameterName = "@lastNumberOfDay";
        lastNumberParameter.Value = lastNumberOfDay;
        command.Parameters.Add(lastNumberParameter);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        var nextFileNumber = Convert.ToInt64(value);

        if (nextFileNumber > lastNumberOfDay)
            throw new InvalidOperationException("ظرفیت شماره پرونده برای تاریخ حضور انتخاب‌شده تکمیل شده است");

        return nextFileNumber;
    }
}
