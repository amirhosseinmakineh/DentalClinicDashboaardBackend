using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public sealed record RewardConsultantOption(long Id, string Name);
public sealed record RewardToothOption(int ServiceId, string ServiceName, int ToothId, string ToothLabel);
public sealed record ServiceRewardContext(List<RewardConsultantOption> Consultants, long? LinkedConsultantProfileId,
    bool CanChooseConsultant, List<RewardToothOption> Options, List<ConsultantServiceRewardRule> Rules);
public sealed record PreparedServiceReward(ServiceRewardSnapshot Snapshot, string Hash);

public sealed class ServiceRewardPreparationService(DentalContext db)
{
    public static (long? Id, string? Error) ResolveRecipient(ServiceRewardContext context, long? requested)
    {
        var recipient = context.LinkedConsultantProfileId ?? requested;
        if (recipient is null || !context.Consultants.Any(x => x.Id == recipient))
            return (null, "مشاور دریافت‌کننده پاداش را از فهرست مشاوران فعال انتخاب کنید.");
        if (context.LinkedConsultantProfileId.HasValue && requested.HasValue && requested != context.LinkedConsultantProfileId)
            return (null, "مشاور متصل به این درمان قابل تغییر نیست.");
        return (recipient, null);
    }
    private static readonly Dictionary<string, int> Services = new() {
        ["composite"] = 1, ["implant"] = 2, ["laminate"] = 3, ["crown"] = 4,
        ["root"] = 5, ["filling"] = 6, ["extraction"] = 7
    };

    public static List<RewardToothOption> ReadChart(string? json)
    {
        var result = new List<RewardToothOption>();
        try {
            using var doc = JsonDocument.Parse(json ?? "{}");
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return result;
            foreach (var tooth in doc.RootElement.EnumerateObject()) {
                if (!int.TryParse(tooth.Name, out var id) || id is < 1 or > 32 || tooth.Name != id.ToString() ||
                    tooth.Value.ValueKind != JsonValueKind.Object) continue;
                var ids = new HashSet<int>();
                if (tooth.Value.TryGetProperty("services", out var array) && array.ValueKind == JsonValueKind.Array) {
                    foreach (var item in array.EnumerateArray())
                        if (item.ValueKind == JsonValueKind.String && Services.TryGetValue(item.GetString()!, out var service)) ids.Add(service);
                } else if (tooth.Value.TryGetProperty("service", out var item) && item.ValueKind == JsonValueKind.String &&
                    Services.TryGetValue(item.GetString()!, out var service)) ids.Add(service);
                var index = id <= 16 ? id - 1 : 32 - id;
                var number = index < 8 ? 8 - index : index - 7;
                var label = $"فک {(id <= 16 ? "بالا" : "پایین")} · {(index < 8 ? "راست" : "چپ")} · دندان {number}";
                foreach (var service in ids) result.Add(new(service, ((DentalServiceType)service).ToPersian(), id, label));
            }
        } catch (JsonException) { return []; }
        return result.OrderBy(x => x.ServiceId).ThenBy(x => x.ToothId).ToList();
    }

    public async Task<ServiceRewardContext> Context(PatientFileWorkflow workflow, Reservation reservation, CancellationToken ct)
    {
        var consultants = await db.ConsultantProfiles.AsNoTracking().Where(x => !x.IsDeleted && !x.User.IsDeleted && x.User.IsActive && x.User.UserRoles.Any(r => r.Role.RoleName == "Consultant"))
            .OrderBy(x => x.User.FirstName).ThenBy(x => x.Id)
            .Select(x => new RewardConsultantOption(x.Id, (x.User.FirstName + " " + x.User.LastName).Trim())).ToListAsync(ct);
        var previous = await db.ConsultantRewardExecutions.AsNoTracking().Where(x => x.FinancialCaseId == workflow.FinancialCaseId).ToListAsync(ct);
        long? linked = previous.OrderBy(x => x.Id).Select(x => (long?)x.ConsultantProfileId).FirstOrDefault() ??
            reservation.RewardConsultantProfileId ?? (reservation.OwnerType != ReservationOwnerType.Secretary &&
                consultants.Any(x => x.Id == reservation.ConsultantProfileId) ? reservation.ConsultantProfileId : null);
        var chart = await db.PatientPaperRecords.AsNoTracking().Where(x => x.PatientFileId == workflow.PatientFileId)
            .Select(x => x.DentalChartJson).SingleOrDefaultAsync(ct);
        var options = ReadChart(chart).Where(x => !previous.Any(p => p.ServiceId == x.ServiceId && p.ToothId == x.ToothId)).ToList();
        var versions = await db.ConsultantServiceRewardRules.AsNoTracking().ToListAsync(ct);
        var frozenVersions = new Dictionary<int, long?>();
        // Keep one rate/version per service within a treatment, including carry between visits.
        var firstExecutions = previous.GroupBy(x => x.ServiceId).Select(g => g.OrderBy(x => x.Id).First()).ToList();
        var reservationIds = firstExecutions.Select(x => x.ReservationId).Distinct().ToList();
        var snapshots = await db.Reservations.IgnoreQueryFilters().AsNoTracking().Where(x => reservationIds.Contains(x.Id))
            .Select(x => new { x.Id, x.ServiceRewardSnapshotJson }).ToDictionaryAsync(x => x.Id, ct);
        foreach (var execution in firstExecutions) {
            var snapshotJson = snapshots.GetValueOrDefault(execution.ReservationId)?.ServiceRewardSnapshotJson;
            var frozen = snapshotJson is null ? null : JsonSerializer.Deserialize<ServiceRewardSnapshot>(snapshotJson)?
                .Lines.SingleOrDefault(x => x.ServiceId == execution.ServiceId);
            frozenVersions[execution.ServiceId] = frozen?.RuleId;
        }
        var rules = ConsultantServiceRewardCalculator.ResolveTreatmentRules(versions, frozenVersions);
        return new(consultants, linked, linked is null, options, rules);
    }

