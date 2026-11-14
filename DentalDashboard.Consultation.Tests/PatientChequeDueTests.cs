using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Services;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class PatientChequeDueTests
{
    [Fact]
    public void DueDateUsesIranMidnightAndExcludesNextDay()
    {
        var day = new DateOnly(2026, 11, 14);
        var (start, _) = IranTimeHelper.GetIranDayRangeAsUtc(day);
        var (end, _) = IranTimeHelper.GetIranDayRangeAsUtc(day.AddDays(1));
        PatientCheque Cheque(long id, DateTime date) => new() {
            Id = id, DueDate = date, FinancialCase = new PatientFinancialCase()
        };
        var source = new[] { Cheque(1, start.AddTicks(-1)), Cheque(2, start),
            Cheque(3, end.AddTicks(-1)), Cheque(4, end) };
        Assert.Equal(13, start.Day);
        Assert.Equal(new long[] { 2, 3 }, PatientChequeDueQuery.ForDay(source.AsQueryable(), day).Select(x => x.Id));
    }

    [Fact]
    public void DeletedAndCancelledRecordsAreExcludedWhilePaidAndGuaranteeAreLabelledByReport()
    {
        var day = new DateOnly(2026, 11, 14);
        var (start, _) = IranTimeHelper.GetIranDayRangeAsUtc(day);
        var cheques = Enumerable.Range(1, 7).Select(id => new PatientCheque {
            Id = id, DueDate = start, FinancialCase = new PatientFinancialCase()
        }).ToArray();
        cheques[0].IsDeleted = true;
        cheques[1].FinancialCase.IsDeleted = true;
        cheques[2].Status = PatientChequeStatus.Cancelled;
        cheques[3].FinancialCase.Status = PatientFinancialCaseStatus.Cancelled;
        cheques[4].Status = PatientChequeStatus.Paid;
        cheques[5].IsGuarantee = true;
        Assert.Equal(new long[] { 5, 6, 7 }, PatientChequeDueQuery.ForDay(cheques.AsQueryable(), day).Select(x => x.Id));
    }

    [Fact]
    public void DueDateFilterTranslatesToSqlWithoutConnectingToDatabase()
    {
        using var db = new DentalContextDesignTimeFactory().CreateDbContext([]);
        var sql = PatientChequeDueQuery.ForDay(db.Set<PatientCheque>(), new DateOnly(2026, 11, 14)).ToQueryString();
        Assert.Contains("DueDate", sql);
        Assert.Contains(">=", sql);
        Assert.Contains("<", sql);
    }
}
