using System.Text.Json;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class ConsultantServiceRewardTests
{
    private static ConsultantServiceRewardRule Rule(string basis = "Block", int units = 4,
        string remainder = "FullBlocks") => new() { Id = 1, ServiceId = 3, Basis = basis,
        UnitsPerReward = units, RemainderMode = remainder, Amount = 1_000_000, IsActive = true };

    [Fact]
    public void PartialBlocksCarryBetweenVisitsWithoutRepeatingRewards()
    {
        var r = Rule();
        Assert.Equal(1_000_000, ConsultantServiceRewardCalculator.Calculate(r, [], [1, 2, 3, 4, 5, 6]));
        Assert.Equal(1_000_000, ConsultantServiceRewardCalculator.Calculate(r, [1, 2, 3, 4, 5, 6], [7, 8]));
        Assert.Equal(0, ConsultantServiceRewardCalculator.Calculate(r, [], [1, 2]));
        Assert.Equal(1_000_000, ConsultantServiceRewardCalculator.Calculate(r, [1, 2], [3, 4]));
    }

    [Fact]
    public void ProportionalAndPerUnitRewardsUseTheConfiguredRates()
    {
        Assert.Equal(1_500_000, ConsultantServiceRewardCalculator.Calculate(Rule(remainder: "Proportional"), [], [1, 2, 3, 4, 5, 6]));
        Assert.Equal(3_000_000, ConsultantServiceRewardCalculator.Calculate(Rule("Unit", 1), [], [1, 2, 3]));
        var r = Rule(units: 3, remainder: "Proportional"); r.Amount = 1;
        var total = ConsultantServiceRewardCalculator.Calculate(r, [], [1]) +
            ConsultantServiceRewardCalculator.Calculate(r, [1], [2]) +
            ConsultantServiceRewardCalculator.Calculate(r, [1, 2], [3]);
        Assert.Equal(1, total);
    }

    [Fact]
    public void JawsNeedTheirConfiguredThresholdAndEachPaysOnlyOnce()
    {
        var r = Rule("Jaw", 4);
        Assert.Equal(0, ConsultantServiceRewardCalculator.Calculate(r, [], [1, 2, 3]));
        Assert.Equal(1_000_000, ConsultantServiceRewardCalculator.Calculate(r, [1, 2, 3], [4]));
        Assert.Equal(0, ConsultantServiceRewardCalculator.Calculate(r, [1, 2, 3, 4], [5, 6]));
        Assert.Equal(1_000_000, ConsultantServiceRewardCalculator.Calculate(r, [1, 2, 3, 4], [17, 18, 19, 20]));
        Assert.Equal(0, ConsultantServiceRewardCalculator.Calculate(r, [], [1, 2, 17, 18]));
    }

    [Fact]
    public void InvalidOrRepeatedTeethAndInvalidRulesAreRejected()
    {
        Assert.Throws<ArgumentException>(() => ConsultantServiceRewardCalculator.Calculate(Rule(), [1], [1]));
        Assert.Throws<ArgumentException>(() => ConsultantServiceRewardCalculator.Calculate(Rule(), [], [1, 1]));
        Assert.Throws<ArgumentException>(() => ConsultantServiceRewardCalculator.Calculate(Rule(), [], [33]));
        Assert.NotNull(ConsultantServiceRewardCalculator.ValidateRule(Rule("Jaw", 17)));
        Assert.NotNull(ConsultantServiceRewardCalculator.ValidateRule(Rule("Unit", 4)));
        var r = Rule(); r.Amount = -1;
        Assert.NotNull(ConsultantServiceRewardCalculator.ValidateRule(r));
        r.Amount = 1; r.IsActive = false;
        Assert.Equal(0, ConsultantServiceRewardCalculator.Calculate(r, [], [1, 2, 3, 4]));
    }

    [Fact]
    public void SnapshotsKeepHistoricalRatesAndCalculationDetails()
    {
        var r = Rule();
        var line = new ServiceRewardLine(3, "لمینت", [1, 2, 3, 4], r.Id, r.Basis, r.UnitsPerReward,
            r.RemainderMode, r.Amount, 0, 4, ConsultantServiceRewardCalculator.Calculate(r, [], [1, 2, 3, 4]));
        var saved = JsonSerializer.Serialize(new ServiceRewardSnapshot(1, Guid.NewGuid(), 2, DateTime.UtcNow, [line]));
        r.Amount = 9_000_000;
        var old = JsonSerializer.Deserialize<ServiceRewardSnapshot>(saved)!;
        Assert.Equal(1_000_000, old.Total); Assert.Equal(1_000_000, old.Lines[0].Rate);
    }

    [Fact]
    public void ChartOptionsAreRealCanonicalTeethWithMultipleServicesSupported()
    {
        var options = ServiceRewardPreparationService.ReadChart("""
            {"1":{"services":["composite","laminate","laminate"]},"32":{"service":"root"},
             "33":{"service":"composite"},"01":{"service":"laminate"}}
            """);
        Assert.Equal(3, options.Count);
        Assert.Contains(options, x => x.ToothId == 1 && x.ServiceId == 3);
        Assert.Contains(options, x => x.ToothId == 32 && x.ToothLabel.Contains("پایین"));
        Assert.Empty(ServiceRewardPreparationService.ReadChart("not json"));
    }

    [Fact]
    public void NewVersionsAndDisablingDoNotChangeTheRuleOfAnOngoingTreatment()
    {
        var old = Rule();
        var latest = Rule(); latest.Id = 2; latest.Amount = 8_000_000; latest.IsActive = false;
        Assert.False(Assert.Single(ConsultantServiceRewardCalculator.ResolveTreatmentRules([old, latest], new Dictionary<int, long?>())).IsActive);
        var frozen = Assert.Single(ConsultantServiceRewardCalculator.ResolveTreatmentRules([old, latest], new Dictionary<int, long?> { [3] = 1 }));
        Assert.Equal(1, frozen.Id); Assert.True(frozen.IsActive); Assert.Equal(1_000_000, frozen.Amount);
        Assert.Empty(ConsultantServiceRewardCalculator.ResolveTreatmentRules([old, latest], new Dictionary<int, long?> { [3] = null }));
    }

    [Fact]
    public void MissingRecipientRequiresAnActiveSelectionAndExistingRecipientsCannotBeReassigned()
    {
        var context = new ServiceRewardContext([new(2, "one"), new(3, "two")], null, true, [], []);
        Assert.NotNull(ServiceRewardPreparationService.ResolveRecipient(context, null).Error);
        Assert.NotNull(ServiceRewardPreparationService.ResolveRecipient(context, 99).Error);
        Assert.Equal(3, ServiceRewardPreparationService.ResolveRecipient(context, 3).Id);
        context = context with { LinkedConsultantProfileId = 2, CanChooseConsultant = false };
        Assert.Equal(2, ServiceRewardPreparationService.ResolveRecipient(context, null).Id);
        Assert.NotNull(ServiceRewardPreparationService.ResolveRecipient(context, 3).Error);
    }

    [Fact]
    public void ManagedServiceResultFlagCannotBeSuppliedByAnApiClient()
    {
        var command = JsonSerializer.Deserialize<DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands.ReviewReservationAttendanceCommand>(
            "{\"ServiceRewardManaged\":true}")!;
        Assert.False(command.ServiceRewardManaged);
    }

    [Fact]
    public async Task ManagedResultsCanUseASelectedRecipientWithoutChangingTheAppointmentOwner()
    {
        var reservation = new Reservation { Id = 1, ConsultantProfileId = 2, RewardConsultantProfileId = 7,
            OwnerType = ReservationOwnerType.Secretary, ReservationAt = DateTime.Now.AddDays(-1) };
        var reservations = PatientFileServiceResultTests.RepositoryDouble.Create<DentalDashboard.Domain.IRepositories.IReservationRepository>(method => method.Name switch {
            "GetAll" => new PatientFileServiceResultTests.AsyncQuery<Reservation>(new[] { reservation }.AsQueryable()),
            "Update" => null, "SaveChange" => Task.CompletedTask, _ => throw new NotSupportedException(method.Name)
        });
        var consultants = PatientFileServiceResultTests.RepositoryDouble.Create<DentalDashboard.Domain.IRepositories.IConsultantProfileRepository>(method =>
            method.Name == "GetAll" ? new PatientFileServiceResultTests.AsyncQuery<ConsultantProfile>(Array.Empty<ConsultantProfile>().AsQueryable())
                : throw new NotSupportedException(method.Name));
        var handler = new DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation.ReviewReservationAttendanceCommandHandler(reservations, consultants);
        var result = await handler.HandleAsync(new() { ReservationId = 1, PatientReceivedService = true,
            DoctorName = "doctor", ServiceRewardManaged = true });
        Assert.True(result.IsSuccess); Assert.NotNull(reservation.ConsultantRewardEligibleAt);
        Assert.Equal(7, reservation.RewardConsultantProfileId); Assert.Equal(2, reservation.ConsultantProfileId);
        Assert.Equal(ReservationOwnerType.Secretary, reservation.OwnerType);
    }

    [Fact]
    public void ExecutionIdentityIsUniqueAndRecipientIsAnIndependentForeignKey()
    {
        using var db = new DentalContextDesignTimeFactory().CreateDbContext([]);
        var model = db.Model.FindEntityType(typeof(ConsultantRewardExecution))!;
        Assert.Contains(model.GetIndexes(), x => x.IsUnique &&
            x.Properties.Select(p => p.Name).SequenceEqual(new[] { "FinancialCaseId", "ServiceId", "ToothId" }));
        var reservation = db.Model.FindEntityType(typeof(Reservation))!;
        Assert.True(reservation.FindProperty("RewardConsultantProfileId")!.IsNullable);
        Assert.NotNull(reservation.FindNavigation("RewardConsultantProfile"));
        var sql = db.Reservations.Where(x => (x.RewardConsultantProfileId.HasValue
            ? x.RewardConsultantProfile!.UserId : x.ConsultantProfile.UserId) == Guid.Empty).ToQueryString();
        Assert.Contains("RewardConsultantProfileId", sql);
    }
}
