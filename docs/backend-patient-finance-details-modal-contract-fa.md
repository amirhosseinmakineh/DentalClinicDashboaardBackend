# قرارداد Backend برای مودال «جزئیات حسابداری بیمار»

این سند قرارداد پیاده‌سازی‌شده برای مودال جزئیات پرونده مالی بیمار را خلاصه می‌کند.

## Endpointها

هر دو درخواست به JWT معتبر نیاز دارند و شناسه مسیر، شناسه `PatientFinancialCase` است:

```http
GET /api/secretary/patient-financial-cases/{caseId}
GET /api/secretary/patient-financial-cases/{caseId}/summary
Authorization: Bearer <access-token>
```

پاسخ موفق هر دو endpoint در `ApiResult<T>` قرار می‌گیرد. پرونده ناموجود با HTTP 404 و `isSuccess = false` برگردانده می‌شود.

## جزئیات پرونده

داده جزئیات شامل `case`، تعداد و مجموع چک/سفته‌های غیرلغوشده و آرایه‌های `cheques` و `promissoryNotes` است. شیء `case` فیلدهای زیر را برای مودال فراهم می‌کند:

- `patientName`، `patientFileNumber` و `patientPhoneNumber`؛
- `serviceId` و نام فارسی `serviceName`؛
- `totalAmount`، `prePaymentAmount` و `depositAmount`؛
- `totalPaidAmount`، `remainingAmount` و `totalDebtAmount`؛
- `agreementType`، `status` و `createdAt`.

آرایه‌ها در نبود داده خالی‌اند و بر اساس `dueDate` و سپس `id` مرتب می‌شوند.

## خلاصه مالی

کارت‌های مودال فقط مقادیر زیر را از endpoint خلاصه می‌خوانند:

| کارت | property |
|---|---|
| مبلغ کل | `totalAmount` |
| پرداخت قطعی | `totalPaidAmount` |
| مانده | `remainingAmount` |
| بدهی باز | `totalDebtAmount` |

`totalPaidAmount` فقط مجموع تراکنش‌های نوع `Payment` است. `remainingAmount` برابر `max(totalAmount - totalPaidAmount, 0)` است. `totalDebtAmount` فقط بدهی‌های `Unpaid` را شامل می‌شود. مجموع چک‌ها و سفته‌ها رکوردهای `Cancelled` را شامل نمی‌شود.

تمام مبلغ‌ها عدد غیرمنفی، بدون قالب نمایشی و با واحد تومان هستند. قالب فارسی و عبارت «تومان» را Frontend اضافه می‌کند.