    public async Task<(PreparedServiceReward? Reward, string? Error)> Prepare(PatientFileWorkflow workflow,
        Reservation reservation, ReviewReservationAttendanceCommand command, CancellationToken ct)
    {
        if (reservation.IsCanceled || reservation.IsDeleted || reservation.IsAttendanceScoreApplied ||
            reservation.PatientReceivedService.HasValue || reservation.ServiceRewardSnapshotJson != null ||
            reservation.ConsultantRewardApprovedByAdmin.HasValue)
            return (null, "نتیجه این مراجعه قبلاً ثبت شده یا مراجعه لغو شده است.");
        var context = await Context(workflow, reservation, ct);
        var recipient = ResolveRecipient(context, command.RewardConsultantProfileId);
        if (recipient.Error is not null) return (null, recipient.Error);
        if (command.PerformedServices is not { Count: > 0 and <= 7 } ||
            command.PerformedServices.Any(x => x is null || x.ToothIds is not { Count: > 0 and <= 32 } ||
                x.ToothIds.Distinct().Count() != x.ToothIds.Count) ||
            command.PerformedServices.Select(x => x.ServiceId).Distinct().Count() != command.PerformedServices.Count)
            return (null, "خدمات و دندان‌های واقعاً انجام‌شده را مشخص کنید.");
        var previous = await db.ConsultantRewardExecutions.AsNoTracking().Where(x => x.FinancialCaseId == workflow.FinancialCaseId).ToListAsync(ct);
        var lines = new List<ServiceRewardLine>();
        foreach (var service in command.PerformedServices.OrderBy(x => x.ServiceId)) {
            if (service.ToothIds.Any(id => !context.Options.Any(x => x.ServiceId == service.ServiceId && x.ToothId == id)))
                return (null, "دندان یا خدمت انتخاب‌شده در چارت نیست یا قبلاً انجام آن ثبت شده است؛ فرم را تازه کنید.");
            var teeth = service.ToothIds.Order().ToArray();
            var before = previous.Where(x => x.ServiceId == service.ServiceId).Select(x => x.ToothId).ToArray();
            var rule = context.Rules.SingleOrDefault(x => x.ServiceId == service.ServiceId && x.IsActive);
            if (rule is not null && ConsultantServiceRewardCalculator.ValidateRule(rule) is { } ruleError) return (null, ruleError);
            lines.Add(new(service.ServiceId, ((DentalServiceType)service.ServiceId).ToPersian(), teeth, rule?.Id,
                rule?.Basis ?? "None", rule?.UnitsPerReward ?? 1, rule?.RemainderMode ?? "FullBlocks", rule?.Amount ?? 0,
                before.Length, teeth.Length, rule is null ? 0 : ConsultantServiceRewardCalculator.Calculate(rule, before, teeth)));
        }
        var snapshot = new ServiceRewardSnapshot(workflow.PatientFileId, workflow.FinancialCaseId!.Value, recipient.Id!.Value, DateTime.UtcNow, lines);
        var stable = JsonSerializer.Serialize(new { snapshot.FinancialCaseId, snapshot.ConsultantProfileId, snapshot.Lines });
        return (new(snapshot, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(stable)))), null);
    }
}
