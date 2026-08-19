# راهنمای فرانت: مدیریت منشی‌ها توسط ادمین

این سند قرارداد API صفحه جدید «مدیریت منشی‌ها» در پنل ادمین را توضیح می‌دهد. تمام endpointهای این صفحه فقط با JWT دارای نقش `Admin` قابل استفاده‌اند.

## ۱. لیست، جست‌وجو و فیلتر منشی‌ها

```http
GET /api/admin/secretaries
Authorization: Bearer <admin-token>
```

مسیر قدیمی `GET /api/admin/secretary` نیز همین خروجی را می‌دهد، ولی برای توسعه جدید مسیر جمع (`secretaries`) پیشنهاد می‌شود. نقش در بک‌اند همیشه روی `Secretary` تنظیم می‌شود؛ بنابراین فرانت نباید `roleName` بفرستد و امکان دریافت کاربران نقش‌های دیگر از این endpoint وجود ندارد.

### Query parameters

| پارامتر | نوع | توضیح |
|---|---|---|
| `search` | string | جست‌وجوی هم‌زمان در نام، نام خانوادگی، نام کامل و شماره موبایل |
| `firstName` | string | فیلتر نام |
| `lastName` | string | فیلتر نام خانوادگی |
| `phoneNumber` | string | فیلتر شماره موبایل |
| `gender` | number | مقدار enum جنسیت مطابق قرارداد فعلی پروژه |
| `isActive` | boolean | فعال/غیرفعال |
| `isCompleteName` | boolean | تکمیل بودن پروفایل (نام این پارامتر برای سازگاری با API فعلی حفظ شده است) |
| `secretaryType` | number | `1` منشی اصلی، `2` منشی کمکی |
| `pageNumber` | number | شماره صفحه؛ پیش‌فرض `1` |
| `pageSize` | number | اندازه صفحه؛ پیش‌فرض `10` |

نمونه:

```http
GET /api/admin/secretaries?search=0912&secretaryType=2&isActive=true&pageNumber=1&pageSize=20
```

### پاسخ موفق (`200`)

```json
{
  "items": [
    {
      "id": "a3f2d637-d7da-4e43-a3d1-f44cf4d4e1ad",
      "firstName": "مریم",
      "lastName": "احمدی",
      "phoneNumber": "09121234567",
      "roleName": "Secretary",
      "isActive": true,
      "isCompleteProfile": true,
      "gender": 1,
      "createdAt": "2026-08-19T09:00:00Z",
      "secretaryType": 2
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 20
}
```

> کاربران منشی قدیمی که مقدار `secretaryType` آن‌ها در دیتابیس خالی است، از نظر دسترسی «اصلی» محسوب می‌شوند و در فیلتر `secretaryType=1` نیز نمایش داده می‌شوند.

## ۲. ایجاد منشی

از endpoint مدیریت کاربران استفاده کنید:

```http
POST /api/User
Content-Type: application/json
Authorization: Bearer <admin-token>
```

در payload حتماً `roleName` را برابر `Secretary` قرار دهید. فیلد اختیاری `secretaryType` مقدار `1` (اصلی) یا `2` (کمکی) می‌گیرد. اگر ارسال نشود، منشی اصلی ساخته می‌شود.

```json
{
  "firstName": "مریم",
  "lastName": "احمدی",
  "phoneNumber": "09121234567",
  "passwordHash": "initial-password",
  "gender": 1,
  "birthDate": "1995-01-01T00:00:00Z",
  "roleName": "Secretary",
  "secretaryType": 2
}
```

## ۳. ویرایش منشی و تبدیل نوع آن

اطلاعات عمومی، وضعیت فعال بودن، نقش و نوع منشی با endpoint فعلی کاربر ویرایش می‌شود:

```http
PUT /api/User
Content-Type: application/json
Authorization: Bearer <admin-token>
```

```json
{
  "id": "a3f2d637-d7da-4e43-a3d1-f44cf4d4e1ad",
  "firstName": "مریم",
  "lastName": "احمدی",
  "phoneNumber": "09121234567",
  "isCompleteProfile": true,
  "avatarImageName": null,
  "gender": 1,
  "isActive": true,
  "roleName": "Secretary",
  "secretaryType": 1
}
```

برای تغییر برنامه و نوع منشی از endpoint زیر استفاده کنید:

```http
PUT /api/admin/secretaries/{userId}/schedule
```

برای منشی کمکی، روزها و دسترسی‌های هر روز را بفرستید:

```json
{
  "secretaryType": 2,
  "dayPermissions": [
    { "day": "Saturday", "permissions": [1, 2] },
    { "day": "Sunday", "permissions": [1] }
  ]
}
```

برای تبدیل به منشی اصلی، این payload پیشنهاد می‌شود:

```json
{
  "secretaryType": 1,
  "dayPermissions": []
}
```

بک‌اند هنگام تبدیل به `Main` برنامه و مجوزهای محدود قبلی را پاک می‌کند. برای سازگاری با state قدیمی فرم، حتی اگر فرانت سهواً روزهای قبلی را نیز همراه درخواست بفرستد، تبدیل به منشی اصلی انجام می‌شود و روزها نادیده گرفته می‌شوند. پاسخ موفق این endpoint `204 No Content` است.

روزهای تنظیم‌شده برای منشی کمکی، **روزهای مجاز ورود و استفاده از بخش** هستند و نباید به‌عنوان روز هفته رزرو تفسیر شوند. برای مثال، اگر منشی در روز شنبه مجوز `ViewReservations` داشته باشد، در همان شنبه تمام رزروهای مطابق فیلتر صفحه (از جمله رزرو روزهای دیگر) برای او لود می‌شوند؛ روز شنبه صرفاً زمان مجاز استفاده از صفحه را مشخص می‌کند.

برای خواندن تنظیمات:

```http
GET /api/admin/secretaries/{userId}/schedule
```

## ۴. حذف منشی

```http
DELETE /api/User?Id={userId}
Authorization: Bearer <admin-token>
```

## پیشنهاد پیاده‌سازی UI

1. جست‌وجو را با debounce حدود ۳۰۰ تا ۵۰۰ میلی‌ثانیه به پارامتر `search` متصل کنید.
2. با تغییر هر فیلتر، `pageNumber` را به `1` برگردانید.
3. برای نوع منشی گزینه‌های «همه»، «اصلی (`1`)» و «کمکی (`2`)» نمایش دهید.
4. فرم نوع/برنامه را پس از پاسخ موفق مجدداً از endpoint `schedule` بخوانید و سپس لیست را refresh کنید.
5. عملیات ایجاد، ویرایش، حذف و تنظیم برنامه فقط برای کاربر دارای نقش ادمین نمایش داده شود.
