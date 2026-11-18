namespace DentalDashboard.Domain.Models;

public sealed class ConsultantServiceRewardRule
{
    public long Id { get; set; }
    public int ServiceId { get; set; }
    public string Basis { get; set; } = "Unit";
    public int UnitsPerReward { get; set; } = 1;
    public string RemainderMode { get; set; } = "FullBlocks";
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
}

// Execution identity, rather than a count entered by the client, prevents rewards being repeated at later visits.
public sealed class ConsultantRewardExecution
{
    public long Id { get; set; }
    public Guid FinancialCaseId { get; set; }
    public long ReservationId { get; set; }
    public long ConsultantProfileId { get; set; }
    public int ServiceId { get; set; }
    public int ToothId { get; set; }
    public DateTime PerformedAtUtc { get; set; }
}

public sealed record ServiceRewardLine(int ServiceId, string ServiceName, int[] Teeth, long? RuleId,
    string Basis, int UnitsPerReward, string RemainderMode, decimal Rate, int PreviousUnits,
    int NewUnits, decimal Amount);
public sealed record ServiceRewardSnapshot(long PatientFileId, Guid FinancialCaseId, long ConsultantProfileId,
    DateTime CalculatedAtUtc, List<ServiceRewardLine> Lines)
{
    public decimal Total => Lines.Sum(x => x.Amount);
}

public static class ConsultantServiceRewardCalculator
{
    public static decimal Calculate(ConsultantServiceRewardRule rule, IReadOnlyCollection<int> previous,
        IReadOnlyCollection<int> current)
    {
        if (ValidateRule(rule) is { } error) throw new ArgumentException(error);
        if (!rule.IsActive) return 0;
        if (current.Any(x => x is < 1 or > 32) || previous.Any(x => x is < 1 or > 32) ||
            current.Distinct().Count() != current.Count || previous.Distinct().Count() != previous.Count || previous.Intersect(current).Any())
            throw new ArgumentException("خدمت یک دندان قبلاً ثبت شده یا شماره دندان معتبر نیست.");
        var after = previous.Concat(current).Distinct().ToArray();
        decimal blocks;
        if (rule.Basis == "Jaw") {
            int CountJaws(IEnumerable<int> teeth) => teeth.GroupBy(x => x <= 16)
                .Count(g => g.Count() >= rule.UnitsPerReward);
            blocks = CountJaws(after) - CountJaws(previous);
        } else if (rule.RemainderMode == "Proportional") {
            // Difference of cumulative rounded amounts prevents rounding drift across visits.
            return decimal.Round(after.Length * rule.Amount / rule.UnitsPerReward, 2) -
                decimal.Round(previous.Distinct().Count() * rule.Amount / rule.UnitsPerReward, 2);
        } else {
            blocks = after.Length / rule.UnitsPerReward - previous.Distinct().Count() / rule.UnitsPerReward;
        }
        return decimal.Round(blocks * rule.Amount, 2);
    }

    public static string? ValidateRule(ConsultantServiceRewardRule rule) =>
        !Enum.IsDefined(typeof(DentalDashboard.Domain.Enums.DentalServiceType), rule.ServiceId) ||
        rule.Basis is not ("Unit" or "Block" or "Jaw") || rule.RemainderMode is not ("FullBlocks" or "Proportional") ||
        rule.UnitsPerReward < 1 || rule.UnitsPerReward > (rule.Basis == "Jaw" ? 16 : 32) ||
        (rule.Basis == "Unit" && rule.UnitsPerReward != 1) ||
        rule.Amount < 0 || rule.Amount > 999_999_999_999m || decimal.Round(rule.Amount, 2) != rule.Amount
            ? "نوع خدمت، مبنای محاسبه، تعداد واحد یا مبلغ پاداش معتبر نیست." : null;

    public static List<ConsultantServiceRewardRule> ResolveTreatmentRules(IEnumerable<ConsultantServiceRewardRule> versions,
        IReadOnlyDictionary<int, long?> frozenVersions)
    {
        var all = versions.ToList();
        var rules = all.GroupBy(x => x.ServiceId).Select(g => g.OrderByDescending(x => x.Id).First()).ToList();
        foreach (var (service, versionId) in frozenVersions) {
            rules.RemoveAll(x => x.ServiceId == service);
            if (versionId.HasValue && all.FirstOrDefault(x => x.Id == versionId && x.ServiceId == service) is { } frozen)
                rules.Add(frozen);
        }
        return rules;
    }
}
