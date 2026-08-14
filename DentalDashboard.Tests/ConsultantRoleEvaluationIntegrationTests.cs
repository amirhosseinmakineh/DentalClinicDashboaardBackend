using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.RolePolicies;
using DentalDashboard.Framwork.IRepositories;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Infrastracture.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Tests;

public class ConsultantRoleEvaluationIntegrationTests
{
    [Fact]
    public async Task Test_without_successful_patient_is_atomically_deactivated()
    {
        await using var database = await TestDatabase.CreateAsync();
        var end = DateTime.UtcNow.AddMinutes(-1);
        var profile = await database.AddConsultantAsync(ConsultantRole.Test, end.AddDays(-10), end);

        await database.CreateEvaluationService().EvaluateDueConsultantsAsync();

        database.Context.ChangeTracker.Clear();
        var saved = await database.Context.ConsultantProfiles.Include(x => x.User).SingleAsync(x => x.Id == profile.Id);
        var evaluation = await database.Context.ConsultantRoleEvaluations.SingleAsync();
        Assert.False(saved.User.IsActive);
        Assert.False(saved.IsAvailable);
        Assert.False(saved.IsOnline);
        Assert.Equal(ConsultantEvaluationResult.Deactivated, evaluation.Result);
        Assert.Null(evaluation.ResultingRole);
    }

    [Fact]
    public async Task Evaluation_is_idempotent_and_uses_previous_period_boundary()
    {
        await using var database = await TestDatabase.CreateAsync();
        var end = DateTime.UtcNow.AddMinutes(-1);
        var profile = await database.AddConsultantAsync(ConsultantRole.Seller, end.AddDays(-10), end);
        await database.AddSuccessfulPatientsAsync(profile, 1, end.AddDays(-2));
        var service = database.CreateEvaluationService();

        await service.EvaluateDueConsultantsAsync();
        await service.EvaluateDueConsultantsAsync();

        database.Context.ChangeTracker.Clear();
        var saved = await database.Context.ConsultantProfiles.SingleAsync(x => x.Id == profile.Id);
        Assert.Equal(ConsultantRole.Seller, saved.ConsultantRole);
        Assert.Equal(end, saved.RoleStartedAt);
        Assert.Equal(end.AddDays(10), saved.NextRoleEvaluationAt);
        Assert.Equal(1, await database.Context.ConsultantRoleEvaluations.CountAsync());
    }

