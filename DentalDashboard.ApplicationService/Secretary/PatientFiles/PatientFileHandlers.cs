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
        var nameParts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return (nameParts.ElementAtOrDefault(0) ?? "-", nameParts.ElementAtOrDefault(1) ?? "-");
    }
}

public sealed class GetPatientFilesQueryHandler(IPatientFileRepository patientFileRepository, IPatientFinanceRepository patientFinanceRepository) : IQueryHandler<GetPatientFilesQuery, Result<PatientFilePageResponse>>
{
    public async Task<Result<PatientFilePageResponse>> HandleAsync(GetPatientFilesQuery request, CancellationToken cancellationToken = default)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100)
            return Result<PatientFilePageResponse>.Failure("مقادیر صفحه‌بندی معتبر نیستند");

        var patientFilesQuery = patientFileRepository.PatientFiles.AsNoTracking();

        if (request.FileNumber.HasValue)
            patientFilesQuery = patientFilesQuery.Where(patientFile => patientFile.FileNumber == request.FileNumber);

        if (request.SourceType.HasValue)
            patientFilesQuery = patientFilesQuery.Where(patientFile => patientFile.SourceType == request.SourceType);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();
            var isNumericSearch = long.TryParse(searchTerm, out var fileNumber);

            patientFilesQuery = patientFilesQuery.Where(patientFile =>
                patientFile.FirstName.Contains(searchTerm) ||
                patientFile.LastName.Contains(searchTerm) ||
                (patientFile.FirstName + " " + patientFile.LastName).Contains(searchTerm) ||
                patientFile.PhoneNumber.Contains(searchTerm) ||
                (isNumericSearch && patientFile.FileNumber == fileNumber));
        }

        var totalCount = await patientFilesQuery.CountAsync(cancellationToken);

        var patientFiles = await patientFilesQuery
            .OrderByDescending(patientFile => patientFile.FileNumber)
            .ThenByDescending(patientFile => patientFile.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(patientFile => new PatientFileDto(
                patientFile.Id,
                patientFile.PatientReferenceId,
                patientFile.FileNumber,
                patientFile.FirstName,
                patientFile.LastName,
                patientFile.PhoneNumber,
                patientFile.Description,
                patientFile.SourceType,
                patientFile.CreatedAt,
                null))
            .ToListAsync(cancellationToken);

        patientFiles = await PatientFileFinanceLoader.AttachAsync(
            patientFiles,
            patientFileRepository,
            patientFinanceRepository,
            cancellationToken);

        return Result<PatientFilePageResponse>.Success(
            new(patientFiles, request.Page, request.PageSize, totalCount));
    }
}

public sealed class GetPatientFileByIdQueryHandler(IPatientFileRepository patientFileRepository, IPatientFinanceRepository patientFinanceRepository) : IQueryHandler<GetPatientFileByIdQuery, Result<PatientFileDto>>
{
    public async Task<Result<PatientFileDto>> HandleAsync(GetPatientFileByIdQuery request, CancellationToken cancellationToken = default)
    {
        var patientFile = await patientFileRepository.PatientFiles
            .AsNoTracking()
            .Where(patientFile => patientFile.Id == request.Id)
            .Select(patientFile => new PatientFileDto(
                patientFile.Id,
                patientFile.PatientReferenceId,
                patientFile.FileNumber,
                patientFile.FirstName,
                patientFile.LastName,
                patientFile.PhoneNumber,
                patientFile.Description,
                patientFile.SourceType,
                patientFile.CreatedAt,
                null))
            .SingleOrDefaultAsync(cancellationToken);

        if (patientFile is not null)
            patientFile = (await PatientFileFinanceLoader.AttachAsync(
                [patientFile],
                patientFileRepository,
                patientFinanceRepository,
                cancellationToken))[0];

        return patientFile is null
            ? Result<PatientFileDto>.Failure("پرونده بیمار یافت نشد")
            : Result<PatientFileDto>.Success(patientFile);
    }
}

