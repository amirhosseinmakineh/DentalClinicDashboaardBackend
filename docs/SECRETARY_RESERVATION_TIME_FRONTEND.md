# قرارداد زمان رزرو در داشبورد منشی

## خلاصه

فیلدهای `reservationAt` و `appointmentDateTime` در پاسخ API، **ساعت محلی ایران** را نشان می‌دهند. بک‌اند نیز زمان رزرو را به‌صورت ساعت محلی ایران ذخیره و برمی‌گرداند. از اعمال دستی اختلاف `+03:30` یا `-03:30` روی پاسخ خودداری کنید.

مسیر تغییر زمان توسط منشی:

```http
POST /api/Reservation/SecretaryReservations/{reservationId}/time
Content-Type: application/json
```

## بدنه پیشنهادی درخواست

تنها یک منبع زمان بسازید و همان مقدار را در هر دو نام سازگار API بفرستید:

```json
{
  "reservationAt": "2026-08-22T21:00:00",
  "appointmentDateTime": "2026-08-22T21:00:00",
  "dentalServices": [1]
}
```

رشته بدون `Z` در این قرارداد یعنی ساعت محلی ایران. بک‌اند برای سازگاری با فرانت‌های فعلی، مقدار UTC تولیدشده با `toISOString()` را هم می‌پذیرد و پیش از ذخیره به ساعت ایران برمی‌گرداند. برای مثال `2026-08-22T17:30:00Z` به‌عنوان ساعت `21:00` ایران ثبت می‌شود.

## نکات الزامی فرانت‌اند

1. برای نمایش جدول و مقدار اولیه مودال، مستقیماً از `reservationAt` (یا `appointmentDateTime`) پاسخ موفق همان درخواست استفاده کنید.
2. پس از پاسخ موفق، رکورد موجود در state را با `response.data.data` جایگزین کنید؛ ساعت جاری سیستم یا `updatedAt` زمان رزرو نیست.
3. روی رشته بدون timezone، تبدیل دستی timezone انجام ندهید. برای نمایش فقط بخش ساعت می‌توان رشته را مستقیماً خواند.
4. اگر هر دو فیلد ارسال می‌شوند باید دقیقاً یک زمان را بیان کنند؛ در غیر این صورت API خطای اعتبارسنجی برمی‌گرداند.
5. بعد از دریافت رویداد SignalR با نام `ReservationUpdated`، از `reservationAt` یا آبجکت `reservation` داخل رویداد استفاده کنید و لیست را به‌روزرسانی کنید.

نمونه ساده TypeScript برای تولید مقدار محلی بدون تبدیل UTC:

```ts
function toLocalDateTime(date: string, time: string): string {
  // date: YYYY-MM-DD, time: HH:mm
  return `${date}T${time}:00`;
}

const appointment = toLocalDateTime(selectedGregorianDate, selectedTime24Hour);
const body = {
  reservationAt: appointment,
  appointmentDateTime: appointment,
  dentalServices: selectedDentalServices,
};
```

در ورودی `time` حتماً مقدار کنترل HTML را در قالب ۲۴ ساعته (`21:00`) بخوانید؛ متن نمایشی `09:00 PM` را مستقیماً تجزیه نکنید.
