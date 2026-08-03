# راهنمای فرانت‌اند: کنترل شماره‌های تعیین‌تکلیف‌نشده مشاور

## هدف تغییر

هنگام باز شدن داشبورد مشاور، فرانت باید وضعیت داشبورد را دریافت کند. بک‌اند اکنون دو وضعیت را کنترل می‌کند:

1. مشاور حداقل یک شماره دارد که گزارش آن هنوز ثبت نشده است؛ فارغ از اینکه تماس شروع شده باشد یا نه.
2. تعداد شماره‌های دارای نتیجه‌ی «نیاز به پیگیری» و وضعیت `Pending` بیشتر از ۱۰ مورد است.

اگر هر کدام برقرار باشد، تخصیص/برداشت شماره‌ی جدید مسدود می‌شود. حد مجاز پیگیری ۱۰ است؛ مسدودی از ۱۱ مورد شروع می‌شود.

## درخواست اولیه داشبورد

در mount شدن صفحه‌ی داشبورد، این endpoint را صدا بزنید:

```http
GET /api/Consultant/GetDashboardStatus?ProfileId={consultantProfileId}
```

فیلدهای جدید پاسخ:

```json
{
  "profileId": 42,
  "canGoOnline": false,
  "onlineStatusBlockReason": "شما 1 شماره بدون گزارش و 11 مورد در حالت پیگیری دارید...",
  "pendingReportCount": 1,
  "uncalledWithoutReportCount": 1,
  "followUpCount": 11,
  "maximumAllowedFollowUps": 10,
  "isNewLeadBlocked": true,
  "shouldShowWorkloadNotification": true,
  "workloadNotificationMessage": "شما 1 شماره بدون گزارش و 11 مورد در حالت پیگیری دارید..."
}
```

## رفتار مورد انتظار UI

- اگر `shouldShowWorkloadNotification === true` بود، یک notification/toast با متن `workloadNotificationMessage` نمایش دهید.
- اگر `isNewLeadBlocked === true` بود، دکمه‌ی آنلاین شدن و UI برداشت شماره‌ی جدید را غیرفعال کنید.
- شمارنده‌های `pendingReportCount` و `followUpCount` را در notification یا کارت هشدار نمایش دهید. `uncalledWithoutReportCount` زیرمجموعه‌ای از `pendingReportCount` است که تماسش هم شروع نشده است.
- CTA هشدار باید کاربر را به لیست شماره‌های خودش ببرد. در این لیست بهتر است ابتدا شماره‌های بدون تماس/گزارش و سپس موارد پیگیری نمایش داده شوند.
- مقدار ۱۰ را در فرانت hard-code نکنید؛ از `maximumAllowedFollowUps` استفاده کنید.
- بعد از ثبت گزارش یا ویرایش نتیجه‌ی یک تماس، endpoint وضعیت داشبورد را دوباره دریافت کنید. وقتی `isNewLeadBlocked` به `false` رسید، کنترل‌های دریافت شماره را دوباره فعال کنید.
- پیام `onlineStatusBlockReason` ممکن است به علت پایان ساعت کاری یا مسدودی workload پر شود؛ همان متن بک‌اند را نمایش دهید.

## Web Push

هنگام ورود به داشبورد در وضعیت مسدود، بک‌اند Web Push زیر را نیز ارسال می‌کند:

```json
{
  "title": "تعیین تکلیف شماره‌های قبلی",
  "data": {
    "type": "ConsultantLeadWorkloadBlocked",
    "route": "/consultant/leads"
  }
}
```

در service worker، برای `type === "ConsultantLeadWorkloadBlocked"` با کلیک روی notification مسیر `data.route` را باز کنید. نمایش toast داخل داشبورد را وابسته به دریافت Web Push نکنید، چون ممکن است کاربر مجوز push نداده باشد؛ پاسخ `GetDashboardStatus` منبع اصلی UI است.

> Web Push بدون ثبت قبلی subscription توسط مرورگر قابل تحویل نیست. فرانت باید حداقل یک بار مجوز notification را بگیرد و `RegisterPushToken` را با subscription معتبر صدا بزند. همچنین ارسال این هشدار زمانی اجرا می‌شود که `GetDashboardStatus` هنگام ورود به داشبورد فراخوانی شود. در مقابل، toast داخل صفحه حتماً به پیاده‌سازی فیلدهای پاسخ در فرانت نیاز دارد.

## مدیریت خطای برداشت هم‌زمان

حتی اگر UI دکمه را غیرفعال کرده باشد، بک‌اند در endpoint برداشت نیز قانون را دوباره بررسی می‌کند:

```http
POST /api/LeadAssignment/{leadAssignmentId}/pickup?consultantProfileId={id}
```

اگر workload مسدود باشد، پاسخ HTTP `423 Locked` با envelope خطای معمول API برمی‌گردد. متن خطای بک‌اند را نمایش دهید و سپس وضعیت داشبورد را refresh کنید. کنترل سمت فرانت جایگزین این کنترل سرور نیست.

## آنلاین شدن

endpoint آنلاین شدن نیز در حالت مسدود پاسخ ناموفق می‌دهد:

```http
POST /api/Consultant/SetOnlineOfflineConsultant
Content-Type: application/json

{
  "profileId": 42,
  "isOnline": true
}
```

در صورت failure، متن پاسخ را نمایش دهید و کاربر را به لیست شماره‌ها هدایت کنید.

## چک‌لیست پذیرش فرانت

- [ ] وضعیت داشبورد در mount و پس از ثبت/ویرایش گزارش refresh می‌شود.
- [ ] notification بر اساس `shouldShowWorkloadNotification` نمایش داده می‌شود.
- [ ] دکمه‌های آنلاین شدن و برداشت لید بر اساس `isNewLeadBlocked` غیرفعال می‌شوند.
- [ ] HTTP 423 برداشت لید مدیریت می‌شود.
- [ ] کلیک Web Push کاربر را به لیست شماره‌های مشاور می‌برد.
- [ ] عدد حد مجاز از `maximumAllowedFollowUps` خوانده می‌شود.
