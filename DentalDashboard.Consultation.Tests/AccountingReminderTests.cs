using System.Reflection;
using DentalDashboard.ApplicationService.Accounting.Reminders;
using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.ApplicationService.Contract.Accounting.Reminders;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Domain.Accounting.Reminders;
using DentalDashboard.Infrastracture.Accounting.Reminders;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class AccountingReminderTests
{
    [Theory]
    [InlineData("2026-11-24T05:29:00Z", "2026-11-23T20:30:00Z", false)]
    [InlineData("2026-11-24T05:30:00Z", "2026-11-23T20:30:00Z", true)]
    [InlineData("2026-11-24T05:30:00Z", "2026-11-24T20:30:00Z", true)]
    [InlineData("2026-11-24T05:30:00Z", "2026-11-25T20:30:00Z", false)]
    [InlineData("2026-11-24T05:30:00Z", "2026-11-22T20:30:00Z", false)]
    public void ScheduleUsesTehranCalendarAndNineAm(string now, string due, bool expected) =>
        Assert.Equal(expected, AccountingReminderSchedule.CanSend(DateTime.Parse(now).ToUniversalTime(), DateTime.Parse(due).ToUniversalTime()));

    [Fact]
    public void BothReminderSourcesAndCurrentStatusFiltersTranslateToSqlServer()
    {
        using var db = new DentalContextDesignTimeFactory().CreateDbContext([]);
        var repository = new AccountingReminderRepository(db);
        var query = (IQueryable<AccountingReminderTarget>)typeof(AccountingReminderRepository)
            .GetProperty("PendingTargets", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(repository)!;
        var sql = query.Where(x => x.Id > 5 && x.DueDate >= new DateTime(2026, 11, 24))
            .OrderBy(x => x.Id).Take(200).ToQueryString();
        Assert.Contains("UNION ALL", sql);
        Assert.Contains("PaidAtUtc", sql);
        Assert.Contains("PatientCheques", sql);
    }

    [Fact]
    public async Task SelectionRequiresPermissionAndPendingTargetButCanAlwaysBeDisabled()
    {
        var repository = new Repository();
        var access = new Access { Allowed = false };
        var service = new AccountingReminderService(access, repository, new Push());
        var command = new SetAccountingReminderCommand(new(Guid.NewGuid()), AccountingReminderKind.Cheque, 10, true);
        await Assert.ThrowsAsync<AccountingForbiddenException>(() => service.SetAsync(command, default));
        Assert.Equal(0, repository.Writes);
        access.Allowed = true;
        repository.Pending = false;
        await Assert.ThrowsAsync<AccountingValidationException>(() => service.SetAsync(command, default));
        await service.SetAsync(command with { Enabled = false }, default);
        Assert.Equal(1, repository.Writes);
        Assert.Equal(command.Actor.UserId, repository.LastUser);
    }

    [Fact]
    public async Task DispatcherDoesNotSendBeforeNineOrWhenTargetPaidOrPermissionRevoked()
    {
        var repository = new Repository();
        var access = new Access();
        var push = new Push();
        var dispatcher = new AccountingReminderDispatcher(repository, access, push, NullLogger<AccountingReminderDispatcher>.Instance);
        await dispatcher.DispatchAsync(new DateTime(2026, 11, 24, 5, 29, 0, DateTimeKind.Utc), default);
        Assert.Equal(0, repository.Scans);
        repository.Pending = false;
        await dispatcher.DispatchAsync(Now, default);
        Assert.Equal(0, push.Calls);
        repository.Pending = true;
        access.Allowed = false;
        await dispatcher.DispatchAsync(Now, default);
        Assert.Equal(0, push.Calls);
    }

    [Fact]
    public async Task SuccessfulSendIsRecordedAndFailedSendIsRetryable()
    {
        var repository = new Repository();
        var push = new Push();
        var dispatcher = new AccountingReminderDispatcher(repository, new Access(), push, NullLogger<AccountingReminderDispatcher>.Instance);
        await dispatcher.DispatchAsync(Now, default);
        Assert.Equal(1, push.Calls);
        Assert.False(repository.Delivered);
        push.Result = true;
        await dispatcher.DispatchAsync(Now, default);
        Assert.True(repository.Delivered);
        await dispatcher.DispatchAsync(Now, default);
        Assert.Equal(2, push.Calls);
        Assert.Equal(repository.Target.UserId, push.UserId);
        Assert.Equal("AccountingDueReminder", push.Data!["type"]);
        Assert.Contains("section=Cheques&targetId=7", push.Data["route"]);
    }

    private static readonly DateTime Now = new(2026, 11, 24, 5, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task DatabaseLeaseBlocksConcurrentSendRetriesFailureAndKeepsDatesSeparate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new DeliveryContext(new DbContextOptionsBuilder<DentalContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var repository = new AccountingReminderRepository(db);
        var today = Now.Date;
        var lease = Guid.NewGuid();
        Assert.True(await repository.ClaimAsync(1, today, today, lease, Now, default));
        Assert.False(await repository.ClaimAsync(1, today, today, Guid.NewGuid(), Now, default));
        await repository.CompleteAsync(lease, false, Now, default);
        Assert.False(await repository.ClaimAsync(1, today, today, Guid.NewGuid(), Now.AddMinutes(14), default));
        var retry = Guid.NewGuid();
        Assert.True(await repository.ClaimAsync(1, today, today, retry, Now.AddMinutes(16), default));
        await repository.CompleteAsync(retry, true, Now.AddMinutes(16), default);
        Assert.False(await repository.ClaimAsync(1, today, today, Guid.NewGuid(), Now.AddHours(2), default));
        Assert.True(await repository.ClaimAsync(1, today.AddDays(1), today, Guid.NewGuid(), Now, default));
    }

    private sealed class DeliveryContext(DbContextOptions<DentalContext> options) : DentalContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            foreach (var property in typeof(DentalContext).GetProperties())
                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                    builder.Ignore(property.PropertyType.GetGenericArguments()[0]);
            builder.Entity<AccountingReminder>();
            builder.Entity<AccountingReminderDelivery>().HasIndex(x => new { x.ReminderId, x.DueDate, x.ReminderDate }).IsUnique();
        }
    }

    private sealed class Access : IAccountingAccess
    {
        public bool Allowed = true;
        public Task<AccountingAccess?> GetAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> AllowsAsync(Guid userId, string permission, CancellationToken ct = default) => Task.FromResult(Allowed);
    }
    private sealed class Push : IPushNotificationService
    {
        public int Calls;
        public bool Result;
        public Guid UserId;
        public IReadOnlyDictionary<string, string>? Data;
        public Task<bool> SendAsync(Guid userId, string title, string body, IReadOnlyDictionary<string, string>? data = null, CancellationToken cancellationToken = default)
        { Calls++; UserId = userId; Data = data; return Task.FromResult(Result); }
    }
    private sealed class Repository : IAccountingReminderRepository
    {
        public int Writes;
        public int Scans;
        public Guid LastUser;
        public bool Pending = true;
        public bool Delivered;
        public AccountingReminderTarget Target = new() { Id = 1, TargetId = 7, UserId = Guid.NewGuid(), Kind = AccountingReminderKind.Cheque,
            DueDate = new DateTime(2026, 11, 23, 20, 30, 0, DateTimeKind.Utc), Amount = 1000, PatientName = "بیمار" };
        public Task<IReadOnlyList<long>> EnabledAsync(Guid userId, AccountingReminderKind kind, long[] ids, CancellationToken ct) => Task.FromResult<IReadOnlyList<long>>([]);
        public Task<bool> IsPendingAsync(AccountingReminderKind kind, long targetId, CancellationToken ct) => Task.FromResult(Pending);
        public Task SetAsync(Guid userId, AccountingReminderKind kind, long targetId, bool enabled, CancellationToken ct) { Writes++; LastUser = userId; return Task.CompletedTask; }
        public Task<IReadOnlyList<AccountingReminderTarget>> DueAsync(DateTime localDate, long afterId, CancellationToken ct)
        { Scans++; return Task.FromResult<IReadOnlyList<AccountingReminderTarget>>(afterId == 0 ? [Target] : []); }
        public Task<bool> StillPendingAsync(AccountingReminderTarget target, CancellationToken ct) => Task.FromResult(Pending);
        public Task<bool> ClaimAsync(long reminderId, DateTime dueDate, DateTime reminderDate, Guid leaseId, DateTime utcNow, CancellationToken ct) => Task.FromResult(!Delivered);
        public Task CompleteAsync(Guid leaseId, bool sent, DateTime utcNow, CancellationToken ct) { Delivered = sent; return Task.CompletedTask; }
    }
}
