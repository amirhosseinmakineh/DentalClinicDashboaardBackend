using ClosedXML.Excel;
using System.IO.Compression;
using DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DentalDashboard.ApplicationService.Secretary.PatientFiles;

internal static class PatientFileNames
{
    public static (string FirstName, string LastName) Split(string fullName)
    {
        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return (parts.ElementAtOrDefault(0) ?? "-", parts.ElementAtOrDefault(1) ?? "-");
    }
}

public sealed class GetPatientFilesQueryHandler(IPatientFileRepository repository, IPatientFinanceRepository financeRepository)
    : IQueryHandler<GetPatientFilesQuery, Result<PatientFilePageResponse>>
{
    public async Task<Result<PatientFilePageResponse>> HandleAsync(GetPatientFilesQuery request, CancellationToken ct = default)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100)
            return Result<PatientFilePageResponse>.Failure("مقادیر صفحه‌بندی معتبر نیستند");
        var query = repository.PatientFiles.AsNoTracking();
        if (request.FileNumber.HasValue) query = query.Where(x => x.FileNumber == request.FileNumber);
        if (request.SourceType.HasValue) query = query.Where(x => x.SourceType == request.SourceType);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            var numeric = long.TryParse(search, out var number);
            query = query.Where(x => x.FirstName.Contains(search) || x.LastName.Contains(search) ||
                (x.FirstName + " " + x.LastName).Contains(search) || x.PhoneNumber.Contains(search) ||
                (numeric && x.FileNumber == number));
        }
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.FileNumber).ThenByDescending(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new PatientFileDto(x.Id, x.PatientReferenceId, x.FileNumber, x.FirstName,
                x.LastName, x.PhoneNumber, x.SourceType, x.CreatedAt, null)).ToListAsync(ct);
        items = await PatientFileFinanceLoader.AttachAsync(items, financeRepository, ct);
        return Result<PatientFilePageResponse>.Success(
            new(items, request.Page, request.PageSize, count));
    }
}

public sealed class GetPatientFileByIdQueryHandler(IPatientFileRepository repository, IPatientFinanceRepository financeRepository)
    : IQueryHandler<GetPatientFileByIdQuery, Result<PatientFileDto>>
{
    public async Task<Result<PatientFileDto>> HandleAsync(GetPatientFileByIdQuery request, CancellationToken ct = default)
    {
        var item = await repository.PatientFiles.AsNoTracking().Where(x => x.Id == request.Id)
            .Select(x => new PatientFileDto(x.Id, x.PatientReferenceId, x.FileNumber, x.FirstName,
                x.LastName, x.PhoneNumber, x.SourceType, x.CreatedAt, null)).SingleOrDefaultAsync(ct);
        if (item is not null)
            item = (await PatientFileFinanceLoader.AttachAsync([item], financeRepository, ct))[0];
        return item is null ? Result<PatientFileDto>.Failure("پرونده بیمار یافت نشد") : Result<PatientFileDto>.Success(item);
    }
}

