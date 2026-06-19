# مستند جامع پیاده‌سازی فرانت: حضور/آنلاین مشاور و مدیریت لیدها

این مستند برای تیم فرانت نوشته شده تا بتواند بخش‌های زیر را بدون نیاز به حدس‌زدن رفتار بک‌اند پیاده‌سازی کند:

- ثبت حضور و عدم حضور مشاور.
- آنلاین/آفلاین شدن مشاور برای دریافت لید لحظه‌ای.
- مشاهده و مدیریت لیدهای هر مشاور.
- ثبت گزارش تماس برای لیدهای `RealTime` و `OfflineQueue`.
- نمایش پیام‌ها، وضعیت‌ها، محدودیت‌ها و سناریوهای خطا.
- دریافت تاریخچه حضور و غیاب و اطلاعات پایه مشاور.

> نکته مهم: مسیرهای API دقیقاً مطابق بک‌اند فعلی نوشته شده‌اند. بعضی نام‌ها مثل `SetAvalableConsultant` غلط املایی دارند، اما برای حفظ سازگاری با بک‌اند نباید در فرانت اصلاح شوند.


---

## 0. ورود، توکن و هدایت کاربر به داشبورد درست

### 0.1 Login

آدرس ورود:

`POST /api/Auth/Login`

Request:

```json
{
  "phoneNumber": "09120000000",
  "passwordHash": "123456"
}
```

Response موفق شامل اطلاعات لازم برای route guard و نمایش نام کاربر است:

```json
{
  "isSuccess": true,
  "message": "ورود با موفقیت انجام شد",
  "data": {
    "userId": "00000000-0000-0000-0000-000000000000",
    "firstName": "علی",
    "lastName": "رضایی",
    "fullName": "علی رضایی",
    "phoneNumber": "09120000000",
    "role": "Consultant",
    "roles": ["Consultant"],
    "consultantProfileId": 12,
    "defaultDashboard": "ConsultantDashboard",
    "defaultDashboardRoute": "/consultant/dashboard",
    "dashboardAccess": [
      {
        "role": "Consultant",
        "dashboard": "ConsultantDashboard",
        "route": "/consultant/dashboard"
      }
    ],
    "token": "jwt-token"
  }
}
```

قانون فرانت بعد از login:

1. اگر `isSuccess=false` بود، پیام را نمایش بده و وارد داشبورد نشو.
2. اگر `isSuccess=true` بود، `data.token` را ذخیره کن.
3. `firstName`, `lastName`, `fullName`, `role`, `roles`, `consultantProfileId` و `defaultDashboardRoute` را در auth state ذخیره کن.
4. کاربر را به `defaultDashboardRoute` هدایت کن.
5. برای route guard داشبوردها فقط به path اعتماد نکن؛ نقش‌های `roles` را چک کن.

### 0.2 Claimهای داخل JWT

توکن حالا هم claimهای استاندارد و هم claimهای ساده برای فرانت دارد:

| claim | کاربرد |
| --- | --- |
| `sub` / `nameidentifier` / `userId` / `Id` | شناسه کاربر |
| `given_name` / `firstName` / `FirstName` | نام |
| `family_name` / `lastName` / `LastName` | نام خانوادگی |
| `name` / `fullName` / `FullName` | نام کامل |
| `phoneNumber` / `PhoneNumber` | شماره موبایل |
| `role` / `roles` | نقش یا نقش‌های کاربر |

پیشنهاد فرانت: برای جلوگیری از تفاوت mapping کتابخانه‌های JWT، اول از response لاگین استفاده کن و اگر صفحه refresh شد از `/api/Auth/Me` یا decode کردن token برای restore کردن auth state استفاده کن.

### 0.3 دریافت کاربر جاری بعد از refresh صفحه

آدرس:

`GET /api/Auth/Me`

Header:

```http
Authorization: Bearer <token>
```

Response نمونه:

```json
{
  "isAuthenticated": true,
  "userId": "00000000-0000-0000-0000-000000000000",
  "firstName": "علی",
  "lastName": "رضایی",
  "fullName": "علی رضایی",
  "phoneNumber": "09120000000",
  "role": "Consultant",
  "roles": ["Consultant"],
  "defaultDashboard": "ConsultantDashboard",
  "defaultDashboardRoute": "/consultant/dashboard",
  "dashboardAccess": [
    {
      "role": "Consultant",
      "dashboard": "ConsultantDashboard",
      "route": "/consultant/dashboard"
    }
  ]
}
```

اگر token معتبر نباشد، بک‌اند `401` می‌دهد. اگر کاربر نقش لازم برای داشبوردی را نداشت، route guard فرانت باید کاربر را به صفحه «عدم دسترسی» ببرد.

### 0.4 Route guard پیشنهادی

| route | نقش مجاز |
| --- | --- |
| `/admin/dashboard` | `Admin` |
| `/consultant/dashboard` | `Consultant` |
| `/patient/dashboard` | `Patient` |
| `/dashboard` | `NormalUser` یا `User` |