    [Fact]
    public async Task Concurrent_evaluation_services_persist_only_one_period()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dental-{Guid.NewGuid():N}.db");
        try
        {
            await using (var setup = await TestDatabase.CreateFileAsync(path))
            {
                var end = DateTime.UtcNow.AddMinutes(-1);
                await setup.AddConsultantAsync(ConsultantRole.Seller, end.AddDays(-10), end);
            }

            await using var first = await TestDatabase.OpenFileAsync(path);
            await using var second = await TestDatabase.OpenFileAsync(path);
            await Task.WhenAll(
                first.CreateEvaluationService().EvaluateDueConsultantsAsync(),
                second.CreateEvaluationService().EvaluateDueConsultantsAsync());

            first.Context.ChangeTracker.Clear();
            Assert.Equal(1, await first.Context.ConsultantRoleEvaluations.CountAsync());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Admin_count_is_capped_at_next_evaluation_boundary()
    {
        await using var database = await TestDatabase.CreateAsync();
        var end = DateTime.UtcNow.AddMinutes(-5);
        var start = end.AddDays(-10);
        var profile = await database.AddConsultantAsync(ConsultantRole.Seller, start, end);
        await database.AddSuccessfulPatientsAsync(profile, 1, end.AddMinutes(-1));
        await database.AddSuccessfulPatientsAsync(profile, 1, end.AddMinutes(1));

        var status = await database.CreateEvaluationService().GetStatusAsync(profile.Id);

        Assert.Equal(1, status.SuccessfulPatientCount);
    }

    [Fact]
    public async Task Existing_consultant_with_migration_baseline_is_not_immediately_evaluated()
    {
        await using var database = await TestDatabase.CreateAsync();
        var baseline = DateTime.UtcNow;
        var profile = await database.AddConsultantAsync(
            ConsultantRole.TopSeller,
            baseline,
            baseline.AddDays(7),
            createdAt: baseline.AddYears(-2));

        await database.CreateEvaluationService().EvaluateDueConsultantsAsync();

        Assert.Empty(await database.Context.ConsultantRoleEvaluations.ToListAsync());
        Assert.Equal(ConsultantRole.TopSeller, profile.ConsultantRole);
        Assert.True(profile.User.IsActive);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly ConsultantRolePolicyProvider policies = new();
        public DentalContext Context { get; }

        private TestDatabase(SqliteConnection connection, DentalContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            return await CreateAsync(connection, ensureCreated: true);
        }

        public static async Task<TestDatabase> CreateFileAsync(string path)
        {
            var connection = new SqliteConnection($"Data Source={path}");
            await connection.OpenAsync();
            return await CreateAsync(connection, ensureCreated: true);
        }

        public static async Task<TestDatabase> OpenFileAsync(string path)
        {
            var connection = new SqliteConnection($"Data Source={path}");
            await connection.OpenAsync();
            return await CreateAsync(connection, ensureCreated: false);
        }

        private static async Task<TestDatabase> CreateAsync(SqliteConnection connection, bool ensureCreated)
        {
            var options = new DbContextOptionsBuilder<DentalContext>().UseSqlite(connection).Options;
            var context = new DentalContext(options);
            if (ensureCreated)
                await context.Database.EnsureCreatedAsync();
            return new TestDatabase(connection, context);
        }

        public async Task<ConsultantProfile> AddConsultantAsync(
            ConsultantRole role,
            DateTime start,
            DateTime next,
            DateTime? createdAt = null)
        {
            var user = NewUser($"09{Guid.NewGuid():N}"[..20]);
            var profile = new ConsultantProfile
            {
                User = user,
                UserId = user.Id,
                NationalCode = "1234567890",
                Address = "Test address",
                ConsultantRole = role,
                RoleStartedAt = start,
                NextRoleEvaluationAt = next,
                CreatedAt = createdAt ?? start,
                IsCompleteProfile = true
            };
            Context.Add(profile);
            await Context.SaveChangesAsync();
            return profile;
        }

        public async Task AddSuccessfulPatientsAsync(ConsultantProfile profile, int count, DateTime reviewedAt)
        {
            for (var index = 0; index < count; index++)
            {
                var patient = NewUser($"08{Guid.NewGuid():N}"[..20]);
                var lead = new LeadAssignment
                {
                    UserName = "Patient",
                    PhoneNumber = $"07{Guid.NewGuid():N}"[..20],
                    ConsultantProfile = profile,
                    ConsultantProfileId = profile.Id
                };
                Context.Reservations.Add(new Reservation
                {
                    ConsultantProfile = profile,
                    ConsultantProfileId = profile.Id,
                    LeadAssignment = lead,
                    PatientUser = patient,
                    PatientUserId = patient.Id,
                    SecretaryReviewedAt = reviewedAt,
                    AttendanceConfirmationStatus = ReservationAttendanceConfirmationStatus.SecretaryApproved,
                    ReservationAt = reviewedAt,
                    InitialReservationAt = reviewedAt,
                    LastActivityAt = reviewedAt
                });
            }
            await Context.SaveChangesAsync();
        }

        public ConsultantRoleEvaluationService CreateEvaluationService() => new(
            new ConsultantProfileRepository(Context, policies),
            new ReservationRepository(Context),
            new BaseRepository<long, ConsultantRoleEvaluation>(Context),
            policies,
            new UnitOfWork(Context));

        private static User NewUser(string phone) => new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = phone,
            PasswordHash = "hash",
            IsActive = true
        };

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
