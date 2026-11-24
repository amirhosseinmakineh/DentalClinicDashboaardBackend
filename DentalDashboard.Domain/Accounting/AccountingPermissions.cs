namespace DentalDashboard.Domain.Accounting;

public static class AccountingPermissions
{
    public const string View = "Accounting.View";
    public const string Export = "Accounting.Export";
    public const string EditPatients = "Accounting.EditPatients";
    public const string EditDocuments = "Accounting.EditDocuments";
    public const string Collect = "Accounting.Collect";
    public const string Cancel = "Accounting.Cancel";
    public const string ManageTransactions = "Accounting.ManageTransactions";
    public const string ManageCategories = "Accounting.ManageCategories";
    public const string ApproveSecretary = "Accounting.ApproveSecretary";
    public const string ApproveConsultant = "Accounting.ApproveConsultant";
    public const string ApproveReferral = "Accounting.ApproveReferral";
    public const string ViewAudit = "Accounting.ViewAudit";
    public static readonly IReadOnlyList<string> All = Array.AsReadOnly(new[] {
        View, Export, EditPatients, EditDocuments, Collect, Cancel, ManageTransactions,
        ManageCategories, ApproveSecretary, ApproveConsultant, ApproveReferral, ViewAudit
    });
}

public sealed class AccountantPermission
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public string Permission { get; set; } = "";
    public Guid GrantedByUserId { get; set; }
    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AccountingAuditEntry
{
    public long Id { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string Operation { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string ChangesJson { get; set; } = "{}";
}
