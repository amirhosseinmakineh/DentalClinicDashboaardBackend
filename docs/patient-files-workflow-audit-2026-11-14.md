**گزارش بررسی گردش کار پرونده بیمار — ۱۴ نوامبر ۲۰۲۶**

وضعیت: مسیر فعلی را نمی‌توان بدون باگ یا آماده بهره‌برداری قطعی دانست. این بررسی شامل تشکیل پرونده، فرم کاغذی، تأیید مشاوره، ثبت و اصلاح مالی، مجوز خدمات، ثبت نتیجه و ارتباط با پاداش مشاور است. کد برنامه و دیتابیس در این بررسی تغییر نکردند.

«قطعی از کد» یعنی شرط و مسیر خطا در کد موجود است؛ به معنای مشاهده رخداد در دیتابیس واقعی نیست. «بازتولیدشده» یعنی سناریو با منطق واقعی برنامه و داده مصنوعی اجرا شد. «ریسک هم‌زمانی/عملیاتی» به آزمایش روی دیتابیس آزمایشی نیاز دارد.

**۱. زیاد — مسیر قدیمی ثبت نتیجه، مجوز پرونده را بررسی نمی‌کند. قطعی از کد.**

حذف دکمه از بخش تأیید حضور، API قبلی را غیرفعال نکرده است. درخواست به `POST /api/Reservation/ReviewAttendance` فقط دسترسی منشی و رزرو را بررسی می‌کند و سپس همان هندلر ثبت نتیجه را اجرا می‌کند. هیچ بررسیِ مالی یا مجوز پرونده در این مسیر یا هندلر نیست. بنابراین کلاینت قدیمی یا درخواست مستقیم می‌تواند قبل از تکمیل مالی، نتیجه خدمت و وضعیت واجد شرایط بودن پاداش را ثبت کند.

منبع: [ReservationController.cs](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard/Controllers/ReservationController.cs:248)، [ReviewReservationAttendanceCommandHandler.cs](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard.ApplicationService/Handlers/CommandHandlers/Reservation/ReviewReservationAttendanceCommandHandler.cs:23)

اصلاح پیشنهادی: قاعده مجوز پرونده باید در مسیر مشترک ثبت نتیجه الزام شود و مسیر قدیمی نیز همان قاعده را اجرا کند یا صریحاً بازنشسته شود.

**۲. زیاد — دو ثبت هم‌زمان می‌توانند نتیجه یک رزرو را بازنویسی کنند. ریسک مستند از کد؛ بدون تست هم‌زمان SQL.**

هندلر ابتدا رزرو را می‌خواند، سپس `IsAttendanceScoreApplied` را بررسی می‌کند و بعد بدون قفل خواندن/نسخه رکورد/به‌روزرسانی شرطی ذخیره می‌کند. دو منشی می‌توانند هر دو وضعیت اولیه را بخوانند و یکی «انجام شد» و دیگری «انجام نشد» ثبت کنند. هر دو درخواست ممکن است موفق شوند و آخرین ذخیره نتیجه، نام دکتر و واجد شرایط بودن پاداش را تعیین کند. جلوگیری از کلیک تکراری در یک مرورگر، این حالت را پوشش نمی‌دهد. این یافته به معنای اثبات پرداخت دو پاداش نیست؛ خطر اصلی تناقض و بازنویسی نتیجه است.

همچنین در مسیر جدید، بررسی مجوز پرونده و ثبت نتیجه در یک عملیات اتمیک نیستند؛ ویرایش مالی در فاصله بین این دو مرحله می‌تواند مجوز را باطل کند، ولی درخواست نتیجه همچنان ادامه یابد.

منبع: [هندلر ثبت نتیجه](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard.ApplicationService/Handlers/CommandHandlers/Reservation/ReviewReservationAttendanceCommandHandler.cs:25)، [مسیر جدید](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard/Controllers/PatientFileServiceResultsController.cs:70)

اصلاح پیشنهادی: کنترل هم‌زمانی و بررسی مجوز/ثبت نتیجه باید در عملیات واحد انجام شود؛ آزمایش دو درخواست موازی با نتیجه مخالف ضروری است.

**۳. زیاد — تغییر موبایل، اتصال مالی و اعتبار تأیید هویت را ناسازگار می‌کند. قطعی از کد.**

