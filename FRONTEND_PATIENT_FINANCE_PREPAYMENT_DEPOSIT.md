# راهنمای فرانت‌اند: مبلغ پیش‌پرداخت و ودیعه بیمار

دو فیلد عددی زیر به پرونده مالی بیمار اضافه شده‌اند:

| فیلد JSON | نوع | عنوان پیشنهادی | توضیح |
|---|---|---|---|
| `prePaymentAmount` | `number` | مبلغ پیش‌پرداخت | مبلغ پیش‌پرداخت ثبت‌شده برای پرونده |
| `depositAmount` | `number` | مبلغ ودیعه | مبلغ ودیعه ثبت‌شده برای پرونده |

> واحد مبلغ همان واحد فعلی `totalAmount` در فرانت‌اند است. تبدیل ریال/تومان را مانند
> سایر مبلغ‌های حسابداری انجام دهید.

## ایجاد پرونده مالی

`POST /api/secretary/patient-financial-cases`

هر دو فیلد جدید را در body ارسال کنید. اگر مبلغی وجود ندارد، مقدار `0` ارسال شود.

```json
{
  "patientId": "7d9f31fd-a4d5-48e8-a278-ce2f6de18b03",
  "serviceId": 1,
  "totalAmount": 150000000,
  "prePaymentAmount": 30000000,
  "depositAmount": 10000000,
  "agreementType": 2,
  "cheques": [],
  "promissoryNotes": []
}
```

## ویرایش پرونده مالی

`PUT /api/secretary/patient-financial-cases/{id}`

این API مدل جایگزینی دارد؛ بنابراین فرانت باید هنگام ویرایش هر دو مقدار فعلی را
ارسال کند. حذف هر مبلغ با ارسال مقدار `0` انجام می‌شود.

```json
{
  "totalAmount": 150000000,
  "prePaymentAmount": 35000000,
  "depositAmount": 5000000,
  "agreementType": 2
}
```

## اعتبارسنجی

- `prePaymentAmount` و `depositAmount` نباید منفی باشند.
- مجموع این دو مبلغ نباید از `totalAmount` بیشتر باشد.
- قواعد قبلی `totalAmount`، `agreementType`، چک و سفته بدون تغییر باقی مانده‌اند.
- برای ورودی مبلغ، حداقل مقدار UI را `0` بگذارید و قبل از ارسال جداکننده‌های
  نمایشی عدد را حذف کنید.

## پاسخ APIها

فیلدهای جدید در آبجکت `PatientFinancialCaseDto` برگردانده می‌شوند؛ در نتیجه در
پاسخ لیست و جزئیات پرونده موجودند:

- `GET /api/secretary/patient-financial-cases`
- `GET /api/secretary/patient-financial-cases/{id}` (داخل فیلد `case`)

نمونه آیتم لیست:

```json
{
  "id": "b72f17b4-482b-4b78-baf1-2688eb54bf64",
  "patientId": "7d9f31fd-a4d5-48e8-a278-ce2f6de18b03",
  "totalAmount": 150000000,
  "prePaymentAmount": 30000000,
  "depositAmount": 10000000,
  "totalPaidAmount": 0,
  "remainingAmount": 150000000,
  "agreementType": 1,
  "status": 1
}
```

مبالغ جدید اطلاعات توافق پرونده هستند و در این تغییر به‌عنوان تراکنش پرداختی
محاسبه نمی‌شوند؛ بنابراین محاسبه `totalPaidAmount` و `remainingAmount` API تغییر
نکرده است. برای نمایش مانده، همیشه مقادیر برگشتی API را مبنا قرار دهید.

## مدل پیشنهادی TypeScript

```ts
export interface PatientFinancialCase {
  id: string;
  patientId: string;
  totalAmount: number;
  prePaymentAmount: number;
  depositAmount: number;
  totalPaidAmount: number;
  remainingAmount: number;
  agreementType: 1 | 2;
  status: 1 | 2 | 3;
}

export interface SavePatientFinancialCaseRequest {
  totalAmount: number;
  prePaymentAmount: number;
  depositAmount: number;
  agreementType: 1 | 2;
}
```

برای سازگاری موقت با داده cache‌شده از نسخه قبلی فرانت می‌توان هنگام خواندن از
`prePaymentAmount ?? 0` و `depositAmount ?? 0` استفاده کرد.