internal static class PatientFileFinanceLoader
{
    public static async Task<List<PatientFileDto>> AttachAsync(
        IReadOnlyList<PatientFileDto> files, IPatientFinanceRepository repository,
        CancellationToken ct)
    {
        if (files.Count == 0) return [];
        var phones = files.Select(x => x.PhoneNumber).Distinct().ToList();
        var financialPatients = await repository.Patients.AsNoTracking()
            .Where(x => !x.IsDeleted && x.PatientProfile != null &&
                !x.PatientProfile.IsDeleted && phones.Contains(x.PhoneNumber))
            .Select(x => new { x.Id, x.PhoneNumber })
            .ToListAsync(ct);
        var patientIdByPhone = financialPatients
            .GroupBy(x => x.PhoneNumber)
            .ToDictionary(x => x.Key, x => (Guid?)x.First().Id);
        var cases = await repository.Cases.AsNoTracking()
            .Where(x => phones.Contains(x.Patient.PhoneNumber))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                Phone = x.Patient.PhoneNumber,
                x.PatientId,
                Case = new PatientFileFinancialCaseDto(
                    x.Id, (int)x.Service, x.Service.ToString(), x.TotalAmount,
                    x.Transactions.Sum(t => (decimal?)t.Amount) ?? 0,
                    x.TotalAmount - (x.Transactions.Sum(t => (decimal?)t.Amount) ?? 0),
                    x.Debts.Where(d => d.Status == PatientDebtStatus.Unpaid).Sum(d => (decimal?)d.Amount) ?? 0,
                    x.AgreementType, x.Status, x.CreatedAt,
                    x.Cheques.OrderBy(c => c.DueDate).Select(c => new PatientFileChequeDto(
                        c.Id, c.Amount, c.SayadNumber, c.OwnerName, c.DueDate, c.Status)).ToList(),
                    x.PromissoryNotes.OrderBy(n => n.DueDate).Select(n => new PatientFilePromissoryNoteDto(
                        n.Id, n.SerialNumber, n.Amount, n.DueDate, n.Status)).ToList(),
                    x.Debts.OrderBy(d => d.DueDate).Select(d => new PatientFileDebtDto(
                        d.Id, d.Amount, d.SourceType, d.SourceId, d.DueDate, d.Status)).ToList(),
                    x.Transactions.OrderByDescending(t => t.CreatedAt).Select(t => new PatientFileTransactionDto(
                        t.Id, t.Amount, t.Type, t.SourceType, t.SourceId, t.CreatedAt)).ToList())
            }).ToListAsync(ct);

        var byPhone = cases.GroupBy(x => x.Phone).ToDictionary(x => x.Key, x => x.ToList());
        return files.Select(file =>
        {
            patientIdByPhone.TryGetValue(file.PhoneNumber, out var financialPatientId);
            if (!byPhone.TryGetValue(file.PhoneNumber, out var patientCases))
                return file with { FinancialPatientId = financialPatientId };
            var activeCases = patientCases.Where(x => x.Case.Status != PatientFinancialCaseStatus.Cancelled).ToList();
            var total = activeCases.Sum(x => x.Case.TotalAmount);
            var paid = activeCases.Sum(x => x.Case.TotalPaidAmount);
            var finance = new PatientFileFinanceDto(
                patientCases[0].PatientId, total, paid, total - paid,
                activeCases.Sum(x => x.Case.TotalDebtAmount),
                patientCases.Count(x => x.Case.Status == PatientFinancialCaseStatus.Active),
                activeCases.Sum(x => x.Case.Cheques.Count(c => c.Status == PatientChequeStatus.Unpaid)),
                activeCases.Sum(x => x.Case.PromissoryNotes.Count(n => n.Status == PatientPromissoryNoteStatus.Unpaid)),
                patientCases.Select(x => x.Case).ToList());
            return file with
            {
                FinancialPatientId = financialPatientId ?? patientCases[0].PatientId,
                Finance = finance
            };
        }).ToList();
    }
}

public sealed class SearchPatientsEligibleForFileQueryHandler(IPatientFileRepository repository)
    : IQueryHandler<SearchPatientsEligibleForFileQuery, Result<EligiblePatientPageResponse>>
{
    public async Task<Result<EligiblePatientPageResponse>> HandleAsync(SearchPatientsEligibleForFileQuery request, CancellationToken ct = default)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100)
            return Result<EligiblePatientPageResponse>.Failure("مقادیر صفحه‌بندی معتبر نیستند");
        var query = repository.Patients.AsNoTracking().Where(p => !p.IsDeleted &&
            repository.Reservations.Any(r => !r.IsDeleted && !r.IsCanceled && r.LeadAssignmentId == p.Id) &&
            !repository.PatientFiles.Any(f => f.PatientReferenceId == p.Id && f.SourceType == PatientFileSourceType.System));
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.UserName.Contains(search) || x.PhoneNumber.Contains(search));
        }
        var count = await query.CountAsync(ct);
        var raw = await query.OrderBy(x => x.UserName).ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new { x.Id, x.UserName, x.PhoneNumber }).ToListAsync(ct);
        var items = raw.Select(x => { var n = PatientFileNames.Split(x.UserName); return new EligiblePatientDto(x.Id, n.FirstName, n.LastName, x.PhoneNumber); }).ToList();
        return Result<EligiblePatientPageResponse>.Success(
            new(items, request.Page, request.PageSize, count));
    }
}

public sealed class CreatePatientFileCommandHandler(IPatientFileRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreatePatientFileCommand, CreatePatientFileResponse>
{
    public async Task<Result<CreatePatientFileResponse>> HandleAsync(CreatePatientFileCommand request, CancellationToken ct = default)
    {
        if (request.PatientId <= 0) return Result<CreatePatientFileResponse>.Failure("شناسه بیمار معتبر نیست");
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var patient = await repository.Patients.SingleOrDefaultAsync(x => x.Id == request.PatientId && !x.IsDeleted, ct);
            if (patient is null) return await Rollback("بیمار یافت نشد");
            if (!await repository.Reservations.AnyAsync(x => x.LeadAssignmentId == patient.Id && !x.IsDeleted && !x.IsCanceled, ct))
                return await Rollback("بیمار رزرو معتبر ندارد");
            if (await repository.PatientFiles.AnyAsync(x => x.PatientReferenceId == patient.Id && x.SourceType == PatientFileSourceType.System, ct))
                return await Rollback("برای این بیمار قبلاً پرونده ایجاد شده است");
            var fileNumber = await repository.GetNextFileNumberWithLockAsync(ct);
            var names = PatientFileNames.Split(patient.UserName);
            var entity = new PatientFile { PatientReferenceId = patient.Id, FileNumber = fileNumber,
                FirstName = names.FirstName, LastName = names.LastName, PhoneNumber = patient.PhoneNumber.Trim(), SourceType = PatientFileSourceType.System };
            await repository.AddAsync(entity, ct);
            await unitOfWork.CommitAsync(ct);
            return Result<CreatePatientFileResponse>.Success(new(entity.Id, entity.FileNumber));
            async Task<Result<CreatePatientFileResponse>> Rollback(string message) { await unitOfWork.RollbackAsync(ct); return Result<CreatePatientFileResponse>.Failure(message); }
        }
        catch { await unitOfWork.RollbackAsync(ct); throw; }
    }
}