ویرایش پرونده فقط `PatientFile.PhoneNumber` را تغییر می‌دهد؛ شماره کاربر مالی همگام نمی‌شود و تأیید پروفایل/مجوز خدمات نیز پاک نمی‌شود. درحالی‌که فهرست صورت‌حساب‌ها با شماره موبایل پیدا می‌شود. پس بعد از تغییر موبایل، صورت‌حساب ممکن است از نمایش پرونده ناپدید شود، ولی همان پرونده هنوز مجاز به انجام خدمات نشان داده شود. اگر مراحل تأیید دوباره اجرا شوند، مرحله تأیید پروفایل به‌علت عدم تطابق شماره‌ها متوقف می‌شود.

منبع: [تغییر موبایل](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard.ApplicationService/Secretary/PatientFiles/PatientFileHandlers.cs:764)، [بارگذاری مالی با موبایل](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard.ApplicationService/Secretary/PatientFiles/PatientFileHandlers.cs:263)، [شرط تطابق پروفایل](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard/Controllers/PatientFileCompletionController.cs:180)

اصلاح پیشنهادی: اتصال مالی باید بر شناسه پایدار بیمار/پرونده متکی باشد؛ تغییر اطلاعات هویتی باید همگام‌سازی مشخص و بازبینی تأییدهای وابسته داشته باشد.

**۴. زیاد — بررسی وصول می‌تواند سررسید را یک روز زودتر تشخیص دهد. بازتولیدشده.**

ثبت مالی جدید، نیمه‌شب ایران را به UTC تبدیل می‌کند. SQL مقدار `datetime2` را با `DateTimeKind.Unspecified` برمی‌گرداند. تابع `GetDateInIran` در این حالت تبدیل منطقه زمانی انجام نمی‌دهد. مثلاً سررسید ۱۵ نوامبر، به‌صورت ۱۴ نوامبر ساعت ۲۰:۳۰ ذخیره می‌شود؛ بررسی وضعیت چک/سفته آن را ۱۴ نوامبر حساب می‌کند و ممکن است از همان روز اجازه ثبت نتیجه وصول بدهد. نمایش گزارش سررسید جدید و فرم اصلاح مالی از مسیرهای دیگری استفاده می‌کنند و این خطا را لزوماً نشان نمی‌دهند.

منبع: [IranTimeHelper.cs](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard.Utilities/Time/IranTimeHelper.cs:45)، [بررسی چک](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard.ApplicationService/Secretary/Accountant/PatientFinance/Handlers/PatientFinanceCommandHandlers.cs:562)، [بررسی سفته](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard.ApplicationService/Secretary/Accountant/PatientFinance/Handlers/PatientFinanceCommandHandlers.cs:640)

اصلاح پیشنهادی: قرارداد ذخیره و خواندن تاریخ مالی یکدست شود؛ اصلاح سراسری تابع عمومی بدون بررسی مصرف‌کننده‌های تاریخ محلی رزرو، مناسب نیست.

**۵. متوسط — فرم پس از تأیید مشاوره قفل می‌شود، بدون مسیر بازگشایی. قطعی از کد.**

وقتی `TreatmentConfirmedAtUtc` مقدار دارد، تغییر شرح درمان، چارت یا هر بخش `IntakeJson` با خطای Conflict رد می‌شود. این شامل اصلاح آدرس، سابقه بیماری و تاریخ تولد نیز می‌شود. پیام می‌گوید ابتدا تأییدهای بعدی بازبینی شوند، اما در مسیر فعلی عملیاتی برای پاک‌کردن تأیید طرح درمان وجود ندارد. ویرایش مالی هم این تأیید را پاک نمی‌کند. حتی قبل از ثبت مالی، منشی نمی‌تواند یک طرح درمان تأییدشده را از همین مسیر اصلاح کند.

منبع: [PatientPaperRecordsController.cs](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard/Controllers/PatientPaperRecordsController.cs:111)

اصلاح پیشنهادی: اصلاح اطلاعات عمومی از تغییر طرح درمان تفکیک شود و بازگشایی طرح درمان با مدیریت تأییدهای وابسته مسیر مشخص داشته باشد.

**۶. متوسط — ذخیره ویرایش پرونده دو مرحله مستقل دارد و می‌تواند نیمه‌کاره شود. قطعی از کد.**

