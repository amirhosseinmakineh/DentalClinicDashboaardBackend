using System.Text.Json;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;

namespace DentalDashboard.Services;

public static class PatientChartFinanceRules
{
    private static readonly Dictionary<string, int> ServiceIds = new() {
        ["composite"] = 1, ["implant"] = 2, ["laminate"] = 3, ["crown"] = 4,
        ["root"] = 5, ["filling"] = 6, ["extraction"] = 7
    };

    public static Dictionary<int, int> ReadUnits(string? json)
    {
        var units = new Dictionary<int, int>();
        try {
            using var document = JsonDocument.Parse(json ?? "{}");
            if (document.RootElement.ValueKind != JsonValueKind.Object) return units;
            foreach (var tooth in document.RootElement.EnumerateObject()) {
                if (!int.TryParse(tooth.Name, out var id) || id is < 1 or > 32 || tooth.Name != id.ToString() ||
                    tooth.Value.ValueKind != JsonValueKind.Object) continue;
                var services = new HashSet<int>();
                if (tooth.Value.TryGetProperty("services", out var values) && values.ValueKind == JsonValueKind.Array) {
                    foreach (var value in values.EnumerateArray())
                        if (value.ValueKind == JsonValueKind.String && ServiceIds.TryGetValue(value.GetString()!, out var service)) services.Add(service);
                } else if (tooth.Value.TryGetProperty("service", out var value) && value.ValueKind == JsonValueKind.String &&
                    ServiceIds.TryGetValue(value.GetString()!, out var service)) services.Add(service);
                foreach (var service in services) units[service] = units.GetValueOrDefault(service) + 1;
            }
        } catch (JsonException) { return []; }
        return units;
    }

    public static bool SameUnits(IReadOnlyDictionary<int, int>? left, IReadOnlyDictionary<int, int> right) =>
        left is not null && left.Count == right.Count && right.All(x => left.TryGetValue(x.Key, out var count) && count == x.Value);

    public static string? ValidateSelection(IReadOnlyDictionary<int, int>? submitted, IReadOnlyDictionary<int, int> chart) =>
        chart.Count == 0 ? "ابتدا دندان‌ها و خدمات را در چارت پرونده ثبت کنید؛ سپس به ثبت مالی برگردید." :
        !SameUnits(submitted, chart) ? "چارت پرونده تغییر کرده است؛ فرم مالی را دوباره باز کنید. خدمات و تعداد واحد فقط از چارت خوانده می‌شوند." : null;

    public static void SetServices(PatientFinancialCase finance, Dictionary<int, int> units)
    {
        finance.Services = units.Keys.Order().Select(x => (DentalServiceType)x).ToList();
        finance.ServiceUnitCountsJson = JsonSerializer.Serialize(units.OrderBy(x => x.Key).ToDictionary());
        finance.ToothUnitCount = units.Values.Sum();
    }

    public static void ClearDependentApprovals(PatientFileWorkflow workflow)
    {
        workflow.FinanceConfirmedAtUtc = null;
        workflow.FinanceConfirmedByUserId = null;
        workflow.ProfileConfirmedAtUtc = null;
        workflow.ProfileConfirmedByUserId = null;
        workflow.ClearApproval();
    }

    public static string? RevisePlan(PatientFileWorkflow workflow, string chartJson, bool chartChanged, bool changed)
    {
        if (chartChanged && PatientFileWorkflowRules.HasFinance(workflow)) {
            var units = ReadUnits(chartJson);
            if (units.Count == 0) return "این پرونده صورت‌حساب دارد؛ چارت را اصلاح کنید، اما حداقل یک دندان با خدمت باید باقی بماند.";
            SetServices(workflow.FinancialCase!, units);
            workflow.FinancialCase!.UpdatedAt = DateTime.UtcNow;
        }
        if (changed) ClearDependentApprovals(workflow);
        return null;
    }
}
