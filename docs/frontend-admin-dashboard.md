# مستند فرانت‌اند: داشبورد ادمین

این سند قرارداد APIهای مورد نیاز فرانت‌اند برای داشبورد ادمین را توضیح می‌دهد. تمرکز این مستند روی سه بخش است:

1. مدیریت کاربران: مشاهده، افزودن، ویرایش و حذف کاربر.
2. مدیریت مشاوران: بدون افزودن/ویرایش/حذف مستقیم؛ مشاور از طریق نقش کاربر در مدیریت کاربران ساخته و ویرایش می‌شود. قابلیت‌های این بخش شامل ثبت امتیاز، مشاهده حضور و غیاب و مشاهده لیدهای مشاور است.
3. مدیریت کامل لیدهای سیستم: مشاهده و فیلتر لیست لیدهای کل سیستم.

## قرارداد عمومی پاسخ‌ها

بیشتر عملیات‌های Command مثل افزودن، ویرایش، حذف و ثبت امتیاز با ساختار زیر برمی‌گردند:

```json
{
  "isSuccess": true,
  "message": "پیام عملیات",
  "data": {}
}
```

در خطا نیز معمولاً همین ساختار با `isSuccess: false` برمی‌گردد:

```json
{
  "isSuccess": false,
  "message": "پیام خطا",
  "data": null
}
```

> نکته مهم: در کنترلرهای فعلی، بسیاری از خطاهای منطقی با HTTP Status برابر `200 OK` برمی‌گردند. فرانت باید موفق یا ناموفق بودن عملیات را از `isSuccess` تشخیص دهد.

لیست‌ها با ساختار صفحه‌بندی زیر برمی‌گردند:

```json
{
  "items": [],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 0
}
```

## Enumهای مورد نیاز فرانت

### Gender

| مقدار | نام | توضیح |
| --- | --- | --- |
| `1` | `Male` | مرد |
| `2` | `Female` | زن |

### نقش‌های قابل استفاده در مدیریت کاربران

در API نقش به صورت رشته‌ای و با فیلد `roleName` ارسال می‌شود. مقادیر رایج قابل استفاده:

| مقدار پیشنهادی `roleName` | کاربرد |
| --- | --- |
| `Admin` | ادمین |
| `Consultant` | مشاور |
| `NormalUser` | کاربر عادی |

> نکته مهم برای مشاوران: هیچ API جداگانه‌ای برای Add/Update/Delete مشاور وجود ندارد. برای ایجاد مشاور، در بخش مدیریت کاربران یک کاربر با `roleName: "Consultant"` ایجاد کنید. برای تغییر اطلاعات مشاور نیز همان API ویرایش کاربر استفاده می‌شود.

### AttendanceStatus

| مقدار | نام | توضیح |
| --- | --- | --- |
| `1` | `Present` | حاضر |
| `2` | `Absent` | غایب |
| `3` | `Leave` | مرخصی |
| `4` | `SickLeave` | مرخصی استعلاجی |
| `5` | `Late` | تأخیر |
| `6` | `Mission` | مأموریت |

### LeadAssignmentState

| مقدار | نام | توضیح پیشنهادی برای UI |
| --- | --- | --- |
| `1` | `New` | جدید |
| `2` | `Assigned` | تخصیص داده شده |
| `3` | `Contacted` | تماس گرفته شده |
| `4` | `Pending` | در انتظار پیگیری |
| `5` | `Converted` | تبدیل شده |
| `6` | `Expired` | منقضی شده |
| `7` | `Rejected` | رد شده |

> محدودیت فعلی فیلتر بک‌اند: در لیست لیدها فقط فیلترهای وضعیت `New`، `Pending`، `Contacted` و `Rejected` اعمال می‌شوند. اگر `Assigned`، `Converted` یا `Expired` ارسال شود، در پیاده‌سازی فعلی فیلتر وضعیت اعمال نمی‌شود.

### LeadAssignmentType

