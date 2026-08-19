# راهنمای پیاده‌سازی فرانت‌اند دسترسی منشی کمکی

## قرارداد نقش و نوع منشی

در پاسخ `GET /api/User` برای هر کاربر علاوه بر `roleName`، فیلد `secretaryType` نیز وجود دارد:

- `1` یا `Main`: منشی اصلی؛ دسترسی کامل و بدون تنظیم روزانه.
- `2` یا `Assistant`: منشی کمکی؛ دسترسی وابسته به روز و Permission.
- `null`: کاربر نقش منشی ندارد.

در `PUT /api/User` وقتی `roleName` برابر `Secretary` است، `secretaryType` را ارسال کنید. با تغییر نقش به نقشی غیر از منشی، Backend این مقدار را پاک می‌کند.

## فرم ویرایش کاربر ادمین

1. انتخاب «نوع منشی» را فقط وقتی نقش `Secretary` است نشان دهید.
2. برای `Main` بخش دسترسی روزانه را مخفی کنید و تنظیمات را با `dayPermissions: []` ذخیره کنید.
3. برای `Assistant` هفت روز را نمایش دهید و زیر هر روز انتخاب Permission مستقل داشته باشید.
4. ابتدا تنظیم فعلی را دریافت و سپس کل ماتریس را یکجا ذخیره کنید؛ PUT جایگزین کامل تنظیم قبلی است.

### دریافت تنظیم

```http
GET /api/admin/secretary/{userId}/schedule
Authorization: Bearer <admin-token>
```

نمونه پاسخ:

```json
{
  "userId": "7f52f6ab-1a3f-42b0-84da-7448722c1f4a",
  "secretaryType": "Assistant",
  "days": ["Monday", "Friday"],
  "dayPermissions": [
    { "day": "Monday", "permissions": ["ViewReservations", "EditReservations"] },
    { "day": "Friday", "permissions": ["ViewReservations", "SecretaryAnnouncement"] }
  ]
}
```

### ذخیره تنظیم

```http
PUT /api/admin/secretary/{userId}/schedule
Authorization: Bearer <admin-token>
Content-Type: application/json
```

```json
{
  "secretaryType": "Assistant",
  "dayPermissions": [
    { "day": "Monday", "permissions": [1, 2] },
    { "day": "Friday", "permissions": [1, 4] }
  ]
}
```

`days` فقط برای سازگاری با کلاینت قدیمی پذیرفته می‌شود و Permission ایجاد نمی‌کند؛ کلاینت جدید باید `dayPermissions` بفرستد. روز تکراری یا enum نامعتبر پاسخ `400` می‌گیرد.

## Enumها و برچسب فارسی

| مقدار | کلید | عنوان UI |
|---:|---|---|
| 1 | `ViewReservations` | مشاهده رزروها |
| 2 | `EditReservations` | ویرایش رزرو |
| 3 | `ConfirmAttendance` | تأیید حضور |
| 4 | `SecretaryAnnouncement` | ثبت اعلام منشی |
| 5 | `ViewPatients` | مشاهده بیماران |
| 6 | `CreateReservation` | ایجاد رزرو |
| 7 | `CancelReservation` | لغو رزرو |

روزها از enum استاندارد .NET هستند: `Saturday`, `Sunday`, `Monday`, `Tuesday`, `Wednesday`, `Thursday`, `Friday`. نگاشت فارسی را در UI انجام دهید و نام انگلیسی را به API بفرستید.

## داشبورد منشی

پس از login (و در هر refresh) دسترسی مؤثر امروز را دریافت کنید:

```http
GET /api/Secretary/access
Authorization: Bearer <secretary-token>
```

```json
{
  "isSecretary": true,
  "hasFullAccess": false,
  "allowedDays": ["Monday", "Friday"],
  "permissions": ["ViewReservations", "SecretaryAnnouncement"]
}
```

`permissions` برای منشی کمکی فقط Permissionهای **روز جاری به وقت ایران** است. اگر امروز در `allowedDays` نباشد، منوهای عملیاتی را نشان ندهید. برای منشی اصلی `hasFullAccess` برابر true و تمام روزها/Permissionها برگردانده می‌شود.

پیشنهاد گارد UI:

```ts
const can = (permission: SecretaryPermissionType) =>
  access.hasFullAccess || access.permissions.includes(permission);
```

- لیست رزرو: `ViewReservations`
- ویرایش: `EditReservations`
- بررسی حضور: `ConfirmAttendance`
- اعلام منشی: `SecretaryAnnouncement`
- بیماران: `ViewPatients`
- ایجاد رزرو: `CreateReservation`
- لغو: `CancelReservation`

مخفی‌سازی UI فقط برای تجربه کاربری است. Backend روی endpointهای منشی کنترل مستقل دارد و در نبود روز/Permission پاسخ `403 Forbidden` می‌دهد. در interceptor، خطای 403 را با پیام «در امروز یا برای این عملیات دسترسی ندارید» نمایش دهید؛ آن را logout یا 401 تلقی نکنید.

## ترتیب پیشنهادی ذخیره فرم

1. ابتدا `PUT /api/User` را با role و `secretaryType` ارسال کنید.
2. اگر نوع `Assistant` است، ماتریس کامل را با PUT schedule ذخیره کنید.
3. اگر نوع `Main` است، PUT schedule را با آرایه خالی بفرستید تا محدودیت‌های قبلی پاک شود.
4. بعد از موفقیت، GET schedule را دوباره اجرا کنید تا state سرور مبنای UI باشد.
