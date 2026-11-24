using System.Reflection;
using System.Text.Json;
using DentalDashboard.ApplicationService.Accounting.Services;
using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.Entities;
using DentalDashboard.Domain.Secretary.Accountant.Enums;
using DentalDashboard.Infrastracture.Accounting.Auditing;
using DentalDashboard.Infrastracture.Accounting.Repositories;
using DentalDashboard.Infrastracture.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class AccountingTests
{
    [Fact]
    public void AllAccountingFiltersTranslateToSqlServerIncludingOptionalColumns()
    {
        using var db = new DentalContextDesignTimeFactory().CreateDbContext([]);
        AccountingReadRepository[] repositories = [new PatientAccountingRepository(db), new ClinicAccountingRepository(db), new StaffAccountingRepository(db)];
        foreach (var section in Enum.GetValues<AccountingSection>()) {
            var repository = Assert.Single(repositories, x => x.Supports(section));
            var rowsMethod = repository.GetType().GetMethod("Rows", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var rows = (IQueryable<AccountingRow>)rowsMethod.Invoke(repository, [section])!;
            var query = AccountingReadRepository.Filter(rows, new() { Search = "test", Status = "1", FromDate = new(2026, 1, 1),
                ToDate = new(2026, 12, 1), UserId = Guid.Empty, CaseId = Guid.Empty });
            var sql = query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).Skip(20).Take(20).ToQueryString();
            Assert.Contains("SELECT", sql);
        }
    }

    [Fact]
    public async Task PermissionIsCheckedBeforeTheFinancialExecutorAndAuditScopeIsCleared()
    {
        var executor = new Executor();
        var access = new Access();
        var audit = new AccountingAuditScope();
        var service = new AccountingOperationService(access, [executor], audit);
        var command = new AccountingOperationCommand(new(Guid.NewGuid()), "Test", new("1", JsonSerializer.SerializeToElement(new { })));
        await Assert.ThrowsAsync<AccountingForbiddenException>(() => service.ExecuteAsync(command, default));
        Assert.Equal(0, executor.Calls);
        access.Allowed = true;
        await service.ExecuteAsync(command, default);
        Assert.Equal(1, executor.Calls);
        Assert.Null(audit.ActorUserId);
        executor.Fail = true;
        await Assert.ThrowsAsync<AccountingValidationException>(() => service.ExecuteAsync(command, default));
        Assert.Null(audit.ActorUserId);
        Assert.Equal("", audit.Operation);
    }

    [Fact]
    public void InvalidPaginationDatesAndSpreadsheetFormulaInputAreHandled()
    {
        Assert.Throws<AccountingValidationException>(() => AccountingFilterRules.Validate(new() { Page = 0 }));
        Assert.Throws<AccountingValidationException>(() => AccountingFilterRules.Validate(new() { FromDate = new(2026, 2, 1), ToDate = new(2026, 1, 1) }));
        Assert.Throws<AccountingValidationException>(() => AccountingPayload.Read<object>(JsonSerializer.SerializeToElement<object?>(null)));
        Assert.StartsWith("\"'", AccountingExportService.CsvCell("=HYPERLINK(\"bad\")"));
    }

    [Fact]
    public async Task AuditUsesGeneratedIdentityAndRollsBackWithTheFinancialWrite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var scope = new AccountingAuditScope();
        var options = new DbContextOptionsBuilder<AuditTestContext>().UseSqlite(connection)
            .AddInterceptors(new AccountingAuditInterceptor(scope)).Options;
        await using var db = new AuditTestContext(options);
        await db.Database.EnsureCreatedAsync();
        var category = new ExpenseCategory { Title = "first" };
        scope.ActorUserId = Guid.NewGuid(); scope.Operation = "CreateCategory";
        db.Add(category);
        await db.SaveChangesAsync();
        var audit = await db.Audits.SingleAsync();
        Assert.Equal(category.Id.ToString(), audit.EntityId);
        Assert.Contains("first", audit.ChangesJson);
        await using (var tx = await db.Database.BeginTransactionAsync()) {
            category.Title = "rolled back";
            scope.Operation = "UpdateCategory";
            await db.SaveChangesAsync();
            Assert.Equal(2, await db.Audits.CountAsync());
            await tx.RollbackAsync();
        }
        db.ChangeTracker.Clear();
        Assert.Equal("first", (await db.Categories.SingleAsync()).Title);
        Assert.Equal(1, await db.Audits.CountAsync());
    }

    [Fact]
    public async Task AuditFailureDoesNotLeaveAPartialFinancialRecord()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var scope = new AccountingAuditScope { ActorUserId = Guid.NewGuid(), Operation = "CreateCategory" };
        var options = new DbContextOptionsBuilder<AuditTestContext>().UseSqlite(connection)
            .AddInterceptors(new AccountingAuditInterceptor(scope), new RejectAudit()).Options;
        await using var db = new AuditTestContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Add(new ExpenseCategory { Title = "must rollback" });
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Categories.CountAsync());
        Assert.Equal(0, await db.Audits.CountAsync());
    }

    private sealed class AuditTestContext(DbContextOptions<AuditTestContext> options) : DbContext(options)
    {
        public DbSet<ExpenseCategory> Categories => Set<ExpenseCategory>();
        public DbSet<AccountingAuditEntry> Audits => Set<AccountingAuditEntry>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExpenseCategory>().Ignore(x => x.FinancialTransactions);
        }
    }
    private sealed class RejectAudit : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<AccountingAuditEntry>().Any(x => x.State == EntityState.Added))
                throw new InvalidOperationException("simulate audit storage failure");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class Access : IAccountingAccess
    {
        public bool Allowed;
        public Task<AccountingAccess?> GetAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<AccountingAccess?>(new(false, []));
        public Task<bool> AllowsAsync(Guid userId, string permission, CancellationToken ct = default) => Task.FromResult(Allowed);
    }
    private sealed class Executor : IAccountingOperationExecutor
    {
        public int Calls;
        public bool Fail;
        public IReadOnlyDictionary<string, string> Permissions { get; } = new Dictionary<string, string> { ["Test"] = AccountingPermissions.Collect };
        public Task<AccountingOperationResponse> ExecuteAsync(string operation, string? targetId, JsonElement data, Guid actorUserId, CancellationToken ct)
        {
            Calls++;
            if (Fail) throw new AccountingValidationException("test failure");
            return Task.FromResult(new AccountingOperationResponse("done"));
        }
    }
}
