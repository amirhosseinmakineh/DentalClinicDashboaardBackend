using System.Text.Json;
using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.Domain.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace DentalDashboard.Infrastracture.Accounting.Auditing;

public sealed class AccountingAuditScope : IAccountingAuditScope
{
    public Guid? ActorUserId { get; set; }
    public string Operation { get; set; } = "";
}

// Audit is persisted in the SAME transaction as the financial write. Generated keys
// are read after the first save; a guarded second save records them before commit.
public sealed class AccountingAuditInterceptor(IAccountingAuditScope scope) : SaveChangesInterceptor
{
    private sealed record Pending(EntityEntry Entry, EntityState State, Dictionary<string, object?> Before, string[] Properties);
    private List<Pending> pending = [];
    private IDbContextTransaction? ownedTransaction;
    private bool recordingAudit;
    private static readonly HashSet<string> AuditedTypes = [
        "PatientFinancialCase", "PatientCheque", "PatientPromissoryNote", "PatientDebt", "PatientFinancialTransaction",
        "PatientInstallment", "FinancialTransaction", "ExpenseCategory", "SecretarySale", "SecretaryWallet",
        "SecretaryWalletTransaction", "PatientReferral", "PatientWallet", "PatientWalletTransaction", "Reservation", "AccountantPermission"
    ];
    private static readonly HashSet<string> Fields = [
        "Id", "UserId", "Permission", "GrantedByUserId", "Amount", "Balance", "Status", "IsDeleted", "IsActive", "Title", "Type",
        "TotalAmount", "PrePaymentAmount", "DepositAmount", "PaymentMethod", "AgreementType", "DueDate", "SayadNumber", "SerialNumber",
        "IsGuarantee", "TransactionDate", "ExpenseCategoryId", "PatientFinancialCaseId", "SourceType", "SourceId", "PaidAtUtc", "PaidByUserId",
        "SalePrice", "SecretaryReward", "ReviewedByAdminId", "ReviewedAt", "RewardAmount", "RejectionReason",
        "ConsultantRewardApprovedByAdmin", "ConsultantRewardAmount", "ConsultantRewardReviewedByAdminId", "ConsultantRewardReviewedAt"
    ];
    private void Capture(DbContext db)
    {
        db.ChangeTracker.DetectChanges();
        pending = db.ChangeTracker.Entries().Where(x => AuditedTypes.Contains(x.Metadata.ClrType.Name) &&
            x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).Select(x => {
                var fields = x.Properties.Where(p => Fields.Contains(p.Metadata.Name) &&
                    (x.State != EntityState.Modified || p.IsModified)).Select(p => p.Metadata.Name).ToArray();
                var before = fields.ToDictionary(p => p, p => x.State == EntityState.Added ? null : x.Property(p).OriginalValue);
                return new Pending(x, x.State, before, fields);
            }).ToList();
    }
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (recordingAudit || scope.ActorUserId is null || eventData.Context is not { } db) return result;
        Capture(db);
        if (pending.Count > 0 && db.Database.CurrentTransaction is null)
            ownedTransaction = await db.Database.BeginTransactionAsync(ct);
        return result;
    }
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        if (recordingAudit || scope.ActorUserId is not { } actor || eventData.Context is not { } db || pending.Count == 0) return result;
        var changes = pending;
        pending = [];
        recordingAudit = true;
        try {
            foreach (var change in changes) {
                var key = string.Join("/", change.Entry.Metadata.FindPrimaryKey()!.Properties.Select(p => change.Entry.Property(p.Name).CurrentValue));
                var after = change.Properties.ToDictionary(p => p, p => change.State == EntityState.Deleted ? null : change.Entry.Property(p).CurrentValue);
                db.Add(new AccountingAuditEntry { ActorUserId = actor, Operation = scope.Operation,
                    EntityType = change.Entry.Metadata.ClrType.Name, EntityId = key,
                    ChangesJson = JsonSerializer.Serialize(new { State = change.State.ToString(), Before = change.Before, After = after }) });
            }
            await db.SaveChangesAsync(ct);
            if (ownedTransaction is not null) await ownedTransaction.CommitAsync(ct);
        } catch {
            if (ownedTransaction is not null) await ownedTransaction.RollbackAsync(CancellationToken.None);
            throw;
        } finally {
            if (ownedTransaction is not null) await ownedTransaction.DisposeAsync();
            ownedTransaction = null;
            recordingAudit = false;
        }
        return result;
    }
    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken ct = default)
    {
        if (recordingAudit) return;
        pending = [];
        if (ownedTransaction is not null) {
            await ownedTransaction.RollbackAsync(CancellationToken.None);
            await ownedTransaction.DisposeAsync();
            ownedTransaction = null;
        }
    }
}
