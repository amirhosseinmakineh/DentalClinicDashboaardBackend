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

# اگر patch را از بکند دارید:
git apply D:\DentalDashboard\DentalClinicDashboaardBackend\docs\patches\drsaeed-realtime-lead-pickup-frontend.patch

git add -A
git status
git commit -m "Implement realtime lead pickup alerts with Web Push overlay"
git push -u origin cursor/realtime-lead-pickup-alerts-fca0
```

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