internal static class PatientFileFinanceLoader
{
    public static async Task<List<PatientFileDto>> AttachAsync(
        IReadOnlyList<PatientFileDto> patientFiles,
        IPatientFileRepository patientFileRepository,
        IPatientFinanceRepository patientFinanceRepository,
        CancellationToken cancellationToken)
    {
        if (patientFiles.Count == 0)
            return [];

        var phoneNumbers = patientFiles
            .Select(patientFile => patientFile.PhoneNumber)
            .Distinct()
            .ToList();
        var patientReferenceIds = patientFiles
            .Where(patientFile => patientFile.PatientId.HasValue)
            .Select(patientFile => patientFile.PatientId!.Value)
            .Distinct()
            .ToList();

        var reservationPatientLinks = await patientFileRepository.Reservations
            .AsNoTracking()
            .Where(reservation =>
                !reservation.IsDeleted &&
                reservation.PatientUserId.HasValue &&
                patientReferenceIds.Contains(reservation.LeadAssignmentId))
            .OrderByDescending(reservation => reservation.ReservationAt)
            .ThenByDescending(reservation => reservation.Id)
            .Select(reservation => new
            {
                reservation.LeadAssignmentId,
                PatientUserId = reservation.PatientUserId!.Value
            })
            .ToListAsync(cancellationToken);

        var patientUserIdByReferenceId = reservationPatientLinks
            .GroupBy(link => link.LeadAssignmentId)
            .ToDictionary(group => group.Key, group => group.First().PatientUserId);
        var linkedPatientUserIds = patientUserIdByReferenceId.Values
            .Distinct()
            .ToList();

        var financialPatients = await patientFinanceRepository.Patients
            .AsNoTracking()
            .Where(patient =>
                !patient.IsDeleted &&
                patient.PatientProfile != null &&
                !patient.PatientProfile.IsDeleted &&
                (linkedPatientUserIds.Contains(patient.Id) ||
                 phoneNumbers.Contains(patient.PhoneNumber)))
            .Select(patient => new { patient.Id, patient.PhoneNumber })
            .ToListAsync(cancellationToken);

        var financialPatientIdByPhoneNumber = financialPatients
            .GroupBy(patient => patient.PhoneNumber)
            .ToDictionary(group => group.Key, group => (Guid?)group.First().Id);
        var validFinancialPatientIds = financialPatients
            .Select(patient => patient.Id)
            .ToHashSet();

        var financialCases = await patientFinanceRepository.Cases
            .AsNoTracking()
            .Where(financialCase => phoneNumbers.Contains(financialCase.Patient.PhoneNumber))
            .OrderByDescending(financialCase => financialCase.CreatedAt)
            .Select(financialCase => new
            {
                PhoneNumber = financialCase.Patient.PhoneNumber,
                financialCase.PatientId,
                Case = new PatientFileFinancialCaseDto(
                    financialCase.Id,
                    (int)financialCase.Service,
                    financialCase.Service.ToString(),
                    financialCase.TotalAmount,
                    financialCase.Transactions.Sum(transaction => (decimal?)transaction.Amount) ?? 0,
                    financialCase.TotalAmount - (financialCase.Transactions.Sum(transaction => (decimal?)transaction.Amount) ?? 0),
                    financialCase.Debts
                        .Where(debt => debt.Status == PatientDebtStatus.Unpaid)
                        .Sum(debt => (decimal?)debt.Amount) ?? 0,
                    financialCase.AgreementType,
                    financialCase.Status,
                    financialCase.CreatedAt,
                    financialCase.Cheques
                        .OrderBy(cheque => cheque.DueDate)
                        .Select(cheque => new PatientFileChequeDto(
                            cheque.Id,
                            cheque.Amount,
                            cheque.SayadNumber,
                            cheque.OwnerName,
                            cheque.DueDate,
                            cheque.Status))
                        .ToList(),
                    financialCase.PromissoryNotes
                        .OrderBy(promissoryNote => promissoryNote.DueDate)
                        .Select(promissoryNote => new PatientFilePromissoryNoteDto(
                            promissoryNote.Id,
                            promissoryNote.SerialNumber,
                            promissoryNote.Amount,
                            promissoryNote.DueDate,
                            promissoryNote.Status))
                        .ToList(),
                    financialCase.Debts
                        .OrderBy(debt => debt.DueDate)
                        .Select(debt => new PatientFileDebtDto(
                            debt.Id,
                            debt.Amount,
                            debt.SourceType,
                            debt.SourceId,
                            debt.DueDate,
                            debt.Status))
                        .ToList(),
                    financialCase.Transactions
                        .OrderByDescending(transaction => transaction.CreatedAt)
                        .Select(transaction => new PatientFileTransactionDto(
                            transaction.Id,
                            transaction.Amount,
                            transaction.Type,
                            transaction.SourceType,
                            transaction.SourceId,
                            transaction.CreatedAt))
                        .ToList())
            })
            .ToListAsync(cancellationToken);

        var financialCasesByPhoneNumber = financialCases
            .GroupBy(financialCase => financialCase.PhoneNumber)
            .ToDictionary(group => group.Key, group => group.ToList());

        return patientFiles.Select(patientFile =>
        {
            Guid? financialPatientId = null;
            if (patientFile.PatientId.HasValue &&
                patientUserIdByReferenceId.TryGetValue(
                    patientFile.PatientId.Value,
                    out var linkedPatientUserId) &&
                validFinancialPatientIds.Contains(linkedPatientUserId))
            {
                financialPatientId = linkedPatientUserId;
            }
            else
            {
                financialPatientIdByPhoneNumber.TryGetValue(
                    patientFile.PhoneNumber,
                    out financialPatientId);
            }

            if (!financialCasesByPhoneNumber.TryGetValue(patientFile.PhoneNumber, out var patientFinancialCases))
                return patientFile with { FinancialPatientId = financialPatientId };

            var activeFinancialCases = patientFinancialCases
                .Where(financialCase => financialCase.Case.Status != PatientFinancialCaseStatus.Cancelled)
                .ToList();

            var totalTreatmentAmount = activeFinancialCases.Sum(financialCase => financialCase.Case.TotalAmount);
            var totalPaidAmount = activeFinancialCases.Sum(financialCase => financialCase.Case.TotalPaidAmount);

            var finance = new PatientFileFinanceDto(
                patientFinancialCases[0].PatientId,
                totalTreatmentAmount,
                totalPaidAmount,
                totalTreatmentAmount - totalPaidAmount,
                activeFinancialCases.Sum(financialCase => financialCase.Case.TotalDebtAmount),
                patientFinancialCases.Count(financialCase => financialCase.Case.Status == PatientFinancialCaseStatus.Active),
                activeFinancialCases.Sum(financialCase => financialCase.Case.Cheques.Count(cheque => cheque.Status == PatientChequeStatus.Unpaid)),
                activeFinancialCases.Sum(financialCase => financialCase.Case.PromissoryNotes.Count(note => note.Status == PatientPromissoryNoteStatus.Unpaid)),
                patientFinancialCases.Select(financialCase => financialCase.Case).ToList());

            return patientFile with
            {
                FinancialPatientId = financialPatientId ?? patientFinancialCases[0].PatientId,
                Finance = finance
            };
        }).ToList();
    }
}