| مقدار | نام | توضیح |
| --- | --- | --- |
| `1` | `RealTime` | تخصیص هم‌زمان |
| `2` | `OfflineQueue` | صف آفلاین |

> محدودیت فعلی فیلتر بک‌اند: در حال حاضر فقط مقدار `OfflineQueue = 2` به عنوان فیلتر نوع لید اعمال می‌شود. اگر `RealTime = 1` ارسال شود، فیلتر نوع اعمال نمی‌شود.

### ScoreSource

برای ثبت امتیاز توسط ادمین، فرانت می‌تواند `source` را `2` ارسال کند؛ با این حال بک‌اند هنگام ذخیره، منبع را به صورت `Manager` ثبت می‌کند.

| مقدار | نام |
| --- | --- |
| `1` | `System` |
| `2` | `Manager` |
| `3` | `Customer` |

### ScoreReason

برای ثبت امتیاز از پنل ادمین فقط دو مقدار زیر مجاز است:

| مقدار | نام | کاربرد | قانون `scoreValue` |
| --- | --- | --- | --- |
| `5` | `ManagerReward` | امتیاز تشویقی مدیر | باید مثبت باشد؛ مثال: `10` |
| `6` | `ManagerPenalty` | جریمه مدیر | باید منفی باشد؛ مثال: `-10` |

سایر مقادیر در بک‌اند وجود دارند اما برای ثبت امتیاز از پنل ادمین پذیرفته نمی‌شوند:

| مقدار | نام |
| --- | --- |
| `1` | `SuccessfulCall` |
| `2` | `FailedCall` |
| `3` | `CustomerPositiveFeedback` |
| `4` | `CustomerNegativeFeedback` |
| `7` | `FastCall` |
| `8` | `LateCall` |
| `9` | `NoAnswer` |

---

# بخش اول: مدیریت کاربران

## 1. دریافت لیست کاربران

| مورد | مقدار |
| --- | --- |
| Method | `GET` |
| URL | `/api/User` |
| نیاز به Body | ندارد |

### Query Parameters

| پارامتر | نوع | اجباری | توضیح |
| --- | --- | --- | --- |
| `firstName` | `string` | خیر | جستجو بر اساس بخشی از نام |
| `lastName` | `string` | خیر | جستجو بر اساس بخشی از نام خانوادگی |
| `roleName` | `string` | خیر | فیلتر بر اساس بخشی از نام نقش؛ مثال: `Consultant` |
| `phoneNumber` | `string` | خیر | جستجو بر اساس بخشی از شماره موبایل |
| `gender` | `number` | خیر | `1` مرد، `2` زن |
| `isCompleteName` | `boolean` | خیر | در بک‌اند به معنی `IsCompleteProfile` استفاده شده است |
| `isActive` | `boolean` | خیر | وضعیت فعال بودن کاربر |
| `createDate` | `string` | خیر | در مدل Query وجود دارد، اما در پیاده‌سازی فعلی فیلتر نمی‌شود |
| `updateDate` | `string` | خیر | در مدل Query وجود دارد، اما در پیاده‌سازی فعلی فیلتر نمی‌شود |
| `deleteDate` | `string` | خیر | در مدل Query وجود دارد، اما در پیاده‌سازی فعلی فیلتر نمی‌شود |
| `pageNumber` | `number` | خیر | پیش‌فرض `1` |
| `pageSize` | `number` | خیر | پیش‌فرض `10` |

### نمونه درخواست

```http
GET /api/User?roleName=Consultant&isActive=true&pageNumber=1&pageSize=10
```

### نمونه پاسخ

