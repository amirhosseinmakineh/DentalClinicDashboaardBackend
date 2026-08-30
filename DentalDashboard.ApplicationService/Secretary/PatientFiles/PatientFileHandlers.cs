using ClosedXML.Excel;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
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

public sealed class GetPatientFilesQueryHandler(IPatientFileRepository repository)
    : IQueryHandler<GetPatientFilesQuery, Result<PaginatedResult<PatientFileDto>>>
{
    public async Task<Result<PaginatedResult<PatientFileDto>>> HandleAsync(GetPatientFilesQuery request, CancellationToken ct = default)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100)
            return Result<PaginatedResult<PatientFileDto>>.Failure("مقادیر صفحه‌بندی معتبر نیستند");
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
                x.LastName, x.PhoneNumber, x.SourceType, x.CreatedAt)).ToListAsync(ct);
        return Result<PaginatedResult<PatientFileDto>>.Success(new()
        { Items = items, PageNumber = request.Page, PageSize = request.PageSize, TotalCount = count });
    }
}

public sealed class GetPatientFileByIdQueryHandler(IPatientFileRepository repository)
    : IQueryHandler<GetPatientFileByIdQuery, Result<PatientFileDto>>
{
    public async Task<Result<PatientFileDto>> HandleAsync(GetPatientFileByIdQuery request, CancellationToken ct = default)
    {
        var item = await repository.PatientFiles.AsNoTracking().Where(x => x.Id == request.Id)
            .Select(x => new PatientFileDto(x.Id, x.PatientReferenceId, x.FileNumber, x.FirstName,
                x.LastName, x.PhoneNumber, x.SourceType, x.CreatedAt)).SingleOrDefaultAsync(ct);
        return item is null ? Result<PatientFileDto>.Failure("پرونده بیمار یافت نشد") : Result<PatientFileDto>.Success(item);
    }
}

public sealed class SearchPatientsEligibleForFileQueryHandler(IPatientFileRepository repository)
    : IQueryHandler<SearchPatientsEligibleForFileQuery, Result<PaginatedResult<EligiblePatientDto>>>
{
    public async Task<Result<PaginatedResult<EligiblePatientDto>>> HandleAsync(SearchPatientsEligibleForFileQuery request, CancellationToken ct = default)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100)
            return Result<PaginatedResult<EligiblePatientDto>>.Failure("مقادیر صفحه‌بندی معتبر نیستند");
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
        return Result<PaginatedResult<EligiblePatientDto>>.Success(new()
        { Items = items, PageNumber = request.Page, PageSize = request.PageSize, TotalCount = count });
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
        var maxRows = configuration.GetValue("PatientFiles:ImportMaxRows", 5000);
        if (request.Length <= 0) return Failure("فایل خالی است");
        if (request.Length > maxBytes) return Failure("حجم فایل بیش از حد مجاز است");
        if (!string.Equals(Path.GetExtension(request.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase)) return Failure("فقط فایل xlsx مجاز است");
        var rows = new List<Row>();
        try
        {
            using var workbook = new XLWorkbook(request.Content);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet is null) return Failure("فایل فاقد Worksheet است");
            var headers = Enumerable.Range(1, 4).Select(i => sheet.Cell(1, i).GetString().Trim()).ToArray();
            if (!headers.SequenceEqual(new[] { "FirstName", "LastName", "FileNumber", "PhoneNumber" })) return Failure("Headerهای فایل معتبر نیستند");
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow <= 1) return Failure("فایل فاقد ردیف داده است");
            if (lastRow - 1 > maxRows) return Failure($"حداکثر تعداد ردیف مجاز {maxRows} است");
            for (var i = 2; i <= lastRow; i++)
            {
                var first = sheet.Cell(i, 1).GetString().Trim(); var last = sheet.Cell(i, 2).GetString().Trim();
                var fileText = sheet.Cell(i, 3).GetFormattedString().Trim(); var phone = sheet.Cell(i, 4).GetFormattedString().Trim();
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
    }
}
