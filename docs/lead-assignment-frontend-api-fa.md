# مستند APIهای مورد نیاز فرانت برای تخصیص لید به مشاور

این مستند فقط APIهای بک‌اند مرتبط با حضور مشاور، آنلاین/آفلاین شدن، دریافت لیدها، ثبت گزارش تماس و ثبت امتیاز مدیریتی را پوشش می‌دهد.

## قرارداد عمومی پاسخ Commandها

اکثر APIهای عملیاتی خروجی `Result` برمی‌گردانند:

```json
{
  "isSuccess": true,
  "message": "متن پیام"
}
```

در حالت خطا، `isSuccess=false` و متن خطا در `message` برمی‌گردد.

## Enumهای مورد نیاز فرانت

### LeadAssignmentType

| مقدار عددی | نام | توضیح |
| --- | --- | --- |
| 1 | RealTime | لید لحظه‌ای با قانون تماس سه‌دقیقه‌ای |
| 2 | OfflineQueue | لید صف آفلاین بدون قانون سه‌دقیقه‌ای |

### LeadAssignmentState

| مقدار عددی | نام |
| --- | --- |
| 1 | New |
| 2 | Assigned |
| 3 | Contacted |
| 4 | Pending |
| 5 | Converted |
| 6 | Expired |
| 7 | Rejected |

### LeadCallResult

| مقدار عددی | نام | اثر روی وضعیت لید |
| --- | --- | --- |
| 1 | Contacted | Contacted |
| 2 | Converted | Converted |
| 3 | Rejected | Rejected |
| 4 | NoAnswer | Contacted |
| 5 | WrongNumber | Rejected |
| 6 | NeedFollowUp | Contacted |

### ScoreReason برای امتیاز مدیریتی

برای API امتیاز مدیریتی فقط این دو مقدار مجاز است:

| مقدار عددی | نام | قانون مقدار امتیاز |
| --- | --- | --- |
| 5 | ManagerReward | `scoreValue` باید صفر یا مثبت باشد |
| 6 | ManagerPenalty | `scoreValue` باید صفر یا منفی باشد |

## 1. ثبت حضور / عدم حضور مشاور

### آدرس

`POST /api/Consultant/SetAvalableConsultant`

> نام مسیر فعلی در بک‌اند همین `SetAvalableConsultant` است و برای جلوگیری از شکستن API تغییر داده نشده است.

### ورودی

```json
{
  "profileId": 12,
  "isAvailable": true
}
```

| فیلد | نوع | اجباری | توضیح |
| --- | --- | --- | --- |
| `profileId` | number | بله | شناسه پروفایل مشاور |
| `isAvailable` | boolean | بله | `true` برای ثبت حضور، `false` برای عدم حضور |

### خروجی موفق ثبت حضور

```json
{
  "isSuccess": true,
  "message": "حضور شما ثبت شد"
}
```

### خروجی موفق عدم حضور

```json
{
  "isSuccess": true,
  "message": "عدم حضور شما ثبت شد"
}
```

### نکته رفتاری مهم

بعد از ثبت حضور با `isAvailable=true`، بک‌اند بلافاصله تخصیص لیدهای `OfflineQueue` در انتظار را اجرا می‌کند. بنابراین فرانت لازم نیست برای تخصیص لید آفلاین API جداگانه‌ای صدا بزند.

## 2. آنلاین / آفلاین کردن مشاور

### آدرس

`POST /api/Consultant/SetOnlineOfflineConsultant`

### ورودی آنلاین شدن

```json
{
  "profileId": 12,
  "isOnline": true,
  "isOffline": false
}
```

### ورودی آفلاین شدن

```json
{
  "profileId": 12,
  "isOnline": false,
  "isOffline": true
}
```

| فیلد | نوع | اجباری | توضیح |
| --- | --- | --- | --- |
| `profileId` | number | بله | شناسه پروفایل مشاور |
| `isOnline` | boolean | بله | اگر `true` باشد درخواست آنلاین شدن است |
| `isOffline` | boolean | خیر/سازگاری | در منطق فعلی تصمیم اصلی با `isOnline` گرفته می‌شود |

### خروجی‌های مهم

```json
{
  "isSuccess": true,
  "message": "شما آنلاین شدید"
}
```

```json
{
  "isSuccess": true,
  "message": "شما آفلاین شدید"
}
```

اگر مشاور لید آفلاین باز داشته باشد:

```json
{
  "isSuccess": false,
  "message": "ابتدا لیدهای آفلاین خود را تعیین تکلیف کنید"
}
```

## 3. دریافت لیدهای مشاور

### آدرس

`GET /api/Consultant/GetLeads`

### Query string

نمونه:

`/api/Consultant/GetLeads?profileId=12&leadAssignmentState=2&leadAssignmentType=2&pageNumber=1&pageSize=10`

| پارامتر | نوع | اجباری | توضیح |
| --- | --- | --- | --- |
| `profileId` | number | بله | شناسه پروفایل مشاور |
| `leadAssignmentState` | number | خیر | فیلتر وضعیت لید |
| `leadAssignmentType` | number | خیر | فیلتر نوع لید: 1 لحظه‌ای، 2 آفلاین |
| `pageNumber` | number | خیر | پیش‌فرض 1 |
| `pageSize` | number | خیر | پیش‌فرض 10 |

