# راهنمای فرانت: جداسازی لیست لیدهای جدید از گزارش‌های ثبت‌شده

## هدف تغییر

از این پس صفحه/بخشی که برای **ثبت گزارش جدید** است نباید از API عمومی
`GetLeads` استفاده کند. endpoint جدید فقط لیدهایی را برمی‌گرداند که:

- به مشاور مشخص‌شده با `ProfileId` تخصیص داده شده‌اند (لید توسط همان مشاور برداشته شده است)؛
- حذف نشده‌اند؛
- هنوز گزارشی برای آن‌ها ثبت نشده است (`IsReportSubmitted = false` و
  `ReportSubmittedAt = null`)؛
- تمام فیلترهای ارسال‌شده و صفحه‌بندی فعلی `GetLeads` را نیز رعایت می‌کنند.

API فعلی `GetLeads` تغییر رفتار نداده و باید در صفحه **لیست/ویرایش گزارش‌ها** باقی
بماند.

## endpoint جدید

```http
GET /api/Consultant/GetNewLeads
```

### Query parameters

پارامترها با `GetLeads` یکسان‌اند:

| پارامتر | نوع | توضیح |
| --- | --- | --- |
| `ProfileId` | `number` | شناسه پروفایل مشاور (الزامی برای دریافت لیدهای همان مشاور) |
| `leadAssignmentState` | `number?` | فیلتر وضعیت تخصیص |
| `LeadAssignmentType` | `number?` | فیلتر نوع لید |
| `SearchText` | `string?` | جست‌وجو در نام و شماره‌های تماس |
| `UserName` | `string?` | فیلتر نام بیمار |
| `PhoneNumber` | `string?` | فیلتر شماره اصلی یا دوم |
| `PatientCity` | `string?` | فیلتر شهر بیمار |
| `Date` | `YYYY-MM-DD?` | فیلتر تاریخ تخصیص |
| `From` / `To` | ISO date-time | بازه تاریخ تخصیص |
| `FromDate` / `ToDate` | ISO date-time | نام‌های قدیمی و سازگار با نسخه‌های فعلی فرانت |
| `PageNumber` | `number` | پیش‌فرض `1` |
| `PageSize` | `number` | پیش‌فرض `10` |

> فرانت نباید `HasSubmittedReport` را برای endpoint جدید ارسال کند. حتی اگر مقدار
> `true` ارسال شود، بک‌اند آن را نادیده می‌گیرد و فقط لیدهای بدون گزارش را برمی‌گرداند.

### نمونه درخواست

```http
GET /api/Consultant/GetNewLeads?ProfileId=42&PageNumber=1&PageSize=10&SearchText=رضا
```

ساختار پاسخ و آیتم‌ها دقیقاً مشابه پاسخ `GetLeads` است؛ بنابراین model و منطق
pagination فعلی فرانت قابل استفاده مجدد است.

## تغییر پیشنهادی در service فرانت

در سرویسی که در حال حاضر `GetLeads` را صدا می‌زند، متد جداگانه‌ای اضافه کنید:

```ts
getNewLeads(params: GetLeadsParams) {
  const { hasSubmittedReport, ...newLeadParams } = params;

  return this.http.get<PaginatedResult<LeadAssignmentItem>>(
    `${environment.apiUrl}/api/Consultant/GetNewLeads`,
    { params: this.toHttpParams(newLeadParams) }
  );
}
```

سپس مصرف APIها را به این شکل تفکیک کنید:

1. **لیست ثبت گزارش جدید:** `getNewLeads(...)` / `GetNewLeads`
2. **لیست گزارش‌ها و ویرایش گزارش:** متد فعلی / `GetLeads`

فیلترها، pagination، loading و error handling موجود نیازی به تغییر ندارند؛ تنها
endpoint صفحه ثبت گزارش جدید عوض می‌شود. بعد از ثبت موفق گزارش، همان صفحه را دوباره
با `GetNewLeads` بارگذاری کنید تا لید گزارش‌شده به‌صورت طبیعی از لیست حذف شود.