اگر کاربر لاگین نیست: redirect به login.

اگر لاگین است ولی نقش لازم را ندارد: نمایش صفحه access denied با پیام «شما به این بخش دسترسی ندارید».

---

## 1. تصویر کلی سیستم برای فرانت

### 1.1 نقش‌های اصلی فرانت

فرانت باید حداقل دو تجربه کاربری داشته باشد:

1. **پنل مشاور**
   - ثبت شروع حضور.
   - ثبت پایان حضور.
   - آنلاین شدن برای دریافت لید لحظه‌ای.
   - آفلاین شدن دستی.
   - مشاهده لیدهای تخصیص‌یافته به خودش.
   - ثبت گزارش تماس برای هر لید.

2. **پنل مدیر/ادمین**
   - مشاهده لیست مشاوران.
   - مشاهده لیدهای هر مشاور.
   - مشاهده تاریخچه حضور و غیاب.
   - ثبت امتیاز تشویقی یا جریمه برای مشاور.

### 1.2 مفهوم‌های اصلی

| مفهوم | توضیح فرانت |
| --- | --- |
| `IsAvailable` | یعنی مشاور امروز/در این شیفت حضور خود را ثبت کرده است. بدون حضور، آنلاین شدن نباید ممکن باشد. |
| `IsOnline` | یعنی مشاور آماده دریافت لید لحظه‌ای است. بعد از گرفتن لید RealTime بک‌اند او را آفلاین/Busy می‌کند. |
| `OfflineQueue` | لید آفلاین است، قانون سه‌دقیقه‌ای ندارد، اما باید توسط مشاور تعیین‌تکلیف شود. |
| `RealTime` | لید لحظه‌ای است، بعد از تخصیص مهلت تماس سه‌دقیقه‌ای دارد. |
| open Offline lead | لید آفلاین تخصیص‌یافته به مشاور که هنوز گزارش ندارد و وضعیت نهایی نشده است. تا وقتی وجود دارد، مشاور نمی‌تواند آنلاین شود. |

### 1.3 ساعت کاری

ساعت کاری سیستم از **09:00** تا قبل از **21:00** است.

اثر آن روی UI:

- خارج از ساعت کاری، لید جدید باید در صف آفلاین قرار بگیرد.
- اگر مشاور خارج از ساعت کاری گزارش تماس ثبت کند، بک‌اند پیام می‌دهد که گزارش ثبت شد ولی مشاور آنلاین نمی‌شود.
- فرانت می‌تواند خارج ساعت کاری دکمه آنلاین شدن را نمایش دهد، اما بهتر است یک هشدار نشان دهد که احتمالاً مشاور آماده دریافت RealTime نخواهد شد.

---

## 2. قرارداد عمومی پاسخ‌ها

### 2.1 پاسخ Commandها

اکثر endpointهای عملیاتی خروجی `Result` دارند:

```json
{
  "isSuccess": true,
  "message": "متن پیام"
}
```

در خطا:

```json
{
  "isSuccess": false,
  "message": "متن خطا"
}
```

قانون فرانت:

- اگر `isSuccess=true` بود، پیام را به صورت success toast نشان بده.
- اگر `isSuccess=false` بود، پیام را به صورت error toast/modal نشان بده.
- متن پیام‌های فارسی بک‌اند را دستکاری نکن؛ همان را نمایش بده.

### 2.2 پاسخ‌های Paginated

برخی queryها خروجی صفحه‌بندی‌شده دارند:

```json
{
  "items": [],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 10
}
```

قانون فرانت:

- `items` منبع نمایش جدول است.
- `totalCount` برای pagination استفاده شود.
- اگر بک‌اند در بعضی queryها `pageNumber/pageSize` را دقیق برنگرداند، فرانت مقدار local خودش را نگه دارد.

---

## 3. Enumهای لازم در فرانت

### 3.1 LeadAssignmentType

```ts
export enum LeadAssignmentType {
  RealTime = 1,
  OfflineQueue = 2,
}
```

| مقدار | نام | نمایش پیشنهادی | توضیح |
| --- | --- | --- | --- |
| 1 | RealTime | لحظه‌ای | لید فوری با قانون تماس سه‌دقیقه‌ای |
| 2 | OfflineQueue | آفلاین | لید صف آفلاین بدون deadline سه‌دقیقه‌ای |

### 3.2 LeadAssignmentState

```ts
export enum LeadAssignmentState {
  New = 1,
  Assigned = 2,
  Contacted = 3,
  Pending = 4,
  Converted = 5,
  Expired = 6,
  Rejected = 7,
}
```

