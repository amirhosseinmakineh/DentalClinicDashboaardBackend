# Frontend PR: Realtime Lead Pickup — drsaeedMoghadamFront

## برنچ

```
cursor/realtime-lead-pickup-alerts-fca0
```

## ریپوی درست

**`drsaeedMoghadamFront`** — نه `dentalClinickDashboard`

مسیر لوکال شما:
`D:\DentalDashboard\DentalDashboardFront\drsaeedMoghadamFront`

## چرا `git switch` خطا داد؟

برنچ فقط commit محلی بود و به GitHub push نشده بود.

## دستورات (PowerShell)

```powershell
cd D:\DentalDashboard\DentalDashboardFront\drsaeedMoghadamFront
git fetch origin
git checkout main
git pull origin main
git checkout -b cursor/realtime-lead-pickup-alerts-fca0

# اعمال patch از بکند (نسخه v2 با فیکس نوتیف):
git apply D:\DentalDashboard\DentalClinicDashboaardBackend\docs\patches\drsaeed-realtime-lead-pickup-frontend.patch

git add -A
git status
git commit -m "Fix realtime and offline push notifications"
git push -u origin cursor/realtime-lead-pickup-alerts-fca0
```

## فیکس‌های نسخه v2

1. مسیر `offline_leads` در service worker جدا و بدون تداخل با realtime
2. لید آنلاین بدون چک API قبل از نمایش overlay (چک فقط هنگام کلیک برداریدش)
3. خطای API `CanPickupLead` دیگر نوتیف را مخفی نمی‌کند
4. `handleForegroundMessage` برای RealtimeLead مستقیماً overlay را باز می‌کند

## لینک PR (بعد از push)

https://github.com/amirhosseinmakineh/drsaeedMoghadamFront/compare/main...cursor/realtime-lead-pickup-alerts-fca0

## وابستگی بکند

برنچ بکند: `cursor/realtime-lead-frontend-support-fca0`

PR بکند:
https://github.com/amirhosseinmakineh/DentalClinicDashboaardBackend/compare/main...cursor/realtime-lead-frontend-support-fca0

## فایل‌های تغییر یافته

- `public/web-push-scope/web-push-sw.js`
- `src/app/core/lead/realtime-lead-pickup.service.ts` (جدید)
- `src/app/core/lead/realtime-lead-alert.service.ts` (جدید)
- `src/app/shared/ui/realtime-lead-alert/realtime-lead-alert.component.ts` (جدید)
- `src/app/core/push/notification.service.ts`
- `src/app/core/push/push-notification.service.ts`
- `src/app/app.component.ts`
