# Website Lead بدون ستون اضافه

مبنا: `codex-lflotm` در `906eb29bf11d156aba8cd9678fe6bf32d17ccf39`؛ تغییر اخیر کاربر در همین برنچ نیز بررسی شده است. از main استفاده نشده است.

## قرارداد نهایی و رفتار

```http
POST /api/consultation-requests
```

```json
{"fullName":"علی رضایی","phoneNumber":"09123456789"}
```

DTO فقط دو Property دارد. requestId از قرارداد، اعتبارسنجی و ذخیره‌سازی Backend حذف شده است. درخواست‌های فرانت قبلی که فیلد اضافه requestId می‌فرستند، با رفتار پیش‌فرض JSON آن فیلد را نادیده می‌گیرند؛ نه آن را ذخیره می‌کنیم و نه به ConsultantProfileId نسبت می‌دهیم. در این تسک فایل‌های فرانت تغییر نکرده‌اند؛ بررسی سه‌فیلدی قدیمی CI فرانت باید جداگانه با قرارداد جدید هماهنگ شود.

Controller دیگر وابستگی مستقیم به DentalContext، Query جست‌وجوی درخواست یا Replay ندارد. پس از همان اعتبارسنجی نام و نرمال‌سازی شماره، فقط `AddNewLeadsIfPhoneDoesNotExistAsync` موجود را اجرا می‌کند. ثبت اول 200 و `success=true` می‌دهد؛ شماره تکراری نرمال‌شده مطابق Repository موجود 409 می‌گیرد، حتی اگر نام و شماره دقیقاً همان درخواست قبلی باشند. بدون ذخیره کلید درخواست، ادعای idempotency بر اساس requestId وجود ندارد. خطای ذخیره همچنان 503 و محدودیت نرخ همان 429 است.

رکورد با `UserName`، `PhoneNumber`، `SourceType=Website`، `AssignmentType=RealTime`، `LeadAssignmentState=New` و `RequiresThreeMinuteCall=true` ثبت می‌شود. `ConsultantProfileId`، `AssignedAt` و `CallDeadlineAt` هنگام ثبت اولیه خالی‌اند؛ انتخاب مشاور و مهلت تماس همچنان توسط مسیر موجود pickup تعیین می‌شوند. هیچ ارتباطی بین شناسه درخواست و شناسه مشاور وجود ندارد.

## حذف وابستگی EF

سه Property و Navigation مربوط به خدمت از Entity حذف شده‌اند. تنظیمات Notes، FK خدمت و ایندکس‌های خدمت/request حذف شده‌اند. همه Projectionها و DTOهای پاسخ ادمین، لیدهای مشاور و لیدهای جدید پاک‌سازی شده‌اند. مدل اجرایی، Queryهای EF و Snapshot نهایی هیچ‌کدام از سه ستون را ندارند. ایندکس عملیاتی لیدهای فعال مشاور و رابطه ConsultantProfile بدون تغییر حفظ شده‌اند.

فایل‌های تغییرکرده:

- `DentalDashboard.Domain/Models/LeadAssignment.cs`
- `DentalDashboard.Infrastracture/Configuration/LeadAssignmentConfiguration.cs`
- `DentalDashboard/Controllers/ConsultationRequestsController.cs`
- `DentalDashboard.ApplicationService.Contract/Responses/LeadResponse/LeadsAssignmentItemsResponse.cs`
- `DentalDashboard.ApplicationService/Handlers/QueryHandlers/Lead/GetLeadsAssignmentQueryHandler.cs`
- `DentalDashboard.ApplicationService/Handlers/QueryHandlers/Lead/GetNewLeadsQueryHandler.cs`
- `DentalDashboard.Infrastracture/Migrations/20261024171233_AddWebsiteConsultationMetadata.cs`
- `DentalDashboard.Infrastracture/Migrations/20261027080000_UseExistingWebsiteLeadSchema.cs`
- `DentalDashboard.Infrastracture/Migrations/20261027080000_UseExistingWebsiteLeadSchema.Designer.cs`
- `DentalDashboard.Infrastracture/Migrations/DentalContextModelSnapshot.cs`
- `DentalDashboard.Consultation.Tests/ConsultationTests.cs`
- `DentalDashboard.Consultation.Tests/ConsultationUnitTests.cs`
- `.github/workflows/consultation-tests.yml`
- `docs/website-lead-operational-transfer-20261027.md`
- `docs/website-lead-no-extra-columns.md`

Repository، LeadAssignmentService، AdminLeadSheetService، Yektanet، workerها، منطق رزرو و فایل‌های تخصیص تغییر نکرده‌اند.

## روش ایمن Migration

دیتابیس عملیاتی یا جدول `__EFMigrationsHistory` آن در این محیط خوانده/تغییر داده نشده است؛ صرفاً از خطای ارائه‌شده می‌دانیم سه ستون موردنظر در آن موجود نیستند. بنابراین حالت اعمال‌شدن Migration را حدس نمی‌زنیم.