| مقدار | نام | نمایش پیشنهادی | رفتار UI |
| --- | --- | --- | --- |
| 1 | New | جدید | معمولاً برای لید RealTime قبل از تخصیص؛ در پنل مشاور کمتر دیده می‌شود. |
| 2 | Assigned | تخصیص‌یافته | باید دکمه ثبت گزارش فعال باشد. |
| 3 | Contacted | تماس/پیگیری | گزارش ثبت شده ولی خروجی تبدیل/رد قطعی نیست. |
| 4 | Pending | در انتظار | لید آفلاین هنوز مشاور نگرفته؛ معمولاً در لیست مشاور دیده نمی‌شود مگر API برگرداند. |
| 5 | Converted | تبدیل‌شده | وضعیت نهایی؛ دکمه ثبت گزارش غیرفعال باشد. |
| 6 | Expired | منقضی | وضعیت نهایی؛ دکمه ثبت گزارش غیرفعال باشد. |
| 7 | Rejected | رد شده | وضعیت نهایی؛ دکمه ثبت گزارش غیرفعال باشد. |

### 3.3 LeadCallResult

```ts
export enum LeadCallResult {
  Contacted = 1,
  Converted = 2,
  Rejected = 3,
  NoAnswer = 4,
  WrongNumber = 5,
  NeedFollowUp = 6,
}
```

| مقدار | نام | label پیشنهادی | وضعیت بعد از submit |
| --- | --- | --- | --- |
| 1 | Contacted | تماس برقرار شد | Contacted |
| 2 | Converted | تبدیل شد | Converted |
| 3 | Rejected | رد شد | Rejected |
| 4 | NoAnswer | پاسخ نداد | Contacted |
| 5 | WrongNumber | شماره اشتباه | Rejected |
| 6 | NeedFollowUp | نیاز به پیگیری | Contacted |

### 3.4 AttendanceStatus

```ts
export enum AttendanceStatus {
  Present = 1,
  Absent = 2,
  Leave = 3,
  SickLeave = 4,
  Late = 5,
  Mission = 6,
}
```

| مقدار | نمایش پیشنهادی |
| --- | --- |
| 1 | حاضر |
| 2 | غایب |
| 3 | مرخصی |
| 4 | مرخصی استعلاجی |
| 5 | تأخیر |
| 6 | مأموریت |

### 3.5 ScoreReason برای امتیاز مدیریتی

برای endpoint امتیاز مدیریتی فقط این دو مقدار استفاده شود:

```ts
export enum ScoreReason {
  ManagerReward = 5,
  ManagerPenalty = 6,
}
```

| مقدار | نام | قانون فرم |
| --- | --- | --- |
| 5 | ManagerReward | `scoreValue` باید صفر یا مثبت باشد. |
| 6 | ManagerPenalty | `scoreValue` باید صفر یا منفی باشد. |

---

## 4. TypeScript DTOهای پیشنهادی

### 4.1 Result

```ts
export interface ApiResult {
  isSuccess: boolean;
  message: string;
}
```

### 4.2 PaginatedResult

```ts
export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}
```

### 4.3 Lead item

```ts
export interface LeadAssignmentItem {
  id: number;
  userName: string;
  phoneNumber: string;
  leadAssignmentState: LeadAssignmentState;
  leadAssignmentType: LeadAssignmentType;
}
```

> `id` همان `leadAssignmentId` است که برای ثبت گزارش تماس باید ارسال شود.

### 4.4 Commands

```ts
export interface SetAvailableCommand {
  profileId: number;
  isAvailable: boolean;
}

export interface SetOnlineOfflineCommand {
  profileId: number;
  isOnline: boolean;
  isOffline?: boolean;
}

export interface SubmitLeadCallReportCommand {
  leadAssignmentId: number;
  consultantProfileId: number;
  callResult: LeadCallResult;
  reportDescription: string;
}

export interface ScoreLogCommand {
  consultantProfileId: number;
  source?: number;
  reason: ScoreReason;
  scoreValue: number;
  description?: string | null;
  leadAssignmentId?: number | null;
  createdByUserId?: string | null;
}
```

---

## 5. APIهای حضور، آنلاین/آفلاین و لید

## 5.1 دریافت لیست مشاوران

### آدرس

`GET /api/Consultant/GetConsultants`

### Query string

```http
GET /api/Consultant/GetConsultants?pageNumber=1&pageSize=10&firstName=علی&lastName=رضایی&phoneNumber=09120000000
```

| پارامتر | نوع | اجباری | توضیح |
| --- | --- | --- | --- |
| `firstName` | string | خیر | فیلتر نام |
| `lastName` | string | خیر | فیلتر نام خانوادگی |
| `phoneNumber` | string | خیر | فیلتر موبایل |
| `pageNumber` | number | خیر | پیش‌فرض 1 |
| `pageSize` | number | خیر | پیش‌فرض 10 |

### خروجی نمونه

