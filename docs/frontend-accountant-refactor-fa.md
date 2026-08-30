# راهنمای تغییرات Frontend پس از بازآرایی Accountant

## نتیجه کوتاه

تغییر پوشه‌ها و namespaceهای Backend به `Secretary.Accountant` به‌تنهایی هیچ تغییری در Frontend لازم ندارد. URL، متد HTTP و ساختار درخواست endpointها تغییر نکرده‌اند.

تنها تغییر قابل مشاهده در قرارداد JSON مربوط به خروجی پرونده مالی بیمار است: فیلد تکراری `userId` از `PatientFinancialCaseDto` حذف شده است. از این پس `patientId` همان `Users.Id` بیمار و تنها شناسه معتبر بیمار برای APIهای حسابداری است.

## اقدام لازم در Frontend

اگر Frontend در پاسخ endpointهای زیر از `userId` استفاده می‌کند، آن را با `patientId` جایگزین کنید:

```http
GET /api/secretary/patient-financial-cases
GET /api/secretary/patient-financial-cases/{id}
```

### قرارداد قبلی

```ts
interface PatientFinancialCaseDto {
  id: string;
  patientId: string;
  userId: string; // تکراری؛ حذف شده است
  patientName: string;
  patientPhoneNumber: string | null;
  // سایر فیلدهای مالی بدون تغییر
}
```

### قرارداد فعلی

```ts
interface PatientFinancialCaseDto {
  id: string;
  patientId: string; // دقیقاً شناسه GUID رکورد بیمار در Users
  patientName: string;
  patientPhoneNumber: string | null;
  serviceId: number;
  serviceName: string;
  totalAmount: number;
  totalPaidAmount: number;
  remainingAmount: number;
  totalDebtAmount: number;
  agreementType: number;
  status: number;
  createdAt: string;
}
```

نمونه اصلاح مصرف داده:

```ts
// قبل
const patientUserId = financialCase.userId;

// اکنون
const patientUserId = financialCase.patientId;
```

## مواردی که تغییر نکرده‌اند

- هیچ route یا HTTP method تغییر نکرده است.
- پارامتر `patientId` در body و query همچنان با همین نام ارسال می‌شود.
- `patientId` از نوع GUID/string است و باید شناسه `Users.Id` کاربری باشد که نقش `Patient` دارد.
- شناسه کاربر لاگین‌شده/منشی همچنان از JWT استخراج می‌شود و نباید از Frontend در body ارسال شود.
- enumها، محاسبات مالی، pagination و ساختار Result تغییری نکرده‌اند.
- حذف migrationهای قدیمی هیچ اثری روی قرارداد Frontend ندارد.

## چک‌لیست مهاجرت Frontend

1. در typeها و interfaceها عبارت `userId` را برای `PatientFinancialCaseDto` جست‌وجو کنید.
2. مصرف آن را به `patientId` تغییر دهید.
3. اگر type از روی OpenAPI تولید می‌شود، client را دوباره generate کنید.
4. تست کنید که بازکردن جزئیات مالی و لینک‌دادن به بیمار با `patientId` انجام می‌شود.
5. هیچ URL جدیدی با عبارت `accountant` نسازید؛ routeهای عمومی API عمداً ثابت مانده‌اند.
