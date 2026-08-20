# راهنمای فرانت‌اند: ویرایش رزرو در داشبورد مشاور

## خلاصه تغییر

برای ویرایش امن رزرو از داشبورد مشاور، endpoint جدید زیر اضافه شده است:

```http
PUT /api/Reservation/ConsultantReservations/{reservationId}
Authorization: Bearer <access-token>
Content-Type: application/json
```

در این API فرانت‌اند **نباید** `consultantProfileId` را ارسال کند. بک‌اند پروفایل مشاور را از توکن کاربر لاگین‌شده پیدا می‌کند و به همین دلیل یک مشاور نمی‌تواند رزرو مشاور دیگری را ویرایش کند.

## نمایش دکمه ویرایش

در خروجی API لیست رزروهای مشاور:

```http
GET /api/Reservation/GetConsultantReservations?pageNumber=1&pageSize=10
Authorization: Bearer <access-token>
```

این API نیز احراز هویت می‌شود و بک‌اند شناسه پروفایل مشاور را از توکن استخراج می‌کند؛ بنابراین ارسال
`consultantProfileId` لازم نیست و اگر هم ارسال شود نادیده گرفته خواهد شد. در پاسخ `401` کاربر باید
دوباره وارد شود و پاسخ `403` یعنی برای کاربر، پروفایل فعال مشاور پیدا نشده است.

فیلد بولی `canEdit` به هر آیتم اضافه شده است. دکمه «ویرایش» فقط وقتی نمایش داده یا فعال شود که مقدار این فیلد `true` باشد.

```json
{
  "id": 125,
  "reservationId": 125,
  "reservationAt": "2026-08-25T10:30:00",
  "patientName": "نام بیمار",
  "patientCity": "تهران",
  "patientRegion": "منطقه ۲",
  "attendanceProbabilityPercent": 80,
  "attendancePrediction": "احتمال حضور بالا",
  "secondaryPhoneNumber": "09121111111",
  "description": "توضیحات رزرو",
  "isCanceled": false,
  "canEdit": true
}
```

`canEdit` برای رزرو کنسل‌شده و نیز رزروی که نتیجه آن توسط منشی تأیید یا رد شده باشد، `false` است.

## بدنه درخواست ویرایش

```json
{
  "reservationAt": "2026-08-25T11:00:00",
  "patientCity": "تهران",
  "patientRegion": "سعادت‌آباد",
  "attendanceProbabilityPercent": 85,
  "attendancePrediction": "بیمار حضور را تلفنی تأیید کرد",
  "secondaryPhoneNumber": "09121111111",
  "description": "ساعت رزرو با بیمار هماهنگ شد",
  "dentalServices": [1, 3]
}
```

| فیلد | نوع | توضیح |
| --- | --- | --- |
| `reservationAt` | `DateTime` | زمان رزرو؛ هنگام تغییر باید در آینده باشد |
| `appointmentDateTime` | `DateTime?` | نام جایگزین زمان رزرو؛ در حالت معمول ارسال نشود |
| `patientCity` | `string?` | شهر بیمار؛ اگر ارسال نشود مقدار قبلی حفظ می‌شود |
| `patientRegion` | `string?` | منطقه بیمار؛ اگر ارسال نشود مقدار قبلی حفظ می‌شود |
| `attendanceProbabilityPercent` | `int?` | عددی بین ۰ تا ۱۰۰ |
| `attendancePrediction` | `string?` | توضیح پیش‌بینی حضور |
| `secondaryPhoneNumber` | `string?` | شماره تماس دوم بیمار |
| `description` | `string?` | توضیحات رزرو |
| `dentalServices` | `DentalServiceType[]?` | خدمات دندان‌پزشکی انتخاب‌شده؛ در صورت ارسال باید حداقل یک مقدار معتبر داشته باشد |

> `reservationAt` در قرارداد فعلی الزامی است. فرم ویرایش باید مقدار فعلی آن را حتی وقتی ساعت عوض نشده ارسال کند.

## پاسخ موفق

تمام پاسخ‌های command در wrapper استاندارد سیستم برمی‌گردند:

```json
{
  "isSuccess": true,
  "message": "رزرو با موفقیت ویرایش شد",
  "data": {
    "id": 125,
    "reservationId": 125,
    "consultantProfileId": 3,
    "reservationAt": "2026-08-25T11:00:00",
    "appointmentDateTime": "2026-08-25T11:00:00",
    "patientCity": "تهران",
    "patientRegion": "سعادت‌آباد",
    "attendanceProbabilityPercent": 85,
    "canEdit": true
  }
}
```

بعد از موفقیت، آیتم جدول را با `data` جایگزین کنید یا لیست رزروها را دوباره دریافت کنید.

بک‌اند همچنین رویداد SignalR با نام `ReservationUpdated` را روی هاب `/hubs/reservations`
منتشر می‌کند. برای همگام‌سازی چند تب یا نمایش تغییر در سایر بخش‌های داشبورد، فرانت‌اند می‌تواند این
رویداد را گوش کند و با `reservation.reservationId` آیتم متناظر را جایگزین کند.

## وضعیت‌ها و خطاها

| وضعیت/نتیجه | رفتار پیشنهادی فرانت |
| --- | --- |
| `401 Unauthorized` | توکن موجود نیست یا شناسه کاربر از توکن خوانده نمی‌شود؛ انتقال به ورود |
| `403 Forbidden` | کاربر پروفایل فعال مشاور ندارد؛ نمایش پیام عدم دسترسی |
| HTTP `200` با `isSuccess: false` | متن `message` را به کاربر نمایش دهید و فرم را باز نگه دارید |

پیام‌های validation مهم عبارت‌اند از:

- `رزرو فعال یافت نشد`
- `این رزرو متعلق به شما نیست`
- `پس از بررسی منشی امکان ویرایش رزرو وجود ندارد`
- `زمان رزرو باید در آینده باشد`
- `ظرفیت این بازه زمانی برای مشاور تکمیل است`
- `شهر بیمار برای رزرو الزامی است`
- `منطقه بیمار برای رزرو الزامی است`
- `احتمال حضور باید بین ۰ تا ۱۰۰ باشد`

## نمونه فراخوانی TypeScript

```ts
type UpdateConsultantReservation = {
  reservationAt: string;
  patientCity?: string;
  patientRegion?: string;
  attendanceProbabilityPercent?: number;
  attendancePrediction?: string;
  secondaryPhoneNumber?: string;
  description?: string;
  dentalServices?: number[];
};

export async function updateConsultantReservation(
  reservationId: number,
  payload: UpdateConsultantReservation,
  token: string,
) {
  const response = await fetch(
    `/api/Reservation/ConsultantReservations/${reservationId}`,
    {
      method: "PUT",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(payload),
    },
  );

  if (response.status === 401 || response.status === 403) {
    throw new Error("دسترسی به ویرایش رزرو امکان‌پذیر نیست");
  }

  const result = await response.json();
  if (!result.isSuccess) throw new Error(result.message);
  return result.data;
}
```