```json
{
  "items": [
    {
      "id": "2d5f0e3f-2f8a-4a89-9d2a-000000000000",
      "firstName": "علی",
      "lastName": "رضایی",
      "phoneNumber": "09120000000",
      "profileId": 12,
      "attendanceResponse": null,
      "scoreLogResponse": null,
      "leadsAssignmentItemsResponse": null
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 10
}
```

### استفاده در فرانت

- در پنل مدیر برای انتخاب مشاور استفاده شود.
- در پنل مشاور، اگر `profileId` در token/local storage ندارید، می‌توانید بر اساس کاربر لاگین‌شده یا لیست مشاوران آن را resolve کنید؛ بهترین حالت این است که بعداً از سمت بک‌اند profileId در auth response ارائه شود.

> محدودیت فعلی API: وضعیت‌های `isAvailable` و `isOnline` در خروجی این endpoint برگردانده نمی‌شود. بنابراین فرانت برای نمایش وضعیت واقعی باید بعد از هر action، وضعیت local را از روی action موفق و پیام بک‌اند sync کند یا از بک‌اند درخواست توسعه response بدهد.

---

## 5.2 ثبت حضور مشاور

### آدرس

`POST /api/Consultant/SetAvalableConsultant`

> نام route در بک‌اند `SetAvalableConsultant` است، نه `SetAvailableConsultant`.

### Request

```json
{
  "profileId": 12,
  "isAvailable": true
}
```

### Response موفق

```json
{
  "isSuccess": true,
  "message": "حضور شما ثبت شد"
}
```

### رفتار بک‌اند بعد از ثبت حضور

بعد از ثبت حضور:

1. `IsAvailable=true` می‌شود.
2. `WorkStartTime` با زمان فعلی ذخیره می‌شود.
3. بک‌اند بلافاصله تخصیص لیدهای آفلاین Pending را اجرا می‌کند.
4. هر مشاور واجد شرایط در هر batch حداکثر ۵ لید آفلاین می‌گیرد.

### رفتار پیشنهادی UI

بعد از success:

1. Toast موفقیت نمایش بده.
2. دکمه «ثبت حضور» را disabled یا تبدیل به «ثبت خروج» کن.
3. دکمه «آنلاین شدن» را فعال کن.
4. لیست لیدها را refresh کن؛ چون ممکن است همان لحظه لیدهای آفلاین به مشاور تخصیص داده شده باشند.

---

## 5.3 ثبت عدم حضور / پایان شیفت

### آدرس

`POST /api/Consultant/SetAvalableConsultant`

### Request

```json
{
  "profileId": 12,
  "isAvailable": false
}
```

### Response موفق

```json
{
  "isSuccess": true,
  "message": "عدم حضور شما ثبت شد"
}
```

### رفتار بک‌اند

- `IsAvailable=false`
- `IsOnline=false`
- ثبت `WorkEndTime`
- ثبت `LastOfflineAt`

### رفتار پیشنهادی UI

بعد از success:

- وضعیت حضور را «خارج از شیفت/عدم حضور» نشان بده.
- دکمه آنلاین شدن را disabled کن.
- اگر لید باز باقی مانده، همچنان در جدول نمایش بده ولی برای شروع شیفت بعدی کاربر را ملزم به تعیین تکلیف کن.

---

## 5.4 آنلاین شدن مشاور

### آدرس

`POST /api/Consultant/SetOnlineOfflineConsultant`

### Request

```json
{
  "profileId": 12,
  "isOnline": true,
  "isOffline": false
}
```

### Response موفق

```json
{
  "isSuccess": true,
  "message": "شما آنلاین شدید"
}
```

### خطاهای مهم

اگر مشاور حضور ثبت نکرده باشد:

```json
{
  "isSuccess": false,
  "message": "ابتدا حضور خود را ثبت کنید"
}
```

اگر مشاور لید آفلاین باز داشته باشد:

```json
{
  "isSuccess": false,
  "message": "ابتدا لیدهای آفلاین خود را تعیین تکلیف کنید"
}
```

اگر پروفایل کامل نباشد:

```json
{
  "isSuccess": false,
  "message": "پروفایل مشاور کامل نیست"
}
```

### رفتار پیشنهادی UI

- اگر success بود، badge وضعیت را «آنلاین» کن.
- اگر پیام لید آفلاین باز برگشت، کاربر را به تب «لیدهای آفلاین باز» هدایت کن.
- اگر مشاور آنلاین شد، بهتر است هر ۳۰ تا ۶۰ ثانیه لیست لیدها refresh شود؛ چون ممکن است BackgroundService لید جدید assign کند.

---

## 5.5 آفلاین شدن مشاور

### آدرس

`POST /api/Consultant/SetOnlineOfflineConsultant`

### Request

```json
{
  "profileId": 12,
  "isOnline": false,
  "isOffline": true
}
```

### Response موفق

```json
{
  "isSuccess": true,
  "message": "شما آفلاین شدید"
}
```

### رفتار پیشنهادی UI

