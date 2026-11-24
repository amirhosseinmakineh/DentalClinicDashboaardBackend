using System.Text.Json;
using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Accounting.Services;

public static class AccountingPayload
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static T Read<T>(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object) throw new AccountingValidationException("اطلاعات فرم معتبر نیست.");
        try {
            return data.Deserialize<T>(JsonOptions) ?? throw new AccountingValidationException("اطلاعات عملیات الزامی است.");
        } catch (JsonException) { throw new AccountingValidationException("مقادیر فرم یا تاریخ واردشده معتبر نیست."); }
    }
    public static long LongId(string? value) => long.TryParse(value, out var id) && id > 0 ? id :
        throw new AccountingValidationException("شناسه معتبر نیست.");
    public static Guid GuidId(string? value) => Guid.TryParse(value, out var id) && id != Guid.Empty ? id :
        throw new AccountingValidationException("شناسه پرونده معتبر نیست.");
    public static AccountingOperationResponse Result(Result result, object? data = null)
    {
        if (!result.IsSuccess) throw new AccountingValidationException(result.Message);
        return new(string.IsNullOrWhiteSpace(result.Message) ? "عملیات با موفقیت ثبت شد." : result.Message, data);
    }
}
