# Frontend PR: Realtime Lead Pickup — drsaeedMoghadamFront

## برنچ

```
cursor/realtime-lead-pickup-alerts-fca0
```

## ریپوی درست

**`drsaeedMoghadamFront`** — نه `dentalClinickDashboard`

مسیر لوکال شما:
`D:\DentalDashboard\DentalDashboardFront\drsaeedMoghadamFront`

## دستورات (PowerShell)

```powershell
cd D:\DentalDashboard\DentalDashboardFront\drsaeedMoghadamFront
git fetch origin
git checkout main
git pull origin main
git checkout -b cursor/realtime-lead-pickup-alerts-fca0

# اعمال patch از بکند (نسخه v3):
git apply D:\DentalDashboard\DentalClinicDashboaardBackend\docs\patches\drsaeed-realtime-lead-pickup-frontend.patch

git add -A
git status
git commit -m "Fix offline and realtime push notifications (v3)"
git push -u origin cursor/realtime-lead-pickup-alerts-fca0
```

## فیکس‌های نسخه v3

1. مسیر `offline_leads` در service worker دقیقاً مثل `main` برگردانده شد (inline، بدون refactor مشترک)
2. هندلر foreground آفلاین از realtime جدا شد (`dispatchConsultantPushMessage`)
3. `RealtimeLead` در service worker و overlay پشتیبانی می‌شود
4. `SW_VERSION` به `2026-07-09-realtime-pickup-v3` تغییر کرد — بعد از deploy حتماً PWA را ببندید و دوباره باز کنید

## بعد از deploy

1. PWA را کامل ببندید (نه فقط refresh)
2. دوباره باز کنید و یک‌بار «فعال‌سازی نوتیفیکیشن» را بزنید
3. تست آفلاین: ثبت حضور → باید نوتیف آفلاین بیاید
4. تست آنلاین: آنلاین شدن → dispatch در DB → باید نوتیف «برداریدش» بیاید

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