- badge وضعیت را «آفلاین» کن.
- دکمه «آنلاین شدن» را فعال نگه دار اگر کاربر هنوز حاضر است.
- اگر کاربر لید باز دارد، هشدار بده که باید تعیین تکلیف کند.

---

## 5.6 دریافت لیدهای مشاور

### آدرس

`GET /api/Consultant/GetLeads`

### Query string نمونه

```http
GET /api/Consultant/GetLeads?profileId=12&pageNumber=1&pageSize=10
```

با فیلتر نوع و وضعیت:

```http
GET /api/Consultant/GetLeads?profileId=12&leadAssignmentType=2&leadAssignmentState=2&pageNumber=1&pageSize=10
```

| پارامتر | نوع | اجباری | توضیح |
| --- | --- | --- | --- |
| `profileId` | number | بله | شناسه پروفایل مشاور |
| `leadAssignmentState` | number | خیر | فیلتر وضعیت لید |
| `leadAssignmentType` | number | خیر | فیلتر نوع لید |
| `pageNumber` | number | خیر | شماره صفحه |
| `pageSize` | number | خیر | تعداد ردیف |

### Response نمونه

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

### ستون‌های پیشنهادی جدول لیدها

| ستون | مقدار |
| --- | --- |
| نام مخاطب | `userName` |
| شماره تماس | `phoneNumber` |
| نوع لید | map از `leadAssignmentType` |
| وضعیت لید | map از `leadAssignmentState` |
| عملیات | دکمه «ثبت گزارش» برای لیدهای قابل گزارش |

### فیلترهای پیشنهادی UI

1. همه لیدها.
2. لیدهای آفلاین باز: `leadAssignmentType=2` و در سمت فرانت وضعیت‌های غیرنهایی را جدا کن.
3. لیدهای لحظه‌ای: `leadAssignmentType=1`.
4. لیدهای نیازمند گزارش: وضعیت `Assigned`.
5. لیدهای نهایی: `Converted`, `Rejected`, `Expired`.

> محدودیت فعلی: API فقط بعضی stateها را در handler فیلتر می‌کند و اگر فیلتر کامل نیاز دارید، بهتر است سمت فرانت هم after-fetch فیلتر تکمیلی انجام دهید یا بک‌اند کامل‌تر شود.

---

## 5.7 ثبت گزارش تماس لید

### آدرس

`POST /api/Consultant/SubmitLeadCallReport`

### Request

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
| `leadAssignmentId` | number | بله | همان `id` از لیست لیدها |
| `consultantProfileId` | number | بله | profileId مشاور |
| `callResult` | number | بله | enum نتیجه تماس |
| `reportDescription` | string | خیر | توضیحات مشاور |

### Responseهای موفق

اگر هنوز لید آفلاین باز وجود دارد:

```json
{
  "isSuccess": true,
  "message": "گزارش ثبت شد، اما هنوز لید آفلاین تعیین‌تکلیف‌نشده دارید"
}
```

اگر داخل ساعت کاری است و دیگر لید آفلاین باز وجود ندارد:

```json
{
  "isSuccess": true,
  "message": "گزارش ثبت شد و شما به صورت خودکار آنلاین شدید"
}
```

اگر خارج از ساعت کاری است:

```json
{
  "isSuccess": true,
  "message": "گزارش ثبت شد، اما خارج از ساعت کاری هستید"
}
```

### Responseهای خطا

گزارش تکراری:

```json
{
  "isSuccess": false,
  "message": "گزارش این لید قبلا ثبت شده است"
}
```

لید RealTime منقضی شده:

```json
{
  "isSuccess": false,
  "message": "مهلت ثبت گزارش این لید به پایان رسیده است"
}
```

لید متعلق به مشاور نیست یا وجود ندارد:

```json
{
  "isSuccess": false,
  "message": "لید یافت نشد"
}
```

### رفتار پیشنهادی UI بعد از submit

بعد از هر response موفق:

1. Modal ثبت گزارش را ببند.
2. Toast پیام بک‌اند را نشان بده.
3. لیست لیدها را refresh کن.
4. اگر پیام «خودکار آنلاین شدید» بود، وضعیت local مشاور را online کن.
5. اگر پیام «هنوز لید آفلاین...» یا «خارج از ساعت کاری...» بود، وضعیت local مشاور را offline نگه دار.

---

## 5.8 دریافت تاریخچه حضور و غیاب

### آدرس

`GET /api/Attendance`

### Query string نمونه

```http
GET /api/Attendance?consultantProfileId=12&pageNumber=1&pageSize=10
```

| پارامتر | نوع | اجباری | توضیح |
| --- | --- | --- | --- |
| `consultantProfileId` | number | بله | شناسه پروفایل مشاور |
| `pageNumber` | number | خیر | شماره صفحه |
| `pageSize` | number | خیر | تعداد ردیف |

### Response نمونه