فرانت ابتدا هویت/موبایل را ذخیره می‌کند و سپس فرم کاغذی را می‌فرستد. اگر مرحله دوم به‌دلیل قفل طرح درمان، اعتبارسنجی یا قطع شبکه شکست بخورد، مرحله اول در دیتابیس باقی می‌ماند. بنابراین پیام خطا الزاماً به معنی ذخیره نشدن کل تغییرات نیست. تغییر موبایل در همین حالت می‌تواند مشکل مورد ۳ را هم فعال کند.

منبع: [patient-files-page.component.ts](C:/Users/ASUS/source/repos/drsaeedMoghadamFront/src/app/features/secretary/patient-files/patient-files-page.component.ts:450)

اصلاح پیشنهادی: ویرایش واحد باید تراکنش سمت سرور داشته باشد، یا عملیات مستقل با وضعیت ذخیره روشن از هم جدا شوند.

**۷. متوسط — وصول موفق چک، مجوز خدمات را بی‌اعتبار می‌کند. بازتولیدشده.**

اثر انگشت تأیید مالی شامل وضعیت چک‌ها، تراکنش‌ها و وضعیت صورت‌حساب است. تبدیل چک در انتظار وصول به پرداخت‌شده، با اینکه مجموع وجه و اسناد هنوز کل درمان را پوشش می‌دهد، اثر انگشت را تغییر می‌دهد. در سناریوی اجراشده `CanApprove` درست و `IsApproved` نادرست شد. نتیجه عملی: دکمه ثبت نتیجه خدمت تا تأیید دوباره مجوز ناپدید می‌شود.

منبع: [PatientFileWorkflow.cs](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard.Domain/Models/PatientFileWorkflow.cs:51)

اصلاح پیشنهادی: «اصلاح مبلغ/اسناد» از «وصول عادی سند تأییدشده» تفکیک شود. برگشت چک و ایجاد بدهی همچنان باید محدودیت لازم را اعمال کنند.

**۸. متوسط — مجوز به پرونده بیمار وصل است، نه به مراجعه/رزرو مشخص. ریسک عملیاتی مستند.**

مسیر جدید فقط تعلق رزرو به همان بیمار را بررسی می‌کند و رزروهای او را نشان می‌دهد؛ ارتباط ذخیره‌شده‌ای بین این مجوز مالی و یک رزرو معین وجود ندارد. در بیمار دارای چند مراجعه، منشی می‌تواند نتیجه یک رزرو قدیمیِ تعیین‌تکلیف‌نشده را با مجوز فعلی ثبت کند و پاداش به مشاور همان رزرو تعلق بگیرد. انتخاب دستی رزرو خطر اشتباه را کم می‌کند، ولی مانع سیستمی نیست.

منبع: [PatientFileServiceResultsController.cs](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard/Controllers/PatientFileServiceResultsController.cs:51)، [تطابق بیمار](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard/Controllers/PatientFileServiceResultsController.cs:76)

اصلاح پیشنهادی: دامنه مجوز برای مراجعه/دوره درمان تعریف و اتصال آن به نتیجه خدمت ثبت شود.

**۹. متوسط — حذف پرونده مالی‌دار می‌تواند مسیر دسترسی به گردش کار را ببندد. قطعی از کد.**

حذف پرونده فقط `PatientFile.IsDeleted` را تغییر می‌دهد؛ مانعی برای وجود مالی/مجوز/نتیجه خدمت بررسی نمی‌شود. اسناد مالی پاک نمی‌شوند، ولی پرونده از فهرست عادی ناپدید می‌شود. صورت‌حساب به workflow پرونده حذف‌شده متصل باقی می‌ماند و اتصال همان صورت‌حساب به پرونده جدید نیز به‌علت بررسی اتصال قبلی رد می‌شود.

منبع: [حذف پرونده](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard.ApplicationService/Secretary/PatientFiles/PatientFileHandlers.cs:776)، [محدودیت اتصال مالی](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard/Controllers/PatientFileCompletionController.cs:78)

اصلاح پیشنهادی: پرونده دارای گردش مالی حذف معمولی نشود؛ بایگانی/بازیابی و تعیین تکلیف اتصال‌ها باید صریح باشد.

**۱۰. متوسط — شرط اجباری صفحه دوم با خواسته ثبت‌شده کاربر سازگار نیست. قطعی از کد.**