```json
{
  "items": [
    {
      "id": "4c4f1a2a-2d6a-4f1f-b45b-54debc92e001",
      "firstName": "سارا",
      "lastName": "محمدی",
      "phoneNumber": "09123456789",
      "roleName": "Consultant",
      "isActive": true
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

## 2. افزودن کاربر

| مورد | مقدار |
| --- | --- |
| Method | `POST` |
| URL | `/api/User` |
| Content-Type | `application/json` |

### Request Body

```json
{
  "firstName": "سارا",
  "lastName": "محمدی",
  "phoneNumber": "09123456789",
  "passwordHash": "StrongPass123",
  "isCompleteProfile": false,
  "avatarImageName": null,
  "gender": 2,
  "birthDate": "1995-05-20T00:00:00",
  "roleName": "Consultant"
}
```

### فیلدها و اعتبارسنجی فرانت

| فیلد | نوع | اجباری | قانون |
| --- | --- | --- | --- |
| `firstName` | `string` | بله | حداکثر ۱۰۰ کاراکتر |
| `lastName` | `string` | بله | حداکثر ۱۰۰ کاراکتر |
| `phoneNumber` | `string` | بله | الگو: `^09\d{9}$` |
| `passwordHash` | `string` | بله | حداقل ۶ و حداکثر ۱۰۰ کاراکتر؛ با وجود نام فیلد، رمز خام ارسال می‌شود و بک‌اند هش می‌کند |
| `isCompleteProfile` | `boolean` | خیر | پیشنهاد: `false` |
| `avatarImageName` | `string \| null` | خیر | نام فایل آواتار یا `null` |
| `gender` | `number` | بله | `1` یا `2` |
| `birthDate` | `string` | بله | تاریخ قبل از زمان فعلی، با فرمت قابل تبدیل به `DateTime` |
| `roleName` | `string` | بله | مثال: `Admin`، `Consultant`، `NormalUser` |

### نمونه پاسخ موفق

```json
{
  "isSuccess": true,
  "message": "ثبت کاربر جدید با موفقیت انجام شد",
  "data": {
    "id": "4c4f1a2a-2d6a-4f1f-b45b-54debc92e001",
    "roleName": "Consultant",
    "isActive": false
  }
}
```

> نکته: در پیاده‌سازی فعلی، کاربر جدیدی که از این API ساخته می‌شود با `isActive: false` ذخیره می‌شود.

### خطاهای مهم

| سناریو | پیام نمونه |
| --- | --- |
| شماره موبایل تکراری | `کاربری با این شماره موبایل قبلاً ثبت شده است` |
| شماره موبایل نامعتبر | `شماره موبایل معتبر نیست.` |
| رمز عبور کمتر از ۶ کاراکتر | `رمز عبور باید حداقل 6 کاراکتر باشد.` |
| تاریخ تولد نامعتبر | `تاریخ تولد معتبر نیست.` |

## 3. ویرایش کاربر

| مورد | مقدار |
| --- | --- |
| Method | `PUT` |
| URL | `/api/User` |
| Content-Type | `application/json` |

### Request Body

```json
{
  "id": "4c4f1a2a-2d6a-4f1f-b45b-54debc92e001",
  "firstName": "سارا",
  "lastName": "محمدی",
  "phoneNumber": "09123456789",
  "isCompleteProfile": true,
  "avatarImageName": "avatar.png",
  "gender": 2,
  "isActive": true,
  "roleName": "Consultant"
}
```

### نکات فرانت

- فیلد `id` الزامی است و باید همان شناسه کاربر باشد.
- این API رمز عبور و تاریخ تولد را آپدیت نمی‌کند.
- برای تبدیل یک کاربر به مشاور یا تغییر نقش مشاور، مقدار `roleName` را ارسال کنید.
- برای فعال/غیرفعال کردن کاربر، از `isActive` استفاده کنید.

### نمونه پاسخ موفق

```json
{
  "isSuccess": true,
  "message": "ویرایش کاربر با موفقیت انجام شد",
  "data": {
    "firstName": "سارا",
    "lastName": "محمدی",
    "roleName": "Consultant",
    "isActive": true
  }
}
```

### خطاهای مهم

| سناریو | پیام نمونه |
| --- | --- |
| کاربر وجود نداشته باشد | `کاربر یافت نشد` |

## 4. حذف کاربر

| مورد | مقدار |
| --- | --- |
| Method | `DELETE` |
| URL | `/api/User?userId={USER_ID}` |
| نیاز به Body | ندارد |

### نمونه درخواست

```http
DELETE /api/User?userId=4c4f1a2a-2d6a-4f1f-b45b-54debc92e001
```

### نمونه پاسخ موفق

```json
{
  "isSuccess": true,
  "message": "حذف کاربر با موفقیت انجام شد",
  "data": true
}
```

### خطاهای مهم

| سناریو | پیام نمونه |
| --- | --- |
| کاربر وجود نداشته باشد | `کاربر یافت نشد` |

---

# بخش دوم: مدیریت مشاوران

## اصل مهم مدیریت مشاوران

مشاوران به صورت مستقیم Add/Update/Delete ندارند. برای ایجاد، ویرایش، فعال/غیرفعال کردن یا حذف مشاور باید از APIهای مدیریت کاربران استفاده شود و نقش کاربر برابر `Consultant` باشد.

در بخش مشاوران داشبورد ادمین فقط این قابلیت‌ها استفاده می‌شود:

1. مشاهده لیست مشاوران.
2. ثبت امتیاز مثبت یا منفی برای مشاور.
3. مشاهده لیست حضور و غیاب‌های مشاور.
4. مشاهده لیدهای مرتبط با مشاور.

## 1. دریافت لیست مشاوران

| مورد | مقدار |
| --- | --- |
| Method | `GET` |
| URL | `/api/Consultant/GetConsultants` |
| نیاز به Body | ندارد |

### Query Parameters

| پارامتر | نوع | اجباری | توضیح |
| --- | --- | --- | --- |
| `firstName` | `string` | خیر | جستجو بر اساس بخشی از نام |
| `lastName` | `string` | خیر | جستجو بر اساس بخشی از نام خانوادگی |
| `phoneNumber` | `string` | خیر | فیلتر دقیق شماره موبایل |
| `pageNumber` | `number` | خیر | پیش‌فرض `1` |
| `pageSize` | `number` | خیر | پیش‌فرض `10` |

> نکته: Query Model فیلدهای دیگری مانند وضعیت حضور و غیاب، امتیاز و وضعیت لید دارد، اما در Handler فعلی لیست مشاوران فقط `firstName`، `lastName`، `phoneNumber` و صفحه‌بندی اعمال می‌شود.

### نمونه درخواست

```http
GET /api/Consultant/GetConsultants?pageNumber=1&pageSize=10&firstName=سارا
```

### نمونه پاسخ

```json
{
  "items": [
    {
      "id": "4c4f1a2a-2d6a-4f1f-b45b-54debc92e001",
      "firstName": "سارا",
      "lastName": "محمدی",
      "phoneNumber": "09123456789",
      "profileId": 12,
      "attendanceResponse": null,
      "scoreLogResponse": null,
      "leadsAssignmentItemsResponse": null
    }
  ],
  "totalCount": 1,
  "pageNumber": 0,
  "pageSize": 0,
  "totalPages": 0
}
```

> نکته: در پیاده‌سازی فعلی Handler فقط `items` و `totalCount` را مقداردهی می‌کند؛ بنابراین ممکن است `pageNumber` و `pageSize` در پاسخ مقدار پیش‌فرض `0` داشته باشند.

## 2. ثبت امتیاز برای مشاور

| مورد | مقدار |
| --- | --- |
| Method | `POST` |
| URL | `/api/ScoreLog` |
| Content-Type | `application/json` |

### Request Body برای امتیاز مثبت مدیر

```json
{
  "consultantProfileId": 12,
  "source": 2,
  "reason": 5,
  "scoreValue": 10,
  "description": "عملکرد خوب در پیگیری لیدها",
  "leadAssignmentId": null,
  "createdByUserId": "11111111-1111-1111-1111-111111111111"
}
```

### Request Body برای جریمه مدیر

```json
{
  "consultantProfileId": 12,
  "source": 2,
  "reason": 6,
  "scoreValue": -10,
  "description": "عدم پیگیری به موقع لید",
  "leadAssignmentId": 45,
  "createdByUserId": "11111111-1111-1111-1111-111111111111"
}
```

### فیلدها

| فیلد | نوع | اجباری | توضیح |
| --- | --- | --- | --- |
| `consultantProfileId` | `number` | بله | مقدار `profileId` دریافتی از لیست مشاوران |
| `source` | `number` | خیر/پیشنهادی | برای ادمین `2` ارسال شود؛ بک‌اند نهایتاً `Manager` ذخیره می‌کند |
| `reason` | `number` | بله | فقط `5` یا `6` مجاز است |
| `scoreValue` | `number` | بله | برای `ManagerReward = 5` مثبت، برای `ManagerPenalty = 6` منفی |
| `description` | `string \| null` | خیر | توضیح قابل نمایش/ذخیره |
| `leadAssignmentId` | `number \| null` | خیر | اگر امتیاز مربوط به یک لید خاص است، شناسه لید ارسال شود |
| `createdByUserId` | `string \| null` | خیر | شناسه ادمینی که امتیاز را ثبت می‌کند |

### نمونه پاسخ موفق

```json
{
  "isSuccess": true,
  "message": "امتیاز با موفقیت ثبت شد"
}
```

### خطاهای مهم

| سناریو | پیام نمونه |
| --- | --- |
| دلیل امتیاز غیر از پاداش/جریمه مدیر باشد | `فقط امتیاز تشویقی یا جریمه مدیر قابل ثبت است` |
| امتیاز تشویقی منفی باشد | `امتیاز تشویقی مدیر باید مثبت باشد` |
| جریمه مدیر مثبت باشد | `امتیاز جریمه مدیر باید منفی باشد` |
| پروفایل مشاور پیدا نشود | `مشاوری یافت نشد` |
| پروفایل مشاور حذف شده باشد | `پروفایل مشاور حذف شده است` |
| پروفایل مشاور کامل نباشد | `پروفایل مشاور کامل نیست` |

## 3. مشاهده حضور و غیاب‌های مشاور

| مورد | مقدار |
| --- | --- |
| Method | `GET` |
| URL | `/api/Attendance` |
| نیاز به Body | ندارد |

### Query Parameters

| پارامتر | نوع | اجباری | توضیح |
| --- | --- | --- | --- |
| `consultantProfileId` | `number` | بله | مقدار `profileId` مشاور |
| `pageNumber` | `number` | خیر | در Query وجود دارد، اما Handler فعلی آن را اعمال نمی‌کند |
| `pageSize` | `number` | خیر | در Query وجود دارد، اما Handler فعلی آن را اعمال نمی‌کند |

### نمونه درخواست

```http
GET /api/Attendance?consultantProfileId=12&pageNumber=1&pageSize=10
```

### نمونه پاسخ

```json
{
  "items": [
    {
      "id": 101,
      "attendanceDate": "1404/03/20",
      "checkInTime": "09:00",
      "checkOutTime": "17:00",
      "status": 1,
      "description": null
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

> نکته: تاریخ و ساعت‌ها در پاسخ به صورت رشته فارسی/نمایشی برمی‌گردند.

## 4. مشاهده لیدهای مرتبط با مشاور

| مورد | مقدار |
| --- | --- |
| Method | `GET` |
| URL | `/api/Consultant/GetLeads` |
| نیاز به Body | ندارد |

### Query Parameters

| پارامتر | نوع | اجباری | توضیح |
| --- | --- | --- | --- |
| `profileId` | `number` | بله | مقدار `profileId` مشاور |
| `leadAssignmentState` | `number` | خیر | وضعیت لید؛ محدودیت فیلتر در بخش Enum توضیح داده شد |
| `leadAssignmentType` | `number` | خیر | نوع تخصیص لید؛ محدودیت فیلتر در بخش Enum توضیح داده شد |
| `pageNumber` | `number` | خیر | پیش‌فرض `1` |
| `pageSize` | `number` | خیر | پیش‌فرض `10` |

### نمونه درخواست

```http
GET /api/Consultant/GetLeads?profileId=12&leadAssignmentState=1&leadAssignmentType=2&pageNumber=1&pageSize=10
```

### نمونه پاسخ

```json
{
  "items": [
    {
      "id": 45,
      "userName": "علی رضایی",
      "phoneNumber": "09121234567",
      "leadAssignmentState": 1,
      "leadAssignmentType": 2
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

---

# بخش سوم: مدیریت کامل لیدهای سیستم

## دریافت لیست همه لیدهای سیستم

| مورد | مقدار |
| --- | --- |
| Method | `GET` |
| URL | `/api/LeadAssignment` |
| نیاز به Body | ندارد |

### Query Parameters

| پارامتر | نوع | اجباری | توضیح |
| --- | --- | --- | --- |
| `leadAssignmentState` | `number` | خیر | وضعیت لید؛ فقط `1`، `3`، `4` و `7` در Handler فعلی فیلتر می‌شوند |
| `leadAssignmentType` | `number` | خیر | نوع تخصیص؛ فقط `2` در Handler فعلی فیلتر می‌شود |
| `pageNumber` | `number` | خیر | پیش‌فرض `1` |
| `pageSize` | `number` | خیر | پیش‌فرض `10` |

### نمونه درخواست بدون فیلتر

```http
GET /api/LeadAssignment?pageNumber=1&pageSize=10
```

### نمونه درخواست با فیلتر

```http
GET /api/LeadAssignment?leadAssignmentState=3&leadAssignmentType=2&pageNumber=1&pageSize=10
```

### نمونه پاسخ

```json
{
  "items": [
    {
      "id": 45,
      "userName": "علی رضایی",
      "phoneNumber": "09121234567",
      "leadAssignmentState": 3,
      "leadAssignmentType": 2
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

> نکته مهم فنی: اکشن فعلی `LeadAssignmentController.Get` نتیجه `DispatchAsync` را بدون `await` داخل `Ok` برمی‌گرداند. اگر در محیط اجرا خروجی غیرمنتظره یا Task wrapper مشاهده شد، این قسمت در بک‌اند باید اصلاح شود.

## چک‌لیست دقیق فرانت برای داشبورد ادمین

- برای تشخیص خطا در عملیات‌های Command، همیشه `isSuccess` را بررسی کنید.
- برای ایجاد مشاور، از `POST /api/User` با `roleName: "Consultant"` استفاده کنید.
- برای ویرایش یا حذف مشاور، از APIهای `PUT /api/User` و `DELETE /api/User` استفاده کنید؛ API مستقیم برای مشاور وجود ندارد.
- برای نمایش صفحه جزئیات مشاور، ابتدا `GET /api/Consultant/GetConsultants` را بزنید و `profileId` را نگه دارید.
- برای ثبت امتیاز مشاور، `consultantProfileId` باید همان `profileId` باشد، نه `id` کاربر.
- برای امتیاز مثبت ادمین، `reason = 5` و `scoreValue > 0` ارسال کنید.
- برای جریمه ادمین، `reason = 6` و `scoreValue < 0` ارسال کنید.
- برای حضور و غیاب مشاور، `GET /api/Attendance?consultantProfileId={profileId}` استفاده شود.
- برای لیدهای یک مشاور، `GET /api/Consultant/GetLeads?profileId={profileId}` استفاده شود.
- برای همه لیدهای سیستم، `GET /api/LeadAssignment` استفاده شود.
- فیلترهای تاریخ در لیست کاربران و برخی فیلترهای Query مشاوران فعلاً در Handler اعمال نمی‌شوند؛ فرانت نباید به آن‌ها وابسته باشد.