Migration قدیمی `20261024171233_AddWebsiteConsultationMetadata` حذف یا تغییرنام نشده و شناسه آن حفظ می‌شود. از آنجا که کل عملیات Up/Down آن فقط مربوط به همین سه ستون، دو ایندکس و FK بود، این Migration به‌صورت کنترل‌شده بازنشسته شده است: Up و Down آن اکنون بدون عملیات DDL هستند.

- اگر قبلاً اعمال شده باشد، EF آن را دوباره اجرا نمی‌کند؛ ستون‌ها و داده‌های احتمالی موجود دست‌نخورده می‌مانند و مدل جدید آن‌ها را نمی‌خواند.
- اگر اعمال نشده باشد، EF فقط شناسه را در تاریخچه ثبت می‌کند و هیچ ستون جدیدی نمی‌سازد.
- اگر تاریخچه اعمال‌شده وجود داشته ولی ستون‌ها حذف شده باشند، مدل جدید بدون آن‌ها کار می‌کند.
- اگر ستون‌ها وجود داشته ولی تاریخچه Migration ثبت نشده باشد، باز هم ستونی اضافه/حذف نمی‌شود و داده قبلی حفظ می‌شود.
- Down بازنشسته نیز داده قبلی را حذف نمی‌کند. بازگشت به کد قدیمیِ وابسته به ستون‌ها از این طریق تضمین نمی‌شود و نباید برای آن ستون ساختگی تولید کرد.

Designer تاریخیِ Migration قدیمی عمداً حفظ شده تا مدل زمان ایجاد آن در تاریخچه از بین نرود؛ نام این سه ستون فقط در آن سند تاریخی و fixtureهای مربوط به دیتابیس قدیمی باقی می‌ماند، نه مدل اجرایی یا Queryها. تغییر رفتار Up/Down همین Migration محدود به عملیات سه ستون است و با تست هر چهار حالت بالا بررسی می‌شود. سایر Migrationهای تاریخی تغییر نکرده‌اند.

Migration جدید `20261027080000_UseExistingWebsiteLeadSchema` هم Up/Down بدون عملیات دارد و فقط مدل نهایی EF را در زنجیره ثبت می‌کند. Designer جدید و ModelSnapshot با مدل بدون سه ستون هماهنگ‌اند. هیچ DropColumn، DropTable، Reset، backfill یا تغییر داده‌ای در Migrationهای اصلاحی اجرا نمی‌شود.

**برای رفع خطای ستون‌ها نیاز به افزودن این سه ستون نیست؛ اجرای باینری Backend اصلاح‌شده وابستگی را حذف می‌کند.** این کار بازسازی تاریخچه قدیمی و متداخل InitialCreate/CreateDatabase را انجام نمی‌دهد.

## آزمون‌ها

تمام تست‌های SQL از دیتابیس‌های موقت با نام تصادفی استفاده می‌کنند. در Initialize هر تست، مدل نهایی دیتابیس را می‌سازد و شمارش ستون‌های سه‌گانه در sys.columns باید صفر باشد. بنابراین ثبت، خواندن، دریافت مشاور، pickup، تخصیص، مهلت سه‌دقیقه‌ای و رگرشن یکتانت/AdminSheet واقعاً در غیاب این ستون‌ها اجرا می‌شوند.

چهار حالت Migration قبلی (applied/pending × ستون‌ها موجود/ناموجود) با تاریخچه baseline عملیاتی و اجرای واقعی EF MigrateAsync تست می‌شوند. فقط برای شبیه‌سازی دیتابیس‌هایی که قبلاً Migration قدیمی را اعمال کرده‌اند، fixture جدا سه ستون فیزیکی با داده نمونه می‌سازد؛ این DDL بخشی از برنامه یا Migration اصلاحی نیست. تست ثابت می‌کند داده آن‌ها حذف نمی‌شود. حالت معمول بدون ستون، پس از Migration هم باید همچنان صفر ستون اضافی داشته باشد.

تست HTTP فقط نام و شماره ارسال می‌کند؛ تست سازگاری دیگری هم ثابت می‌کند کلید اضافه فرانت قدیمی یا consultantProfileId ارسالی، باعث تخصیص مشاور هنگام ذخیره نمی‌شود. Unit Test قرارداد دو Property، نبود فیلد/Navigation در مدل، SQL بدون نام ستون‌ها، تطابق Snapshot و خالی‌بودن Up/Down هر دو Migration را بررسی می‌کند.

نتایج اولیه محلی: Build کامل Release و ۵ تست Unit موفق. تست نهایی SQL CI در حال اجراست و تا دریافت نتیجه، موفق اعلام نمی‌شود.

هیچ دیتابیس عملیاتی Drop/Reset/تغییر داده نشده و هیچ Deploy انجام نشده است.