public sealed class SearchPatientsEligibleForFileQueryHandler(IPatientFileRepository patientFileRepository) : IQueryHandler<SearchPatientsEligibleForFileQuery, Result<EligiblePatientPageResponse>>
{
    public async Task<Result<EligiblePatientPageResponse>> HandleAsync(SearchPatientsEligibleForFileQuery request, CancellationToken cancellationToken = default)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100)
            return Result<EligiblePatientPageResponse>.Failure("مقادیر صفحه‌بندی معتبر نیستند");

        var eligiblePatientsQuery = patientFileRepository.Patients
            .AsNoTracking()
            .Where(patient =>
                !patient.IsDeleted &&
                patientFileRepository.Reservations.Any(reservation =>
                    !reservation.IsDeleted &&
                    !reservation.IsCanceled &&
                    reservation.LeadAssignmentId == patient.Id) &&
                !patientFileRepository.PatientFiles.Any(patientFile =>
                    patientFile.PatientReferenceId == patient.Id &&
                    patientFile.SourceType == PatientFileSourceType.System));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();
            eligiblePatientsQuery = eligiblePatientsQuery.Where(patient =>
                patient.UserName.Contains(searchTerm) ||
                patient.PhoneNumber.Contains(searchTerm));
        }

        var totalCount = await eligiblePatientsQuery.CountAsync(cancellationToken);

        var patients = await eligiblePatientsQuery
            .OrderBy(patient => patient.UserName)
            .ThenBy(patient => patient.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(patient => new
            {
                patient.Id,
                patient.UserName,
                patient.PhoneNumber
            })
            .ToListAsync(cancellationToken);

        var eligiblePatients = patients.Select(patient =>
        {
            var patientName = PatientFileNames.Split(patient.UserName);
            return new EligiblePatientDto(
                patient.Id,
                patientName.FirstName,
                patientName.LastName,
                patient.PhoneNumber);
        }).ToList();

        return Result<EligiblePatientPageResponse>.Success(
            new(eligiblePatients, request.Page, request.PageSize, totalCount));
    }
}