```json
{
  "items": [
    {
      "id": 1,
      "attendanceDate": "1405/03/29",
      "checkInTime": "09:10",
      "checkOutTime": "18:30",
      "status": 1,
      "description": null
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 10
}
```

### استفاده در UI

- تب «تاریخچه حضور» در پروفایل مشاور.
- ستون‌های پیشنهادی: تاریخ، ورود، خروج، وضعیت، توضیحات.

---

## 5.9 ثبت امتیاز مدیریتی مشاور

### آدرس

`POST /api/ScoreLog`

### Request تشویقی

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

### Request جریمه

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

### Response موفق

```json
{
  "isSuccess": true,
  "message": "امتیاز با موفقیت ثبت شد"
}
```

### خطاهای validation

- اگر reason غیر از 5 و 6 باشد: فقط امتیاز تشویقی یا جریمه مدیر قابل ثبت است.
- اگر reward منفی باشد: امتیاز تشویقی مدیر باید مثبت باشد.
- اگر penalty مثبت باشد: امتیاز جریمه مدیر باید منفی باشد.

---

## 6. صفحه پیشنهادی پنل مشاور

### 6.1 Header وضعیت مشاور

نمایش پیشنهادی:

- نام مشاور.
- شماره تماس.
- وضعیت حضور: حاضر / خارج از شیفت.
- وضعیت دریافت لید: آنلاین / آفلاین / مشغول.
- تعداد لیدهای آفلاین باز.

> چون بک‌اند فعلی `isAvailable/isOnline` را در endpoint لیست مشاوران برنمی‌گرداند، وضعیت‌ها را فعلاً از actionهای موفق local نگه دارید و با refresh لیدها وضعیت «مشغول/دارای لید باز» را derive کنید.

### 6.2 دکمه‌ها

| دکمه | چه زمانی فعال باشد | API |
| --- | --- | --- |
| ثبت حضور | وقتی local `isAvailable=false` است | `SetAvalableConsultant` با `isAvailable=true` |
| ثبت خروج | وقتی local `isAvailable=true` است | `SetAvalableConsultant` با `isAvailable=false` |
| آنلاین شدن | وقتی حاضر است و open OfflineQueue ندارد | `SetOnlineOfflineConsultant` با `isOnline=true` |
| آفلاین شدن | وقتی online است | `SetOnlineOfflineConsultant` با `isOnline=false` |
| Refresh لیدها | همیشه | `GetLeads` |

### 6.3 محاسبه open OfflineQueue در فرانت

اگر لیست لیدها را دارید، open offline را این‌طور حساب کنید:

```ts
function isFinalLeadState(state: LeadAssignmentState): boolean {
  return [
    LeadAssignmentState.Converted,
    LeadAssignmentState.Rejected,
    LeadAssignmentState.Expired,
  ].includes(state);
}

function hasOpenOfflineLead(leads: LeadAssignmentItem[]): boolean {
  return leads.some(
    l => l.leadAssignmentType === LeadAssignmentType.OfflineQueue && !isFinalLeadState(l.leadAssignmentState)
  );
}
```

> تعریف دقیق بک‌اند برای open OfflineQueue علاوه بر state، `ReportSubmittedAt == null` هم دارد؛ چون این فیلد در خروجی فعلی `GetLeads` نیست، فرانت با state approximation کار کند یا برای نمایش دقیق‌تر درخواست توسعه API بدهد.

### 6.4 وضعیت «مشغول» برای RealTime

اگر مشاور لید `RealTime` با وضعیت `Assigned` دارد، UI او را «مشغول تماس» نشان بده و دکمه آنلاین شدن را غیرفعال کن تا گزارش ثبت شود.

```ts
function hasActiveRealTime(leads: LeadAssignmentItem[]): boolean {
  return leads.some(
    l => l.leadAssignmentType === LeadAssignmentType.RealTime &&
         l.leadAssignmentState === LeadAssignmentState.Assigned
  );
}
```

---

## 7. صفحه مدیریت لیدهای مشاور

### 7.1 تب‌های پیشنهادی

1. **همه لیدها**
2. **لیدهای نیازمند اقدام**: `Assigned`
3. **لیدهای آفلاین باز**: `OfflineQueue` و state غیرنهایی
4. **لیدهای RealTime**
5. **لیدهای موفق**: `Converted`
6. **لیدهای رد/منقضی**: `Rejected`, `Expired`

### 7.2 دکمه ثبت گزارش

دکمه ثبت گزارش فقط وقتی فعال باشد که:

- `leadAssignmentState === Assigned`
- یا اگر تصمیم محصول این است که `Contacted/NeedFollowUp` هم دوباره پیگیری شود، باید بک‌اند امکان گزارش چندباره بدهد؛ در بک‌اند فعلی گزارش تکراری رد می‌شود، پس دکمه برای `Contacted` فعال نشود.

### 7.3 فرم ثبت گزارش

فیلدها:

- نتیجه تماس: dropdown از `LeadCallResult`.
- توضیحات: textarea.
- دکمه ثبت.

Validation فرانت:

- `callResult` اجباری.
- `leadAssignmentId` از row جدول.
- `consultantProfileId` از context کاربر مشاور.
- `reportDescription` می‌تواند خالی باشد، ولی پیشنهاد می‌شود برای `Rejected`, `NoAnswer`, `NeedFollowUp` اجباری شود.

### 7.4 refresh strategy

- بعد از ثبت حضور: `GetLeads` را صدا بزن.
- بعد از آنلاین شدن: هر ۳۰ تا ۶۰ ثانیه `GetLeads` را refresh کن.
- بعد از ثبت گزارش: `GetLeads` را refresh کن.
- وقتی modal ثبت گزارش باز است، از refresh خودکار که state فرم را خراب کند جلوگیری کن.

---

## 8. UX پیشنهادی برای سناریوهای اصلی

### 8.1 شروع روز مشاور

1. کاربر وارد پنل می‌شود.
2. فرانت `GetLeads` را برای profileId می‌گیرد.
3. کاربر «ثبت حضور» را می‌زند.
4. بک‌اند لیدهای آفلاین را همان لحظه assign می‌کند.
5. فرانت بعد از success لیست لیدها را refresh می‌کند.
6. اگر لید آفلاین باز وجود داشت، دکمه آنلاین شدن disabled و پیام «ابتدا لیدهای آفلاین را تعیین تکلیف کنید» نشان داده شود.
7. اگر لید آفلاین باز نبود، دکمه آنلاین شدن فعال باشد.

### 8.2 آنلاین شدن و دریافت لید لحظه‌ای

1. کاربر «آنلاین شدن» را می‌زند.
2. اگر success شد، badge آنلاین نمایش داده شود.
3. بک‌اند در cycle تخصیص، اگر لید RealTime برسد آن را به مشاور می‌دهد و مشاور را آفلاین/busy می‌کند.
4. فرانت با polling لید جدید Assigned را می‌بیند.
5. UI باید کارت/هشدار «لید جدید برای تماس» نشان دهد.
6. مشاور گزارش تماس را ثبت می‌کند.
7. اگر ساعت کاری باشد و لید آفلاین باز نداشته باشد، بک‌اند او را دوباره آنلاین می‌کند.

### 8.3 مدیریت لید آفلاین

1. لیدهای آفلاین بعد از حضور یا BackgroundService به مشاور assign می‌شوند.
2. این لیدها deadline سه‌دقیقه‌ای ندارند.
3. مشاور باید برای تک‌تک آن‌ها گزارش ثبت کند.
4. تا وقتی حداقل یک لید آفلاین باز باقی مانده، مشاور نمی‌تواند آنلاین شود.
5. بعد از آخرین گزارش آفلاین، اگر داخل ساعت کاری باشد، بک‌اند مشاور را آنلاین می‌کند.

### 8.4 پایان روز

1. مشاور «آفلاین شدن» را می‌زند، اگر آنلاین است.
2. مشاور «ثبت خروج/عدم حضور» را می‌زند.
3. فرانت وضعیت local را حاضر=false و online=false کند.

---

## 9. پیام‌ها و رفتار پیشنهادی فرانت

| پیام بک‌اند | رفتار پیشنهادی UI |
| --- | --- |
| حضور شما ثبت شد | success toast، refresh لیدها، فعال کردن کنترل آنلاین شدن |
| عدم حضور شما ثبت شد | success toast، وضعیت خارج از شیفت |
| شما آنلاین شدید | success toast، badge آنلاین |
| شما آفلاین شدید | success toast، badge آفلاین |
| ابتدا حضور خود را ثبت کنید | error toast، highlight دکمه ثبت حضور |
| ابتدا لیدهای آفلاین خود را تعیین تکلیف کنید | error toast، هدایت به تب لیدهای آفلاین باز |
| گزارش ثبت شد، اما هنوز لید آفلاین تعیین‌تکلیف‌نشده دارید | success/warning toast، refresh جدول، نگه داشتن offline |
| گزارش ثبت شد و شما به صورت خودکار آنلاین شدید | success toast، badge آنلاین، refresh جدول |
| گزارش ثبت شد، اما خارج از ساعت کاری هستید | success/warning toast، badge آفلاین |
| گزارش این لید قبلا ثبت شده است | error toast، refresh جدول برای sync |
| مهلت ثبت گزارش این لید به پایان رسیده است | error toast، refresh جدول برای نمایش Expired |

---

## 10. محدودیت‌های فعلی API که فرانت باید بداند

