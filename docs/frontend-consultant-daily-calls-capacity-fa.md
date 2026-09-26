# راهنمای فرانت: تماس‌های امروز و ظرفیت روزانه مشاور

## هدف

در داشبورد مشاور، تعداد لیدهایی که مشاور **امروز با آن‌ها تماس را شروع کرده** و ظرفیت روزانه او نمایش داده شود. مبنای «امروز»، روز تقویم ایران است.

## API

```http
GET /api/Consultant/GetDashboardStatus?ProfileId={consultantProfileId}
Authorization: Bearer {accessToken}
```

پاسخ این endpoint مستقیم یک object است (داخل `data` پیچیده نشده است). فیلدهای قبلی بدون تغییر مانده‌اند و فیلدهای زیر اضافه شده‌اند:

| فیلد | نوع | توضیح |
|---|---:|---|
| `todayCallsCount` | `number` | تعداد لیدهای این مشاور که `RecordLeadCallInitiated` آن‌ها در روز جاری ایران ثبت شده است |
| `dailyLimit` | `number` | سقف مؤثر روزانه؛ همان `LimitNumber` مشاور و در صورت null بودن، مقدار پیش‌فرض سیستم (فعلاً ۱۰) |
| `todayPickupCount` | `number` | تعداد لیدهای لحظه‌ای برداشته‌شده توسط مشاور در روز جاری ایران |
| `remainingDailyCapacity` | `number` | ظرفیت باقی‌مانده برای برداشتن لید؛ حداقل صفر و برابر `dailyLimit - todayPickupCount` |

نمونه پاسخ:

```json
{
  "profileId": 42,
  "isAvailable": true,
  "isOnline": true,
  "lastOnlineAt": "2026-08-24T06:15:00Z",
  "lastOfflineAt": null,
  "canGoOnline": true,
  "onlineStatusBlockReason": null,
  "todayReservationsCount": 3,
  "todayCallsCount": 7,
  "dailyLimit": 10,
  "todayPickupCount": 6,
  "remainingDailyCapacity": 4
}
```

> `todayCallsCount` و `todayPickupCount` عمداً دو مفهوم متفاوت‌اند: اولی تماس واقعاً شروع‌شده را می‌شمارد و دومی مصرف ظرفیت دریافت لید لحظه‌ای را. برای نمایش ظرفیت از `dailyLimit` و `remainingDailyCapacity` استفاده کنید.

## تغییر پیشنهادی مدل TypeScript

```ts
export interface ConsultantStatusSnapshot {
  profileId: number;
  isAvailable: boolean;
  isOnline: boolean;
  canGoOnline?: boolean;
  onlineStatusBlockReason?: string | null;
  todayReservationsCount: number;
  todayCallsCount: number;
  dailyLimit: number;
  todayPickupCount: number;
  remainingDailyCapacity: number;
}
```

اگر لایه نرمال‌سازی پروژه هر دو حالت PascalCase و camelCase را پشتیبانی می‌کند، نگاشت‌های زیر را نیز اضافه کنید:

```ts
todayCallsCount: Number(data?.todayCallsCount ?? data?.TodayCallsCount ?? 0),
dailyLimit: Number(data?.dailyLimit ?? data?.DailyLimit ?? 0),
todayPickupCount: Number(data?.todayPickupCount ?? data?.TodayPickupCount ?? 0),
remainingDailyCapacity: Number(
  data?.remainingDailyCapacity ?? data?.RemainingDailyCapacity ?? 0
),
```

## پیشنهاد UI داشبورد

دو کارت آماری کنار هم نمایش داده شود:

1. **تماس‌های امروز**: مقدار `todayCallsCount`
2. **ظرفیت روزانه**: متن `${remainingDailyCapacity} از ${dailyLimit} باقی‌مانده`

برای نوار پیشرفت ظرفیت می‌توان از فرمول زیر استفاده کرد (حالت limit صفر نیز کنترل شده است):

```ts
const usedCapacityPercent = dailyLimit > 0
  ? Math.min(100, Math.round((todayPickupCount / dailyLimit) * 100))
  : 100;
```

پس از برداشتن موفق لید یا ثبت شروع تماس، endpoint وضعیت داشبورد دوباره فراخوانی شود تا اعداد به‌روز شوند. همچنین هنگام بازگشت فوکوس به صفحه یا با polling متناسب با معماری فعلی، refresh انجام شود.

## نکات پذیرش

- تغییر روز بر اساس ساعت ایران انجام می‌شود، نه ساعت مرورگر کاربر.
- تماس تکراری روی یک لید، چون زمان شروع تماس فقط بار اول ثبت می‌شود، شمارنده را دوباره افزایش نمی‌دهد.
- اگر ادمین `LimitNumber` را null گذاشته باشد، فرانت مقدار `dailyLimit` بازگشتی را نمایش دهد و مقدار پیش‌فرض را hard-code نکند.
- `remainingDailyCapacity` هیچ‌وقت منفی نیست.
- برای مشاوری با limit صفر، ظرفیت باقی‌مانده صفر و امکان دریافت لید جدید بسته است.