### خروجی

```json
{
  "items": [
    {
      "id": 1001,
      "userName": "علی رضایی",
      "phoneNumber": "09120000000",
      "leadAssignmentState": 2,
      "leadAssignmentType": 2
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 10
}
```

> فیلد `id` همان `leadAssignmentId` مورد نیاز برای ثبت گزارش تماس است.

## 4. ثبت گزارش تماس لید

### آدرس

`POST /api/Consultant/SubmitLeadCallReport`

### ورودی

```json
{
  "leadAssignmentId": 1001,
  "consultantProfileId": 12,
  "callResult": 2,
  "reportDescription": "مشتری تمایل به رزرو نوبت داشت."
}
```

| فیلد | نوع | اجباری | توضیح |
| --- | --- | --- | --- |
| `leadAssignmentId` | number | بله | شناسه لید تخصیص‌یافته |
| `consultantProfileId` | number | بله | شناسه پروفایل مشاور مالک لید |
| `callResult` | number | بله | مقدار enum نتیجه تماس |
| `reportDescription` | string | خیر | توضیحات مشاور |

### خروجی‌های موفق مهم

اگر گزارش ثبت شود و مشاور هنوز لید آفلاین باز داشته باشد:

```json
{
  "isSuccess": true,
  "message": "گزارش ثبت شد، اما هنوز لید آفلاین تعیین‌تکلیف‌نشده دارید"
}
```

اگر گزارش ثبت شود و مشاور داخل ساعت کاری به صورت خودکار آنلاین شود:

```json
{
  "isSuccess": true,
  "message": "گزارش ثبت شد و شما به صورت خودکار آنلاین شدید"
}
```

اگر گزارش ثبت شود ولی خارج از ساعت کاری باشد:

```json
{
  "isSuccess": true,
  "message": "گزارش ثبت شد، اما خارج از ساعت کاری هستید"
}
```

### خروجی‌های خطای مهم

اگر لید قبلاً گزارش شده باشد:

```json
{
  "isSuccess": false,
  "message": "گزارش این لید قبلا ثبت شده است"
}
```

اگر مهلت لید لحظه‌ای تمام شده و لید منقضی شده باشد:

```json
{
  "isSuccess": false,
  "message": "مهلت ثبت گزارش این لید به پایان رسیده است"
}
```

## 5. ثبت امتیاز مدیریتی مشاور

### آدرس

`POST /api/ScoreLog`

### ورودی تشویقی

```json
{
  "consultantProfileId": 12,
  "reason": 5,
  "scoreValue": 10,
  "description": "عملکرد خوب در پیگیری لیدها",
  "leadAssignmentId": null,
  "createdByUserId": "00000000-0000-0000-0000-000000000000"
}
```

### ورودی جریمه

```json
{
  "consultantProfileId": 12,
  "reason": 6,
  "scoreValue": -5,
  "description": "تاخیر در پیگیری",
  "leadAssignmentId": null,
  "createdByUserId": "00000000-0000-0000-0000-000000000000"
}
```

| فیلد | نوع | اجباری | توضیح |
| --- | --- | --- | --- |
| `consultantProfileId` | number | بله | شناسه پروفایل مشاور |
| `reason` | number | بله | فقط 5 یا 6 مجاز است |
| `scoreValue` | number | بله | برای تشویق مثبت، برای جریمه منفی |
| `description` | string | خیر | توضیح مدیر |
| `leadAssignmentId` | number/null | خیر | اگر امتیاز به لید خاصی مربوط است |
| `createdByUserId` | guid/null | خیر | شناسه کاربر ثبت‌کننده |

### خروجی موفق

```json
{
  "isSuccess": true,
  "message": "امتیاز با موفقیت ثبت شد"
}
```

### نکته رفتاری مهم

با ثبت امتیاز مدیریتی، `CurrentScore` مشاور همان لحظه به اندازه `scoreValue` تغییر می‌کند و در اولویت تخصیص لیدهای بعدی اثر می‌گذارد.

## سناریوهای دستی پیشنهادی برای QA فرانت/بک‌اند

1. لید خارج از ساعت 09:00 تا 21:00 باید به صورت `OfflineQueue/Pending` و بدون deadline ذخیره شود.
2. بعد از ثبت حضور مشاور، تا ۵ لید آفلاین Pending باید همان لحظه به او تخصیص داده شود.
3. اگر مشاور لید آفلاین باز دارد، آنلاین شدن باید با پیام «ابتدا لیدهای آفلاین خود را تعیین تکلیف کنید» رد شود.
4. اگر مشاور لید RealTime فعال و گزارش‌نشده دارد، نباید در ظرفیت لید RealTime بعدی حساب شود.
5. بعد از ثبت گزارش موفق در ساعت کاری و بدون لید آفلاین باز، مشاور باید خودکار آنلاین شود.
6. بعد از ثبت گزارش یک لید آفلاین در حالی که لید آفلاین باز دیگری باقی مانده، مشاور باید آفلاین بماند.
7. اگر لید RealTime تا سه دقیقه گزارش نشود، باید Expired شود، امتیاز `-10` ثبت شود و `CurrentScore` مشاور ۱۰ امتیاز کم شود.