public sealed class UpdatePatientFileCommandHandler(IPatientFileRepository repository, IUnitOfWork unitOfWork) : ICommandHandler<UpdatePatientFileCommand>
{
    public async Task<Result> HandleAsync(UpdatePatientFileCommand request, CancellationToken ct = default)
    {
        var first = request.FirstName?.Trim() ?? ""; var last = request.LastName?.Trim() ?? ""; var phone = request.PhoneNumber?.Trim() ?? "";
        if (request.Id <= 0 || first.Length is 0 or > 100 || last.Length is 0 or > 100 || phone.Length is 0 or > 20)
            return Result.Failure("اطلاعات پرونده معتبر نیست");
        var entity = await repository.PatientFiles.SingleOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null) return Result.Failure("پرونده بیمار یافت نشد");
        entity.FirstName = first; entity.LastName = last; entity.PhoneNumber = phone; entity.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success("پرونده بیمار ویرایش شد");
    }
}

public sealed class DeletePatientFileCommandHandler(IPatientFileRepository repository, IUnitOfWork unitOfWork) : ICommandHandler<DeletePatientFileCommand>
{
    public async Task<Result> HandleAsync(DeletePatientFileCommand request, CancellationToken ct = default)
    {
        var entity = await repository.PatientFiles.SingleOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null) return Result.Failure("پرونده بیمار یافت نشد");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success("پرونده بیمار حذف شد");
    }
}

