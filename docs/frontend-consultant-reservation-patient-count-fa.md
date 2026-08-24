# راهنمای فرانت‌اند: تعداد بیماران در رزرو مشاور

## خلاصه تغییر

در فرم ایجاد رزرو داشبورد مشاور، یک ورودی با عنوان **«تعداد بیماران»** اضافه کنید. نام فیلد API برابر `patientCount` و نوع آن عدد صحیح است. مقدار معتبر از **۱ تا ۱۰** است و مقدار پیشنهادی اولیه فرم `1` است.

این مقدار روی خود رزرو ذخیره می‌شود؛ بنابراین وقتی یک تماس برای دو بیمار است، مشاور می‌تواند فقط یک رزرو بسازد و `patientCount: 2` بفرستد.

## ایجاد رزرو

```http
POST /api/Reservation
Authorization: Bearer <access-token>
Content-Type: application/json
```

نمونه درخواست برای دو بیمار:

```json
{
  "leadAssignmentId": 123,
  "consultantProfileId": 45,
  "reservationAt": "2026-08-26T10:30:00",
  "patientCount": 2,
  "patientCity": "تهران",
  "patientRegion": "منطقه ۲",
  "dentalServices": [1]
}
```

- `patientCount` باید عدد صحیح بین `1` و `10` باشد.
- برای سازگاری با نسخه‌های قبلی، اگر این فیلد در ایجاد رزرو ارسال نشود بک‌اند مقدار `1` را ذخیره می‌کند.
- مقدار صفر، مقدار منفی یا بیشتر از ۱۰ با پاسخ ناموفق و پیام `تعداد بیماران باید بین ۱ تا ۱۰ نفر باشد` رد می‌شود.

در پاسخ موفق ایجاد نیز مقدار ثبت‌شده داخل `data.patientCount` برمی‌گردد:

```json
{
  "isSuccess": true,
  "message": "رزرو با موفقیت ثبت شد",
  "data": {
    "reservationId": 987,
    "appointmentDateTime": "2026-08-26T10:30:00",
    "patientCount": 2
  }
}
```

## دریافت و نمایش رزروها

در هر آیتم خروجی لیست رزروهای مشاور، `patientCount` وجود دارد:

```http
GET /api/Reservation/GetConsultantReservations?pageNumber=1&pageSize=10
Authorization: Bearer <access-token>
```

```json
{
  "reservationId": 987,
  "patientName": "بیمار نمونه",
  "patientCount": 2,
  "canEdit": true
}
```

در کارت یا ردیف رزرو، مقدار را مثلاً به شکل **«تعداد بیماران: ۲ نفر»** نمایش دهید. داده‌های قدیمی بعد از migration مقدار `1` خواهند داشت.

## ویرایش رزرو مشاور

```http
PUT /api/Reservation/ConsultantReservations/{reservationId}
Authorization: Bearer <access-token>
Content-Type: application/json
```

برای تغییر تعداد، `patientCount` را همراه اطلاعات فرم بفرستید:

```json
{
  "reservationAt": "2026-08-26T10:30:00",
  "patientCount": 3,
  "patientCity": "تهران",
  "patientRegion": "منطقه ۲",
  "dentalServices": [1]
}
```

در ویرایش، نبودن `patientCount` یا ارسال `null` مقدار قبلی را حفظ می‌کند. اگر مقدار ارسال شود، باید بین ۱ و ۱۰ باشد. پاسخ موفق ویرایش نیز مقدار نهایی را در `data.patientCount` برمی‌گرداند.

## پیشنهاد پیاده‌سازی فرم

```ts
type CreateReservationPayload = {
  leadAssignmentId: number;
  consultantProfileId: number;
  reservationAt: string;
  patientCount: number;
  patientCity?: string;
  patientRegion?: string;
  dentalServices: number[];
};

const initialValues = {
  patientCount: 1,
};

function validatePatientCount(value: number): string | undefined {
  if (!Number.isInteger(value) || value < 1 || value > 10) {
    return "تعداد بیماران باید بین ۱ تا ۱۰ نفر باشد";
  }
}
```

برای کنترل رابط کاربری می‌توان از `input[type="number"]` با `min="1"`، `max="10"` و `step="1"` یا یک select با گزینه‌های ۱ تا ۱۰ استفاده کرد. اعتبارسنجی سمت فرانت فقط برای تجربه بهتر است و جای اعتبارسنجی بک‌اند را نمی‌گیرد.

## استقرار بک‌اند

قبل از انتشار API، migration دیتابیس اجرا شود:

```bash
dotnet ef database update \
  --project DentalDashboard.Infrastracture \
  --startup-project DentalDashboard
```

migration ستون اجباری `PatientCount` را با مقدار پیش‌فرض `1` به جدول `Reservations` اضافه می‌کند.