1. `GetConsultants` در خروجی فعلی `isAvailable` و `isOnline` نمی‌دهد؛ برای نمایش دقیق وضعیت آنلاین/حضور بهتر است بک‌اند بعداً این دو فیلد را اضافه کند.
2. `GetLeads` در خروجی فعلی `assignedAt`, `callDeadlineAt`, `reportSubmittedAt`, `contactedAt`, `callResult`, `reportDescription` نمی‌دهد. بنابراین countdown سه‌دقیقه‌ای دقیق برای RealTime از خروجی فعلی قابل پیاده‌سازی نیست.
3. `GetLeads` فیلتر همه state/typeها را به شکل کامل و generic پیاده نکرده است؛ اگر خروجی دقیق نیاز دارید، سمت فرانت فیلتر تکمیلی انجام دهید یا بک‌اند کامل‌تر شود.
4. Route ثبت حضور غلط املایی دارد: `SetAvalableConsultant`؛ فرانت باید همین route را صدا بزند.
5. `isOffline` در command آنلاین/آفلاین وجود دارد، اما منطق بک‌اند عملاً با `isOnline` تصمیم می‌گیرد؛ با این حال برای سازگاری، فرانت مقدار مناسب `isOffline` را هم ارسال کند.

---

## 11. ساختار پیشنهادی Angular

> این بخش پیشنهادی است و نیاز به تغییر بک‌اند ندارد.

### 11.1 Serviceها

```text
src/app/core/services/consultant.service.ts
src/app/core/services/lead-assignment.service.ts
src/app/core/services/attendance.service.ts
src/app/core/services/score-log.service.ts
```

### 11.2 Componentهای پیشنهادی

```text
consultant-dashboard/
  consultant-status-card.component.ts
  consultant-lead-table.component.ts
  lead-report-dialog.component.ts
  attendance-history.component.ts

admin-consultants/
  consultant-list.component.ts
  consultant-detail.component.ts
  manager-score-dialog.component.ts
```

### 11.3 نمونه متدهای Service

```ts
getConsultants(params: GetConsultantsParams): Observable<PaginatedResult<ConsultantItem>>;
setAvailable(command: SetAvailableCommand): Observable<ApiResult>;
setOnlineOffline(command: SetOnlineOfflineCommand): Observable<ApiResult>;
getConsultantLeads(params: GetLeadsParams): Observable<PaginatedResult<LeadAssignmentItem>>;
submitLeadReport(command: SubmitLeadCallReportCommand): Observable<ApiResult>;
getAttendances(params: GetAttendancesParams): Observable<PaginatedResult<AttendanceItem>>;
setAdminScore(command: ScoreLogCommand): Observable<ApiResult>;
```

---

## 12. چک‌لیست QA فرانت

1. **ثبت حضور:** با `isAvailable=true` پیام موفق بیاید و لیست لیدها refresh شود.
2. **تخصیص آفلاین بعد از حضور:** اگر لید آفلاین Pending وجود دارد، بعد از ثبت حضور حداکثر ۵ لید به مشاور نمایش داده شود.
3. **ممانعت آنلاین شدن:** اگر لید آفلاین باز وجود دارد، آنلاین شدن باید خطا بدهد و UI کاربر را به لیست لیدهای آفلاین هدایت کند.
4. **ثبت گزارش OfflineQueue:** با ثبت یک گزارش، اگر هنوز لید آفلاین باز هست، مشاور offline بماند.
5. **آخرین گزارش OfflineQueue:** اگر آخرین لید آفلاین در ساعت کاری تعیین تکلیف شد، پیام auto online نمایش داده شود.
6. **ثبت گزارش RealTime:** بعد از ثبت گزارش موفق در ساعت کاری، اگر لید آفلاین باز نیست، مشاور auto online شود.
7. **گزارش تکراری:** دکمه ثبت گزارش برای stateهای نهایی غیرفعال باشد؛ اگر باز هم خطای تکراری آمد، جدول refresh شود.
8. **پایان حضور:** با `isAvailable=false` وضعیت local حضور و آنلاین هر دو false شود.
9. **تاریخچه حضور:** endpoint `/api/Attendance` در تب تاریخچه نمایش داده شود.
10. **امتیاز مدیریتی:** reward فقط مثبت و penalty فقط منفی ارسال شود.

---

## 13. خلاصه مسیرهای API

| کاربرد | Method | URL |
| --- | --- | --- |
| ورود | POST | `/api/Auth/Login` |
| دریافت کاربر جاری | GET | `/api/Auth/Me` |
| دریافت مشاوران | GET | `/api/Consultant/GetConsultants` |
| ثبت حضور/عدم حضور | POST | `/api/Consultant/SetAvalableConsultant` |
| آنلاین/آفلاین شدن | POST | `/api/Consultant/SetOnlineOfflineConsultant` |
| دریافت لیدهای مشاور | GET | `/api/Consultant/GetLeads` |
| ثبت گزارش تماس | POST | `/api/Consultant/SubmitLeadCallReport` |
| تاریخچه حضور | GET | `/api/Attendance` |
| ثبت امتیاز مدیر | POST | `/api/ScoreLog` |