public sealed class ImportPatientFilesCommandHandler(IPatientFileRepository repository, IUnitOfWork unitOfWork, IConfiguration configuration)
    : ICommandHandler<ImportPatientFilesCommand, ImportPatientFilesResponse>
{
    private sealed record Row(int Number, string FirstName, string LastName, long FileNumber, string PhoneNumber);
    public async Task<Result<ImportPatientFilesResponse>> HandleAsync(ImportPatientFilesCommand request, CancellationToken ct = default)
    {
        var errors = new List<ImportPatientFileError>();
        var maxBytes = configuration.GetValue("PatientFiles:ImportMaxBytes", 10 * 1024 * 1024L);
        var maxExpandedBytes = configuration.GetValue("PatientFiles:ImportMaxExpandedBytes", 50 * 1024 * 1024L);
        var maxRows = configuration.GetValue("PatientFiles:ImportMaxRows", 5000);
        if (request.Length <= 0) return Failure("فایل خالی است");
        if (request.Length > maxBytes) return Failure("حجم فایل بیش از حد مجاز است");
        if (!string.Equals(Path.GetExtension(request.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase)) return Failure("فقط فایل xlsx مجاز است");
        var rows = new List<Row>();
        try
        {
            // Parse only a bounded in-memory copy. Besides preventing a changed stream
            // from being read twice, this lets us validate the actual OOXML signature.
            await using var safeContent = new MemoryStream();
            await request.Content.CopyToAsync(safeContent, ct);
            if (safeContent.Length == 0 || safeContent.Length > maxBytes)
                return Failure("اندازه واقعی فایل معتبر نیست");
            safeContent.Position = 0;
            var signature = new byte[4];
            if (safeContent.Read(signature) != signature.Length ||
                !signature.SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }))
                return Failure("محتوای فایل یک Excel معتبر از نوع xlsx نیست");
            safeContent.Position = 0;
            if (!IsSafeOpenXmlPackage(safeContent, maxExpandedBytes))
                return Failure("ساختار داخلی فایل Excel ناامن یا بیش از حد مجاز است");
            safeContent.Position = 0;

            using var workbook = new XLWorkbook(safeContent);
            if (workbook.Worksheets.Count != 1)
                return Failure("فایل باید دقیقاً شامل یک Worksheet باشد");
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet is null) return Failure("فایل فاقد Worksheet است");
            if (sheet.CellsUsed().Any(cell => cell.HasFormula))
                return Failure("برای حفظ امنیت، سلول دارای Formula مجاز نیست");
            var headers = Enumerable.Range(1, 4).Select(i => sheet.Cell(1, i).GetString().Trim()).ToArray();
            if (!headers.SequenceEqual(new[] { "FirstName", "LastName", "FileNumber", "PhoneNumber" })) return Failure("Headerهای فایل معتبر نیستند");
            if ((sheet.LastColumnUsed()?.ColumnNumber() ?? 0) != 4)
                return Failure("فایل باید دقیقاً شامل چهار ستون تعریف‌شده باشد");
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow <= 1) return Failure("فایل فاقد ردیف داده است");
            if (lastRow - 1 > maxRows) return Failure($"حداکثر تعداد ردیف مجاز {maxRows} است");
            for (var i = 2; i <= lastRow; i++)
            {
                var first = sheet.Cell(i, 1).GetString().Trim(); var last = sheet.Cell(i, 2).GetString().Trim();
                var fileText = sheet.Cell(i, 3).GetFormattedString().Trim(); var phone = sheet.Cell(i, 4).GetFormattedString().Trim();
                if (ContainsUnsafeControlCharacter(first) || ContainsUnsafeControlCharacter(last) || ContainsUnsafeControlCharacter(phone))
                    errors.Add(new(i, "Row", "محتوای ردیف شامل کاراکتر کنترلی غیرمجاز است."));
                if (string.IsNullOrWhiteSpace(first) || first.Length > 100) errors.Add(new(i, "FirstName", "نام الزامی و حداکثر ۱۰۰ کاراکتر است."));
                if (string.IsNullOrWhiteSpace(last) || last.Length > 100) errors.Add(new(i, "LastName", "نام خانوادگی الزامی و حداکثر ۱۰۰ کاراکتر است."));
                if (!long.TryParse(fileText, out var number) || number <= 0) errors.Add(new(i, "FileNumber", "شماره پرونده باید عددی بزرگ‌تر از صفر باشد."));
                if (string.IsNullOrWhiteSpace(phone) || phone.Length > 20) errors.Add(new(i, "PhoneNumber", "شماره تماس الزامی و حداکثر ۲۰ کاراکتر است."));
                if (!errors.Any(e => e.Row == i)) rows.Add(new(i, first, last, number, phone));
            }
        }
        catch (Exception) { return Failure("ساختار فایل Excel معتبر نیست"); }
        foreach (var duplicate in rows.GroupBy(x => x.FileNumber).Where(x => x.Count() > 1))
            foreach (var row in duplicate) errors.Add(new(row.Number, "FileNumber", $"شماره پرونده {row.FileNumber} در فایل تکراری است."));
        var numbers = rows.Select(x => x.FileNumber).Distinct().ToList();
        var existing = await repository.PatientFiles.IgnoreQueryFilters().Where(x => numbers.Contains(x.FileNumber)).Select(x => x.FileNumber).ToListAsync(ct);
        foreach (var row in rows.Where(x => existing.Contains(x.FileNumber))) errors.Add(new(row.Number, "FileNumber", $"شماره پرونده {row.FileNumber} قبلاً در سیستم وجود دارد."));
        if (errors.Count != 0) return Result<ImportPatientFilesResponse>.Success(new(false, 0, errors));
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await repository.AddRangeAsync(rows.Select(x => new PatientFile { FirstName = x.FirstName, LastName = x.LastName,
                FileNumber = x.FileNumber, PhoneNumber = x.PhoneNumber, SourceType = PatientFileSourceType.Legacy }), ct);
            await unitOfWork.CommitAsync(ct);
            return Result<ImportPatientFilesResponse>.Success(new(true, rows.Count, []));
        }
        catch { await unitOfWork.RollbackAsync(ct); throw; }
        Result<ImportPatientFilesResponse> Failure(string message) => Result<ImportPatientFilesResponse>.Success(new(false, 0, [new(0, "File", message)]));
        static bool ContainsUnsafeControlCharacter(string value) =>
            value.Any(character => char.IsControl(character) && character is not '\t');
        static bool IsSafeOpenXmlPackage(Stream content, long maxExpandedLength)
        {
            using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count is 0 or > 1000)
                return false;
            long expandedLength = 0;
            var hasContentTypes = false;
            var hasWorkbook = false;
            foreach (var entry in archive.Entries)
            {
                var path = entry.FullName.Replace('\\', '/');
                if (path.StartsWith('/') || path.Split('/').Contains(".."))
                    return false;
                expandedLength = checked(expandedLength + entry.Length);
                if (expandedLength > maxExpandedLength ||
                    entry.CompressedLength > 0 && entry.Length / entry.CompressedLength > 1000)
                    return false;
                hasContentTypes |= path.Equals("[Content_Types].xml", StringComparison.OrdinalIgnoreCase);
                hasWorkbook |= path.Equals("xl/workbook.xml", StringComparison.OrdinalIgnoreCase);
            }
            return hasContentTypes && hasWorkbook;
        }
    }
}
