# مستند فیلتر نوع ثبت‌کننده گزارش رزروهای روزانه ادمین

## هدف و جریان کاربری

گزارش نمایشی و فایل خروجی رزروهای روزانه از فیلترهای مشترک `date`،
`reservationOwnerType`، شناسه ثبت‌کننده و `requestStatus` استفاده می‌کنند. با تغییر
نوع ثبت‌کننده، کلاینت باید شناسه گروه قبلی را پاک کند.

## منابع فهرست‌ها

```http
GET /api/Consultant/GetConsultants?pageNumber=1&pageSize=500
GET /api/admin/secretaries?pageNumber=1&pageSize=500
Authorization: Bearer <token>
```

شناسه مشاور `profileId` و شناسه منشی `id` است. هر دو endpoint فهرست برای ادمین
مجاز هستند.

## قرارداد API گزارش

```http
GET /api/admin/reports/daily-reservations
GET /api/admin/reports/daily-reservations/export
```

| پارامتر | نوع | توضیح |
| --- | --- | --- |
| `date` | `string` | تاریخ ثبت رزرو با قالب `YYYY-MM-DD` و مرز روز ایران |
| `reservationOwnerType` | `Consultant \| Secretary` | نوع ثبت‌کننده رزرو |
| `consultantProfileId` | `number` | فقط برای یک مشاور |
| `secretaryUserId` | `string (UUID)` | فقط برای یک منشی |
| `requestStatus` | `number` | وضعیت اختیاری درخواست |
| `includeAll` | `boolean` | نمایش همه رزروها و نادیده‌گرفتن سایر فیلترها |

### نمونه‌ها

```http
GET /api/admin/reports/daily-reservations?date=2026-08-17&reservationOwnerType=Consultant
GET /api/admin/reports/daily-reservations?date=2026-08-17&reservationOwnerType=Consultant&consultantProfileId=42
GET /api/admin/reports/daily-reservations?date=2026-08-17&reservationOwnerType=Secretary
GET /api/admin/reports/daily-reservations/export?date=2026-08-17&reservationOwnerType=Secretary&secretaryUserId=00000000-0000-0000-0000-000000000001
```

## قواعد بک‌اند

- تاریخ روی زمان ثبت رزرو و بر اساس مرز روز ایران اعمال می‌شود.
- نبود شناسه فرد به معنی همه اعضای نوع انتخاب‌شده است.
- ارسال `secretaryUserId` با نوع `Consultant` یا `consultantProfileId` با نوع
  `Secretary` پاسخ `400` برمی‌گرداند.
- query، مرتب‌سازی و summary برای گزارش و CSV مشترک است.
- با `includeAll=true` همه فیلترهای دیگر نادیده گرفته می‌شوند.
- رزروهای جدید، نوع و شناسه کاربر ثبت‌کننده را هنگام ایجاد ذخیره می‌کنند؛ داده‌های
  قدیمی که پیش از migration ایجاد شده‌اند ثبت‌کننده مشخصی ندارند.
