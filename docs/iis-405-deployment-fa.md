# رفع خطای 405 همه‌ی APIها در IIS

مسیر `POST /api/Auth/Login` در بک‌اند وجود دارد و فقط متد `POST` را می‌پذیرد. اگر درخواست
`POST` هم روی سرور اصلی 405 می‌گیرد، پاسخ قبل از رسیدن به ASP.NET Core توسط IIS، WebDAV،
یا سایت فرانت‌اند تولید شده است؛ این خطا از اکشن Login یا CORS نیست.

فایل `DentalDashboard/web.config` همه‌ی مسیرها و verbها را به
`AspNetCoreModuleV2` می‌فرستد و handler/module مربوط به WebDAV را برای این application حذف
می‌کند.

## استقرار صحیح

1. Hosting Bundle سازگار با .NET 10 را روی سرور IIS نصب یا به‌روزرسانی کنید و سپس اجرا کنید:

   ```powershell
   iisreset
   ```

2. خروجی publish را بسازید؛ سورس پروژه یا فقط DLLها را دستی کپی نکنید:

   ```powershell
   dotnet publish DentalDashboard/DentalDashboard.csproj -c Release -o ./publish
   ```

3. **کل محتوای** پوشه‌ی `publish`، از جمله `web.config`، را در مسیر فیزیکی بک‌اند کپی کنید.

4. در IIS مسیر `/api` را به Virtual Directory جداگانه تبدیل نکنید. سایت/اپلیکیشنی که فایل
   `web.config` و `mywebapp.dll` در ریشه‌ی آن هستند باید درخواست `/api/...` را دریافت کند.
   اگر فرانت‌اند و بک‌اند در دو application جدا هستند، reverse proxy باید `/api/*` را قبل از
   fallback مربوط به SPA به بک‌اند بفرستد.

5. Application Pool را روی **No Managed Code** تنظیم کنید و مطمئن شوید identity آن به پوشه‌ی
   publish دسترسی Read/Execute دارد.

## تشخیص روی خود سرور

با payload واقعی یا آزمایشی، خود endpoint را با `POST` بررسی کنید:

```powershell
curl.exe -i -X POST https://drsaeedmoghadam.com/api/Auth/Login `
  -H "Content-Type: application/json" `
  --data "{\"phoneNumber\":\"test\",\"password\":\"test\"}"
```

- پاسخ 400 یا 200 یعنی درخواست به ASP.NET Core رسیده است.
- پاسخ 405 با هدر `Server: Microsoft-IIS` و بدنه‌ی HTML معمولاً یعنی handler یا WebDAV هنوز
  درخواست را قبل از برنامه گرفته است.
- پاسخ 405 از سایت فرانت‌اند یعنی rule مربوط به SPA یا proxy پیش از route بک‌اند اجرا می‌شود.
- `GET /api/Auth/Login` ذاتاً باید 405 باشد، چون Login عمداً فقط `POST` است؛ برای عیب‌یابی
  حتماً `POST` را تست کنید.

بعد از deploy، وجود `web.config` کنار `mywebapp.dll` و تبدیل پوشه به IIS Application را دوباره
بررسی کنید. در صورت ادامه‌ی خطا، Failed Request Tracing کد 405 را فعال کنید تا نام module تولیدکننده
پاسخ (مانند WebDAVModule یا StaticFileModule) مشخص شود.