صفحه دوم را می‌توان بدون تأیید مشاوره خالی ذخیره کرد؛ اما برای عبور به مالی، سرور رضایت بیمار و حداقل یک ردیف با شرح و هزینه مثبت را اجباری می‌کند و تمام ردیف‌ها باید معتبر باشند. بنابراین گفته «در صفحه دوم چیزی اجباری نباشد» در عبور به مرحله مالی اجرا نشده است. وجود یک ردیف اضافه و خالی هم تأیید را متوقف می‌کند.

منبع: [PatientPaperRecordsController.cs](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard/Controllers/PatientPaperRecordsController.cs:80)

اصلاح پیشنهادی: معیار تأیید مشاوره با تصمیم کاربر یکدست شود؛ این موضوع از قاعده درستِ پوشش کامل مبلغ در مرحله مالی جداست.

**۱۱. وابسته به استقرار — SQL دستی قدیمی، تمام ستون‌های فعلی workflow را نمی‌سازد. وضعیت دیتابیس واقعی تأیید نشده است.**

فایل `docs/patient-file-workflow.sql` فقط ساختار اولیه را ایجاد می‌کند. ستون‌های تأیید طرح درمان، مالی و پروفایل در مایگریشن‌های بعدی هستند. اجرای خودکار `Database.Migrate` در Program کامنت است. اگر فقط SQL اولیه در محیط مقصد اجرا شده باشد، درخواست‌های پرونده می‌توانند خطای ستون نامعتبر بدهند. مایگریشن‌های ترمیم در مخزن وجود دارند؛ این یافته ادعای نبود ستون در دیتابیس فعلی نیست.

منبع: [SQL اولیه](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/docs/patient-file-workflow.sql:7)، [مایگریشن ستون‌های بعدی](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard.Infrastracture/Migrations/20261113190000_AddSecretaryWorkflowConfirmations.cs:51)، [Program.cs](C:/Users/ASUS/source/repos/DentalClinicDashboaardBackend/DentalDashboard/Program.cs:183)

**نتیجه آزمون‌ها و حدود بررسی**

- ۲۱ تست مرتبط موجود اجرا شد: ۱۹ موفق، ۲ ناموفق. دو تست `PermissionRequiresConsultationAndFinanceAndIsRevokedByFinancialChanges` و `ServiceReleaseRequiresFullCoverageWithoutAnUnpaidDebt` هنوز داده آزمایشی بدون زمان‌های تأیید جدید می‌سازند، ولی انتظار مجوز معتبر دارند؛ با شرط فعلی `CanApprove` سازگار نیستند. این دو شکست به‌تنهایی اثبات خرابی مسیر واقعی نیست، اما نشان می‌دهد مجموعه تست فعلی به‌روز نیست.
- دو سناریوی تشخیصیِ تاریخ SQL و بی‌اعتبار شدن مجوز پس از وصول موفق، با منطق واقعی C# و داده مصنوعی بازتولید شدند. فایل موقت این بررسی پس از اجرا حذف شد؛ نتایج در `DentalDashboard.Consultation.Tests/TestResults/patient-workflow-probes.trx` قرار دارند.
- هر دو اسکریپت `validate-patient-file-completion.mjs` و `validate-patient-file-service-results.mjs` موفق بودند؛ این اسکریپت‌ها منطق واقعی کامپوننت با API مصنوعی را آزمایش می‌کنند و جای تست مرورگر/دیتابیس را نمی‌گیرند.
- حساب مالی واقعی، نتیجه خدمت واقعی و پاداش واقعی ایجاد یا اصلاح نشد. اجرای کامل با دو منشی هم‌زمان، مرورگر و SQL آزمایشی انجام نشده است.
- برای این گزارش فقط سند گزارش اضافه شد؛ تغییرات قبلیِ ثبت‌نشده مخزن حفظ شدند.

**ترتیب پیشنهادی رسیدگی**

ابتدا مسیر قدیمیِ دورزننده مجوز، ثبت هم‌زمان نتیجه، ارتباط هویت و مالی و تبدیل تاریخ وصول اصلاح شوند. سپس ویرایش اتمیک/بازگشایی فرم، رفتار مجوز هنگام وصول، حذف پرونده مالی‌دار و اتصال به مراجعه رسیدگی شوند. پیش از اعلام آمادگی بهره‌برداری، تست‌ها به‌روز شوند و مسیر نقدی، اقساط مختلط، اصلاح اشتباه، وصول، برگشت چک، تغییر هویت و دو منشی هم‌زمان روی محیط آزمایشی اجرا شوند.