public sealed class CreatePatientFileCommandHandler(IPatientFileRepository patientFileRepository, IUnitOfWork unitOfWork) : ICommandHandler<CreatePatientFileCommand, CreatePatientFileResponse>
{
    public async Task<Result<CreatePatientFileResponse>> HandleAsync(CreatePatientFileCommand request, CancellationToken cancellationToken = default)
    {
        if (request.PatientId <= 0)
            return Result<CreatePatientFileResponse>.Failure("شناسه بیمار معتبر نیست");

        var description = request.Description?.Trim();
        if (description?.Length > 2000)
            return Result<CreatePatientFileResponse>.Failure("توضیحات پرونده نمی‌تواند بیشتر از ۲۰۰۰ کاراکتر باشد");

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var patient = await patientFileRepository.Patients.SingleOrDefaultAsync(
                patient => patient.Id == request.PatientId && !patient.IsDeleted,
                cancellationToken);

            if (patient is null)
                return await Rollback("بیمار یافت نشد");

            var attendanceAt = await patientFileRepository.Reservations
                .Where(reservation =>
                    reservation.LeadAssignmentId == patient.Id &&
                    !reservation.IsDeleted &&
                    !reservation.IsCanceled)
                .OrderByDescending(reservation => reservation.ReservationAt)
                .Select(reservation => (DateTime?)reservation.ReservationAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (!attendanceAt.HasValue)
                return await Rollback("بیمار رزرو معتبر ندارد");

            if (await patientFileRepository.PatientFiles.AnyAsync(
                    patientFile =>
                        patientFile.PatientReferenceId == patient.Id &&
                        patientFile.SourceType == PatientFileSourceType.System,
                    cancellationToken))
                return await Rollback("برای این بیمار قبلاً پرونده ایجاد شده است");

            var fileNumber = await patientFileRepository.GetNextFileNumberWithLockAsync(
                DateOnly.FromDateTime(attendanceAt.Value),
                cancellationToken);
            var patientName = PatientFileNames.Split(patient.UserName);

            var patientFile = new PatientFile
            {
                PatientReferenceId = patient.Id,
                FileNumber = fileNumber,
                FirstName = patientName.FirstName,
                LastName = patientName.LastName,
                PhoneNumber = patient.PhoneNumber.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description,
                SourceType = PatientFileSourceType.System
            };

            await patientFileRepository.AddAsync(patientFile, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<CreatePatientFileResponse>.Success(
                new(patientFile.Id, patientFile.FileNumber));

            async Task<Result<CreatePatientFileResponse>> Rollback(string message)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result<CreatePatientFileResponse>.Failure(message);
            }
        }
        catch (InvalidOperationException exception)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return Result<CreatePatientFileResponse>.Failure(exception.Message);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class UpdatePatientFileCommandHandler(IPatientFileRepository patientFileRepository, IUnitOfWork unitOfWork) : ICommandHandler<UpdatePatientFileCommand>
{
    public async Task<Result> HandleAsync(UpdatePatientFileCommand request, CancellationToken cancellationToken = default)
    {
        var firstName = request.FirstName?.Trim() ?? "";
        var lastName = request.LastName?.Trim() ?? "";
        var phoneNumber = request.PhoneNumber?.Trim() ?? "";
        var description = request.Description?.Trim();

        if (request.Id <= 0 || firstName.Length is 0 or > 100 || lastName.Length is 0 or > 100 ||
            phoneNumber.Length is 0 or > 20 || description?.Length > 2000)
            return Result.Failure("اطلاعات پرونده معتبر نیست");

        var patientFile = await patientFileRepository.PatientFiles.SingleOrDefaultAsync(
            patientFile => patientFile.Id == request.Id,
            cancellationToken);

        if (patientFile is null)
            return Result.Failure("پرونده بیمار یافت نشد");

        patientFile.FirstName = firstName;
        patientFile.LastName = lastName;
        patientFile.PhoneNumber = phoneNumber;
        patientFile.Description = string.IsNullOrWhiteSpace(description) ? null : description;
        patientFile.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success("پرونده بیمار ویرایش شد");
    }
}

public sealed class DeletePatientFileCommandHandler(IPatientFileRepository patientFileRepository, IUnitOfWork unitOfWork) : ICommandHandler<DeletePatientFileCommand>
{
    public async Task<Result> HandleAsync(DeletePatientFileCommand request, CancellationToken cancellationToken = default)
    {
        var patientFile = await patientFileRepository.PatientFiles.SingleOrDefaultAsync(
            patientFile => patientFile.Id == request.Id,
            cancellationToken);

        if (patientFile is null)
            return Result.Failure("پرونده بیمار یافت نشد");

        patientFile.IsDeleted = true;
        patientFile.DeletedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success("پرونده بیمار حذف شد");
    }
}

public sealed class ImportPatientFilesCommandHandler(IPatientFileRepository patientFileRepository, IUnitOfWork unitOfWork, IConfiguration configuration) : ICommandHandler<ImportPatientFilesCommand, ImportPatientFilesResponse>
{
    private sealed record Row(int Number, string FirstName, string LastName, long FileNumber, string PhoneNumber);

    public async Task<Result<ImportPatientFilesResponse>> HandleAsync(ImportPatientFilesCommand request, CancellationToken cancellationToken = default)
    {
        var errors = new List<ImportPatientFileError>();
        var maxFileSizeBytes = configuration.GetValue("PatientFiles:ImportMaxBytes", 10 * 1024 * 1024L);
        var maxExpandedFileSizeBytes = configuration.GetValue("PatientFiles:ImportMaxExpandedBytes", 50 * 1024 * 1024L);
        var maxRows = configuration.GetValue("PatientFiles:ImportMaxRows", 5000);

        if (request.Length <= 0)
            return Failure("فایل خالی است");

        if (request.Length > maxFileSizeBytes)
            return Failure("حجم فایل بیش از حد مجاز است");

        if (!string.Equals(Path.GetExtension(request.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return Failure("فقط فایل xlsx مجاز است");

        var rows = new List<Row>();

        try
        {
            await using var safeContent = new MemoryStream();
            await request.Content.CopyToAsync(safeContent, cancellationToken);

            if (safeContent.Length == 0 || safeContent.Length > maxFileSizeBytes)
                return Failure("اندازه واقعی فایل معتبر نیست");

            safeContent.Position = 0;
            var signature = new byte[4];

            if (safeContent.Read(signature) != signature.Length ||
                !signature.SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }))
                return Failure("محتوای فایل یک Excel معتبر از نوع xlsx نیست");

            safeContent.Position = 0;

            if (!IsSafeOpenXmlPackage(safeContent, maxExpandedFileSizeBytes))
                return Failure("ساختار داخلی فایل Excel ناامن یا بیش از حد مجاز است");

            safeContent.Position = 0;

            using var workbook = new XLWorkbook(safeContent);

            if (workbook.Worksheets.Count != 1)
                return Failure("فایل باید دقیقاً شامل یک Worksheet باشد");

            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet is null)
                return Failure("فایل فاقد Worksheet است");

            if (worksheet.CellsUsed().Any(cell => cell.HasFormula))
                return Failure("برای حفظ امنیت، سلول دارای Formula مجاز نیست");

            var headers = Enumerable.Range(1, 4)
                .Select(columnIndex => worksheet.Cell(1, columnIndex).GetString().Trim())
                .ToArray();

            if (!headers.SequenceEqual(new[] { "FirstName", "LastName", "FileNumber", "PhoneNumber" }))
                return Failure("Headerهای فایل معتبر نیستند");

            if ((worksheet.LastColumnUsed()?.ColumnNumber() ?? 0) != 4)
                return Failure("فایل باید دقیقاً شامل چهار ستون تعریف‌شده باشد");

            var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            if (lastRowNumber <= 1)
                return Failure("فایل فاقد ردیف داده است");

            if (lastRowNumber - 1 > maxRows)
                return Failure($"حداکثر تعداد ردیف مجاز {maxRows} است");

            for (var rowNumber = 2; rowNumber <= lastRowNumber; rowNumber++)
            {
                var firstName = worksheet.Cell(rowNumber, 1).GetString().Trim();
                var lastName = worksheet.Cell(rowNumber, 2).GetString().Trim();
                var fileNumberText = worksheet.Cell(rowNumber, 3).GetFormattedString().Trim();
                var phoneNumber = worksheet.Cell(rowNumber, 4).GetFormattedString().Trim();

                if (ContainsUnsafeControlCharacter(firstName) ||
                    ContainsUnsafeControlCharacter(lastName) ||
                    ContainsUnsafeControlCharacter(phoneNumber))
                    errors.Add(new(rowNumber, "Row", "محتوای ردیف شامل کاراکتر کنترلی غیرمجاز است."));

                if (string.IsNullOrWhiteSpace(firstName) || firstName.Length > 100)
                    errors.Add(new(rowNumber, "FirstName", "نام الزامی و حداکثر ۱۰۰ کاراکتر است."));

                if (string.IsNullOrWhiteSpace(lastName) || lastName.Length > 100)
                    errors.Add(new(rowNumber, "LastName", "نام خانوادگی الزامی و حداکثر ۱۰۰ کاراکتر است."));

                if (!long.TryParse(fileNumberText, out var fileNumber) || fileNumber <= 0)
                    errors.Add(new(rowNumber, "FileNumber", "شماره پرونده باید عددی بزرگ‌تر از صفر باشد."));

                if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length > 20)
                    errors.Add(new(rowNumber, "PhoneNumber", "شماره تماس الزامی و حداکثر ۲۰ کاراکتر است."));

                if (!errors.Any(error => error.Row == rowNumber))
                    rows.Add(new(rowNumber, firstName, lastName, fileNumber, phoneNumber));
            }
        }
        catch (Exception)
        {
            return Failure("ساختار فایل Excel معتبر نیست");
        }

        foreach (var duplicateFileNumberGroup in rows.GroupBy(row => row.FileNumber).Where(group => group.Count() > 1))
            foreach (var row in duplicateFileNumberGroup)
                errors.Add(new(row.Number, "FileNumber", $"شماره پرونده {row.FileNumber} در فایل تکراری است."));

        var fileNumbers = rows.Select(row => row.FileNumber).Distinct().ToList();

        var existingFileNumbers = await patientFileRepository.PatientFiles
            .IgnoreQueryFilters()
            .Where(patientFile => fileNumbers.Contains(patientFile.FileNumber))
            .Select(patientFile => patientFile.FileNumber)
            .ToListAsync(cancellationToken);

        foreach (var row in rows.Where(row => existingFileNumbers.Contains(row.FileNumber)))
            errors.Add(new(row.Number, "FileNumber", $"شماره پرونده {row.FileNumber} قبلاً در سیستم وجود دارد."));

        if (errors.Count != 0)
            return Result<ImportPatientFilesResponse>.Success(new(false, 0, errors));

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await patientFileRepository.AddRangeAsync(
                rows.Select(row => new PatientFile
                {
                    FirstName = row.FirstName,
                    LastName = row.LastName,
                    FileNumber = row.FileNumber,
                    PhoneNumber = row.PhoneNumber,
                    SourceType = PatientFileSourceType.Legacy
                }),
                cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<ImportPatientFilesResponse>.Success(
                new(true, rows.Count, []));
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }

        Result<ImportPatientFilesResponse> Failure(string message) =>
            Result<ImportPatientFilesResponse>.Success(
                new(false, 0, [new(0, "File", message)]));

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
                var entryPath = entry.FullName.Replace('\\', '/');

                if (entryPath.StartsWith('/') || entryPath.Split('/').Contains(".."))
                    return false;

                expandedLength = checked(expandedLength + entry.Length);

                if (expandedLength > maxExpandedLength ||
                    entry.CompressedLength > 0 && entry.Length / entry.CompressedLength > 1000)
                    return false;

                hasContentTypes |= entryPath.Equals("[Content_Types].xml", StringComparison.OrdinalIgnoreCase);
                hasWorkbook |= entryPath.Equals("xl/workbook.xml", StringComparison.OrdinalIgnoreCase);
            }

            return hasContentTypes && hasWorkbook;
        }
    }
}
