using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Services;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class PatientChartFinanceTests
{
    private const string Chart = """{"1":{"services":["root","crown","root"]},"2":{"service":"crown"}}""";

    [Fact]
    public void UnitsComeFromRealTeethAndSupportMultipleServicesAndHistoricalEntries()
    {
        var units = PatientChartFinanceRules.ReadUnits(Chart);
        Assert.Equal(1, units[5]); Assert.Equal(2, units[4]); Assert.Equal(2, units.Count);
        Assert.Empty(PatientChartFinanceRules.ReadUnits("""{"33":{"service":"root"},"0":{"service":"root"},"01":{"service":"root"},"x":{"service":"root"},"3":{"services":["unknown"]}}"""));
        Assert.Empty(PatientChartFinanceRules.ReadUnits("[]"));
        Assert.Empty(PatientChartFinanceRules.ReadUnits("invalid"));
    }

    [Fact]
    public void MissingChartAndManualOrStaleUnitsCannotCompleteFinance()
    {
        var units = PatientChartFinanceRules.ReadUnits(Chart);
        Assert.NotNull(PatientChartFinanceRules.ValidateSelection(new Dictionary<int, int> { [1] = 20 }, units));
        Assert.NotNull(PatientChartFinanceRules.ValidateSelection(units, new Dictionary<int, int>()));
        Assert.NotNull(PatientChartFinanceRules.ValidateSelection(null, units));
        Assert.Null(PatientChartFinanceRules.ValidateSelection(new Dictionary<int, int> { [4] = 2, [5] = 1 }, units));
    }

    [Fact]
    public void PreviouslyConfirmedPlanCanBeEditedBeforeFinanceWithoutADeadEnd()
    {
        var w = new PatientFileWorkflow { ConsultationCompleted = true, TreatmentConfirmedAtUtc = DateTime.UtcNow };
        Assert.Null(PatientChartFinanceRules.RevisePlan(w, Chart, true, true));
        Assert.True(w.ConsultationCompleted); Assert.NotNull(w.TreatmentConfirmedAtUtc);
        Assert.Null(w.FinancialCase); Assert.Null(w.ApprovedAtUtc);
    }

    [Fact]
    public void ChartCorrectionSynchronizesFinanceServicesAndRevokesApprovalsWithoutChangingReceipts()
    {
        var cheque = new PatientCheque { Id = 7, Amount = 800, SayadNumber = "123" };
        var transaction = new PatientFinancialTransaction { Id = 9, Amount = 100 };
        var c = new PatientFinancialCase { Id = Guid.NewGuid(), TotalAmount = 1000, PrePaymentAmount = 100 };
        c.Cheques.Add(cheque); c.Transactions.Add(transaction);
        var w = new PatientFileWorkflow { ConsultationCompleted = true, FinancialCase = c,
            TreatmentConfirmedAtUtc = DateTime.UtcNow, FinanceConfirmedAtUtc = DateTime.UtcNow,
            ProfileConfirmedAtUtc = DateTime.UtcNow, ApprovedAtUtc = DateTime.UtcNow, ApprovedFinanceHash = "old" };
        Assert.Null(PatientChartFinanceRules.RevisePlan(w, Chart, true, true));
        Assert.Equal(1000, c.TotalAmount); Assert.Equal(100, c.PrePaymentAmount);
        Assert.Same(cheque, Assert.Single(c.Cheques)); Assert.Equal(800, cheque.Amount);
        Assert.Same(transaction, Assert.Single(c.Transactions));
        Assert.Equal(3, c.ToothUnitCount);
        Assert.Contains(DentalDashboard.Domain.Enums.DentalServiceType.Crown, c.Services);
        Assert.Contains(DentalDashboard.Domain.Enums.DentalServiceType.RootCanal, c.Services);
        Assert.Null(w.FinanceConfirmedAtUtc); Assert.Null(w.ProfileConfirmedAtUtc);
        Assert.Null(w.ApprovedAtUtc); Assert.Null(w.ApprovedFinanceHash);
        Assert.NotNull(w.TreatmentConfirmedAtUtc);
    }

    [Fact]
    public void EmptyDraftIsAllowedBeforeFinanceButCannotEraseAFinancedPlan()
    {
        Assert.Null(PatientChartFinanceRules.RevisePlan(new(), "{}", true, true));
        var c = new PatientFinancialCase { TotalAmount = 1000 };
        var w = new PatientFileWorkflow { FinancialCase = c, ApprovedAtUtc = DateTime.UtcNow };
        var approval = w.ApprovedAtUtc;
        Assert.NotNull(PatientChartFinanceRules.RevisePlan(w, "{}", true, true));
        Assert.Equal(approval, w.ApprovedAtUtc); Assert.Equal(1000, c.TotalAmount);
    }

    [Fact]
    public void UnchangedSaveDoesNotInvalidateExistingApprovals()
    {
        var w = new PatientFileWorkflow { FinanceConfirmedAtUtc = DateTime.UtcNow, ApprovedAtUtc = DateTime.UtcNow };
        Assert.Null(PatientChartFinanceRules.RevisePlan(w, Chart, false, false));
        Assert.NotNull(w.FinanceConfirmedAtUtc); Assert.NotNull(w.ApprovedAtUtc);
    }
}
