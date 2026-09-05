using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;
using DentalDashboard.Utilities.Time;
using System.Data;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.PatientFinance.Handlers;

internal static class FinanceRules {
  private const string PastDueDateMessage =
      "تاریخ سررسید چک یا سفته نمی‌تواند قبل از امروز باشد.";

  private static bool IsPastDueDate(DateTime due) =>
      IranTimeHelper.GetDateInIran(due) < IranTimeHelper.TodayInIran();

  public static string? Cheque(decimal amount, string? sayad, string? owner,
                               DateTime due) =>
      amount <= 0                        ? "مبلغ چک باید بیشتر از صفر باشد"
      : string.IsNullOrWhiteSpace(sayad) ? "شماره صیاد الزامی است"
      : string.IsNullOrWhiteSpace(owner) ? "نام صاحب چک الزامی است"
      : due == default                   ? "تاریخ سررسید الزامی است"
      : IsPastDueDate(due)               ? PastDueDateMessage
                                         : null;
  public static string? Note(decimal amount, string? serial, DateTime due) =>
      amount <= 0                         ? "مبلغ سفته باید بیشتر از صفر باشد"
      : string.IsNullOrWhiteSpace(serial) ? "شماره سریال سفته الزامی است"
      : due == default                    ? "تاریخ سررسید الزامی است"
      : IsPastDueDate(due)                ? PastDueDateMessage
                                          : null;
}

public sealed class CreatePatientFinancialCaseCommandHandler(
    IPatientFinanceRepository repo, IUnitOfWork uow)
    : ICommandHandler<CreatePatientFinancialCaseCommand,
                      PatientFinancialCaseIdResponse> {
  public async Task<Result<PatientFinancialCaseIdResponse>>
  HandleAsync(CreatePatientFinancialCaseCommand c,
              CancellationToken ct = default) {
    if (c.ActorUserId == Guid.Empty)
      return Result<PatientFinancialCaseIdResponse>.Failure("کاربر معتبر نیست");
    if (c.TotalAmount <= 0)
      return Result<PatientFinancialCaseIdResponse>.Failure(
          "مبلغ کل باید بیشتر از صفر باشد");
    if (c.PrePaymentAmount < 0 || c.DepositAmount < 0 ||
        c.PrePaymentAmount + c.DepositAmount > c.TotalAmount)
      return Result<PatientFinancialCaseIdResponse>.Failure(
          "مبلغ پیش‌پرداخت یا ودیعه معتبر نیست");
    if (!Enum.IsDefined(c.AgreementType))
      return Result<PatientFinancialCaseIdResponse>.Failure("نوع توافق معتبر نیست");
    if (!Enum.IsDefined(typeof(DentalServiceType), c.ServiceId))
      return Result<PatientFinancialCaseIdResponse>.Failure("خدمت معتبر نیست");
    if (c.PatientId == Guid.Empty)
      return Result<PatientFinancialCaseIdResponse>.Failure("بیمار معتبر نیست");
    var patient = await repo.Patients.AsNoTracking()
        .Where(x => !x.IsDeleted && x.Id == c.PatientId &&
                    x.PatientProfile != null && !x.PatientProfile.IsDeleted)
        .Select(x => new { x.Id })
        .SingleOrDefaultAsync(ct);
    if (patient is null)
      return Result<PatientFinancialCaseIdResponse>.Failure("بیمار معتبر نیست");
    var cheques = c.Cheques ?? [];
    var notes = c.PromissoryNotes ?? [];
    if (c.AgreementType == PatientFinancialAgreementType.PrePayment &&
        cheques.Count == 0 && notes.Count == 0)
      return Result<PatientFinancialCaseIdResponse>.Failure(
          "برای پیش‌پرداخت ثبت حداقل یک چک یا سفته الزامی " +
          "است");
    foreach (var x in cheques) {
      var e =
          FinanceRules.Cheque(x.Amount, x.SayadNumber, x.OwnerName, x.DueDate);
      if (e != null)
        return Result<PatientFinancialCaseIdResponse>.Failure(e);
    }
    foreach (var x in notes) {
      var e = FinanceRules.Note(x.Amount, x.SerialNumber, x.DueDate);
      if (e != null)
        return Result<PatientFinancialCaseIdResponse>.Failure(e);
    }
    var entity = new PatientFinancialCase {
      PatientId = patient.Id, Service = (DentalServiceType)c.ServiceId,
      TotalAmount = c.TotalAmount, PrePaymentAmount = c.PrePaymentAmount,
      DepositAmount = c.DepositAmount, AgreementType = c.AgreementType,
      CreatedByUserId = c.ActorUserId
    };
    foreach (var x in cheques)
      entity.Cheques.Add(new PatientCheque { Amount = x.Amount,
                                             SayadNumber = x.SayadNumber.Trim(),
                                             OwnerName = x.OwnerName.Trim(),
                                             DueDate = x.DueDate });
    foreach (var x in notes)
      entity.PromissoryNotes.Add(
          new PatientPromissoryNote { Amount = x.Amount,
                                      SerialNumber = x.SerialNumber.Trim(),
                                      DueDate = x.DueDate });
    await repo.AddCaseAsync(entity, ct);
    await uow.SaveChangesAsync();
    return Result<PatientFinancialCaseIdResponse>.Success(new(entity.Id));
  }
}
public sealed class AddPatientChequeCommandHandler(
    IPatientFinanceRepository repo, IUnitOfWork uow)
    : ICommandHandler<AddPatientChequeCommand, PatientFinanceIdResponse> {
  public async Task<Result<PatientFinanceIdResponse>>
  HandleAsync(AddPatientChequeCommand c, CancellationToken ct = default) {
    var e =
        FinanceRules.Cheque(c.Amount, c.SayadNumber, c.OwnerName, c.DueDate);
    if (e != null)
      return Result<PatientFinanceIdResponse>.Failure(e);
    if (!await repo.Cases.AnyAsync(x => x.Id == c.PatientFinancialCaseId &&
                                        x.Status ==
                                            PatientFinancialCaseStatus.Active,
                                   ct))
      return Result<PatientFinanceIdResponse>.Failure("پرونده فعال یافت نشد");
    var x = new PatientCheque {
      PatientFinancialCaseId = c.PatientFinancialCaseId, Amount = c.Amount,
      SayadNumber = c.SayadNumber.Trim(), OwnerName = c.OwnerName.Trim(),
      DueDate = c.DueDate
    };
    await repo.AddChequeAsync(x, ct);
    await uow.SaveChangesAsync();
    return Result<PatientFinanceIdResponse>.Success(new(x.Id));
  }
}
public sealed class AddPatientPromissoryNoteCommandHandler(
    IPatientFinanceRepository repo, IUnitOfWork uow)
    : ICommandHandler<AddPatientPromissoryNoteCommand,
                      PatientFinanceIdResponse> {
  public async Task<Result<PatientFinanceIdResponse>>
  HandleAsync(AddPatientPromissoryNoteCommand c,
              CancellationToken ct = default) {
    var e = FinanceRules.Note(c.Amount, c.SerialNumber, c.DueDate);
    if (e != null)
      return Result<PatientFinanceIdResponse>.Failure(e);
    if (!await repo.Cases.AnyAsync(x => x.Id == c.PatientFinancialCaseId &&
                                        x.Status ==
                                            PatientFinancialCaseStatus.Active,
                                   ct))
      return Result<PatientFinanceIdResponse>.Failure("پرونده فعال یافت نشد");
    var x = new PatientPromissoryNote {
      PatientFinancialCaseId = c.PatientFinancialCaseId, Amount = c.Amount,
      SerialNumber = c.SerialNumber.Trim(), DueDate = c.DueDate
    };
    await repo.AddPromissoryNoteAsync(x, ct);
    await uow.SaveChangesAsync();
    return Result<PatientFinanceIdResponse>.Success(new(x.Id));
  }
}
public sealed class UpdatePatientFinancialCaseCommandHandler(
    IPatientFinanceRepository repo, IUnitOfWork uow)
    : ICommandHandler<UpdatePatientFinancialCaseCommand,
                      PatientFinancialCaseIdResponse> {
  public async Task<Result<PatientFinancialCaseIdResponse>>
  HandleAsync(UpdatePatientFinancialCaseCommand c,
              CancellationToken ct = default) {
    var x = await repo.Cases.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
    if (x is null)
      return Result<PatientFinancialCaseIdResponse>.Failure("پرونده یافت نشد");
    if (x.Status != PatientFinancialCaseStatus.Active)
      return Result<PatientFinancialCaseIdResponse>.Failure(
          "فقط پرونده فعال قابل ویرایش است");var paid=await repo.Transactions.Where(t=>t.PatientFinancialCaseId==c.Id).SumAsync(t=>(decimal?)t.Amount,ct)??0;
    if (c.TotalAmount <= 0 || c.TotalAmount < paid)
      return Result<PatientFinancialCaseIdResponse>.Failure(
          "مبلغ کل نمی‌تواند کمتر از پرداخت قطعی " +
          "باشد");
    if (c.PrePaymentAmount < 0 || c.DepositAmount < 0 ||
        c.PrePaymentAmount + c.DepositAmount + paid > c.TotalAmount)
      return Result<PatientFinancialCaseIdResponse>.Failure(
          "مبلغ پیش‌پرداخت یا ودیعه معتبر نیست");
    x.TotalAmount = c.TotalAmount;
    x.PrePaymentAmount = c.PrePaymentAmount;
    x.DepositAmount = c.DepositAmount;
    x.AgreementType = c.AgreementType;
    x.UpdatedAt = DateTime.UtcNow;
    await uow.SaveChangesAsync();
    return Result<PatientFinancialCaseIdResponse>.Success(new(x.Id));
  }
}
public sealed class CancelPatientFinancialCaseCommandHandler(
    IPatientFinanceRepository repo, IUnitOfWork uow)
    : ICommandHandler<CancelPatientFinancialCaseCommand,
                      PatientFinancialCaseIdResponse> {
  public async Task<Result<PatientFinancialCaseIdResponse>>
  HandleAsync(CancelPatientFinancialCaseCommand c,
              CancellationToken ct = default) {
    await uow.BeginTransactionAsync(ct, IsolationLevel.Serializable);
    try {
      var x = await repo.Cases.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
      if (x is null) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinancialCaseIdResponse>.Failure("پرونده یافت نشد");
      }
      if (x.Status != PatientFinancialCaseStatus.Active) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinancialCaseIdResponse>.Failure(
            "فقط پرونده فعال قابل لغو است");
      }
      if (x.AgreementType != PatientFinancialAgreementType.Deposit) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinancialCaseIdResponse>.Failure(
            "لغو مالی فقط برای توافق ودیعه امکان‌پذیر است.");
      }
      if (await repo.Transactions.AnyAsync(t =>
              t.PatientFinancialCaseId == c.Id &&
              t.Type == PatientFinancialTransactionType.Payment, ct)) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinancialCaseIdResponse>.Failure(
            "پس از ثبت اولین پرداخت، لغو مالی بیمار امکان‌پذیر نیست.");
      }
      x.Status = PatientFinancialCaseStatus.Cancelled;
      x.UpdatedAt = DateTime.UtcNow;
      await uow.CommitAsync(ct);
      return Result<PatientFinancialCaseIdResponse>.Success(new(x.Id));
    } catch {
      await uow.RollbackAsync(ct);
      throw;
    }
  }
}

public sealed class UpdatePatientChequeCommandHandler(
    IPatientFinanceRepository repo, IUnitOfWork uow)
    : ICommandHandler<UpdatePatientChequeCommand, PatientFinanceIdResponse> {
  public async Task<Result<PatientFinanceIdResponse>> HandleAsync(
      UpdatePatientChequeCommand c, CancellationToken ct = default) {
    if (c.AdditionalFields is { Count: > 0 })
      return Result<PatientFinanceIdResponse>.Failure(
          "درخواست شامل فیلد غیرمجاز است.");
    if (c.Amount <= 0)
      return Result<PatientFinanceIdResponse>.Failure(
          "مبلغ چک باید بیشتر از صفر باشد");
    if (string.IsNullOrWhiteSpace(c.OwnerName))
      return Result<PatientFinanceIdResponse>.Failure("نام صاحب چک الزامی است");
    if (c.OwnerName.Trim().Length > 200)
      return Result<PatientFinanceIdResponse>.Failure(
          "نام صاحب چک نمی‌تواند بیشتر از ۲۰۰ نویسه باشد");

    await uow.BeginTransactionAsync(ct, IsolationLevel.Serializable);
    try {
      var cheque = await repo.Cheques.Include(x => x.FinancialCase)
          .FirstOrDefaultAsync(x => x.Id == c.ChequeId, ct);
      if (cheque is null) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinanceIdResponse>.Failure("چک یافت نشد");
      }
      if (cheque.Status != PatientChequeStatus.Pending) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinanceIdResponse>.Failure(
            "چک تأیید یا رد شده است و دیگر قابل ویرایش نیست.");
      }
      var otherCommitments = await repo.Cheques
          .Where(x => x.PatientFinancialCaseId == cheque.PatientFinancialCaseId &&
                      x.Id != cheque.Id)
          .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
      otherCommitments += await repo.PromissoryNotes
          .Where(x => x.PatientFinancialCaseId == cheque.PatientFinancialCaseId)
          .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
      if (otherCommitments + c.Amount > cheque.FinancialCase.TotalAmount) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinanceIdResponse>.Failure(
            "مجموع تعهدات نمی‌تواند از مبلغ کل پرونده بیشتر باشد");
      }
      cheque.Amount = c.Amount;
      cheque.OwnerName = c.OwnerName.Trim();
      cheque.UpdatedAt = DateTime.UtcNow;
      await uow.CommitAsync(ct);
      return Result<PatientFinanceIdResponse>.Success(
          new(cheque.Id), "اطلاعات چک با موفقیت ویرایش شد.");
    } catch {
      await uow.RollbackAsync(ct);
      throw;
    }
  }
}

public sealed class UpdatePatientPromissoryNoteCommandHandler(
    IPatientFinanceRepository repo, IUnitOfWork uow)
    : ICommandHandler<UpdatePatientPromissoryNoteCommand,
                      PatientFinanceIdResponse> {
  public async Task<Result<PatientFinanceIdResponse>> HandleAsync(
      UpdatePatientPromissoryNoteCommand c, CancellationToken ct = default) {
    if (c.AdditionalFields is { Count: > 0 })
      return Result<PatientFinanceIdResponse>.Failure(
          "درخواست شامل فیلد غیرمجاز است.");
    if (c.Amount <= 0)
      return Result<PatientFinanceIdResponse>.Failure(
          "مبلغ سفته باید بیشتر از صفر باشد");

    await uow.BeginTransactionAsync(ct, IsolationLevel.Serializable);
    try {
      var note = await repo.PromissoryNotes.Include(x => x.FinancialCase)
          .FirstOrDefaultAsync(x => x.Id == c.PromissoryNoteId, ct);
      if (note is null) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinanceIdResponse>.Failure("سفته یافت نشد");
      }
      if (note.Status != PatientPromissoryNoteStatus.Pending) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinanceIdResponse>.Failure(
            "سفته تأیید یا رد شده است و دیگر قابل ویرایش نیست.");
      }
      var otherCommitments = await repo.PromissoryNotes
          .Where(x => x.PatientFinancialCaseId == note.PatientFinancialCaseId &&
                      x.Id != note.Id)
          .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
      otherCommitments += await repo.Cheques
          .Where(x => x.PatientFinancialCaseId == note.PatientFinancialCaseId)
          .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
      if (otherCommitments + c.Amount > note.FinancialCase.TotalAmount) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinanceIdResponse>.Failure(
            "مجموع تعهدات نمی‌تواند از مبلغ کل پرونده بیشتر باشد");
      }
      note.Amount = c.Amount;
      note.UpdatedAt = DateTime.UtcNow;
      await uow.CommitAsync(ct);
      return Result<PatientFinanceIdResponse>.Success(
          new(note.Id), "اطلاعات سفته با موفقیت ویرایش شد.");
    } catch {
      await uow.RollbackAsync(ct);
      throw;
    }
  }
}

public sealed class UpdatePatientChequeStatusCommandHandler(
    IPatientFinanceRepository repo, IUnitOfWork uow)
    : ICommandHandler<UpdatePatientChequeStatusCommand,
                      PatientFinanceIdResponse> {
  public async Task<Result<PatientFinanceIdResponse>>
  HandleAsync(UpdatePatientChequeStatusCommand c,
              CancellationToken ct = default) {
    if (c.Status is not (PatientChequeStatus.Paid or PatientChequeStatus.Unpaid))
      return Result<PatientFinanceIdResponse>.Failure("وضعیت مقصد معتبر نیست");
    await uow.BeginTransactionAsync(ct, IsolationLevel.Serializable);
    try {
      var x = await repo.Cheques.Include(x => x.FinancialCase)
                  .FirstOrDefaultAsync(x => x.Id == c.ChequeId, ct);
      if (x is null) {
        await uow.RollbackAsync();
        return Result<PatientFinanceIdResponse>.Failure("چک یافت نشد");
      }
      if (x.Status != PatientChequeStatus.Pending) {
        await uow.RollbackAsync();
        return Result<PatientFinanceIdResponse>.Failure(
            "وضعیت چک قبلاً تعیین شده و قابل تغییر نیست");
      }
      if ((c.Status is PatientChequeStatus.Paid or PatientChequeStatus.Unpaid) &&
          IranTimeHelper.TodayInIran() < IranTimeHelper.GetDateInIran(x.DueDate)) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinanceIdResponse>.Failure(
            "ثبت نتیجه پرداخت فقط از روز سررسید امکان‌پذیر است");
      }
      if (c.Status == PatientChequeStatus.Paid) {var paid=await repo.Transactions.Where(t=>t.PatientFinancialCaseId==x.PatientFinancialCaseId).SumAsync(t=>(decimal?)t.Amount,ct)??0;
        if (paid + x.Amount > x.FinancialCase.TotalAmount) {
          await uow.RollbackAsync();
          return Result<PatientFinanceIdResponse>.Failure(
              "پرداخت از مبلغ کل درمان بیشتر " +
              "می‌شود");
        }
        var legacyDebts = await repo.Debts
            .Where(d => d.SourceType == PatientDebtSourceType.Cheque &&
                        d.SourceId == x.Id &&
                        d.Status == PatientDebtStatus.Unpaid)
            .ToListAsync(ct);
        foreach (var debt in legacyDebts)
          debt.Status = PatientDebtStatus.Cancelled;
        await repo.AddTransactionAsync(
            new() { PatientFinancialCaseId = x.PatientFinancialCaseId,
                    Amount = x.Amount,
                    SourceType = PatientFinancialTransactionSourceType.Cheque,
                    SourceId = x.Id,
                    CreatedByUserId = c.ActorUserId },
            ct);
        if (paid + x.Amount == x.FinancialCase.TotalAmount)
          x.FinancialCase.Status = PatientFinancialCaseStatus.Completed;
      } else if (c.Status == PatientChequeStatus.Unpaid) {
        if (await repo.Debts.AnyAsync(d => d.SourceType ==
                                               PatientDebtSourceType.Cheque &&
                                           d.SourceId == x.Id,
                                      ct)) {
          await uow.RollbackAsync();
          return Result<PatientFinanceIdResponse>.Failure(
              "برای این چک قبلاً بدهی ثبت شده است");
        }
        await repo.AddDebtAsync(
            new() { PatientFinancialCaseId = x.PatientFinancialCaseId,
                    Amount = x.Amount,
                    SourceType = PatientDebtSourceType.Cheque, SourceId = x.Id,
                    DueDate = x.DueDate },
            ct);
      }
      x.Status = c.Status;
      await uow.CommitAsync();
      return Result<PatientFinanceIdResponse>.Success(new(x.Id));
    } catch {
      await uow.RollbackAsync();
      throw;
    }
  }
}

public sealed class UpdatePatientPromissoryNoteStatusCommandHandler(
    IPatientFinanceRepository repo, IUnitOfWork uow)
    : ICommandHandler<UpdatePatientPromissoryNoteStatusCommand,
                      PatientFinanceIdResponse> {
  public async Task<Result<PatientFinanceIdResponse>>
  HandleAsync(UpdatePatientPromissoryNoteStatusCommand c,
              CancellationToken ct = default) {
    if (c.Status is not (PatientPromissoryNoteStatus.Paid or
                         PatientPromissoryNoteStatus.Unpaid))
      return Result<PatientFinanceIdResponse>.Failure("وضعیت مقصد معتبر نیست");
    await uow.BeginTransactionAsync(ct, IsolationLevel.Serializable);
    try {
      var x = await repo.PromissoryNotes.Include(x => x.FinancialCase)
                  .FirstOrDefaultAsync(x => x.Id == c.PromissoryNoteId, ct);
      if (x is null) {
        await uow.RollbackAsync();
        return Result<PatientFinanceIdResponse>.Failure("سفته یافت نشد");
      }
      if (x.Status != PatientPromissoryNoteStatus.Pending) {
        await uow.RollbackAsync();
        return Result<PatientFinanceIdResponse>.Failure(
            "وضعیت سفته قبلاً تعیین شده و قابل تغییر نیست");
      }
      if ((c.Status is PatientPromissoryNoteStatus.Paid or PatientPromissoryNoteStatus.Unpaid) &&
          IranTimeHelper.TodayInIran() < IranTimeHelper.GetDateInIran(x.DueDate)) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinanceIdResponse>.Failure(
            "ثبت نتیجه پرداخت فقط از روز سررسید امکان‌پذیر است");
      }
      if (c.Status == PatientPromissoryNoteStatus.Paid) {var paid=await repo.Transactions.Where(t=>t.PatientFinancialCaseId==x.PatientFinancialCaseId).SumAsync(t=>(decimal?)t.Amount,ct)??0;
        if (paid + x.Amount > x.FinancialCase.TotalAmount) {
          await uow.RollbackAsync();
          return Result<PatientFinanceIdResponse>.Failure(
              "پرداخت از مبلغ کل درمان بیشتر " +
              "می‌شود");
        }
        var legacyDebts = await repo.Debts
            .Where(d => d.SourceType == PatientDebtSourceType.PromissoryNote &&
                        d.SourceId == x.Id &&
                        d.Status == PatientDebtStatus.Unpaid)
            .ToListAsync(ct);
        foreach (var debt in legacyDebts)
          debt.Status = PatientDebtStatus.Cancelled;
        await repo.AddTransactionAsync(
            new() { PatientFinancialCaseId = x.PatientFinancialCaseId,
                    Amount = x.Amount,
                    SourceType =
                        PatientFinancialTransactionSourceType.PromissoryNote,
                    SourceId = x.Id,
                    CreatedByUserId = c.ActorUserId },
            ct);
        if (paid + x.Amount == x.FinancialCase.TotalAmount)
          x.FinancialCase.Status = PatientFinancialCaseStatus.Completed;
      } else if (c.Status == PatientPromissoryNoteStatus.Unpaid) {
        if (await repo.Debts.AnyAsync(
                d => d.SourceType == PatientDebtSourceType.PromissoryNote &&
                     d.SourceId == x.Id,
                ct)) {
          await uow.RollbackAsync();
          return Result<PatientFinanceIdResponse>.Failure(
              "برای این سفته قبلاً بدهی ثبت شده است");
        }
        await repo.AddDebtAsync(
            new() { PatientFinancialCaseId = x.PatientFinancialCaseId,
                    Amount = x.Amount,
                    SourceType = PatientDebtSourceType.PromissoryNote,
                    SourceId = x.Id, DueDate = x.DueDate },
            ct);
      }
      x.Status = c.Status;
      await uow.CommitAsync();
      return Result<PatientFinanceIdResponse>.Success(new(x.Id));
    } catch {
      await uow.RollbackAsync();
      throw;
    }
  }
}

public sealed class PayPatientDebtCommandHandler(IPatientFinanceRepository repo,
                                                 IUnitOfWork uow)
    : ICommandHandler<PayPatientDebtCommand, PatientFinanceIdResponse> {
  public async Task<Result<PatientFinanceIdResponse>>
  HandleAsync(PayPatientDebtCommand c, CancellationToken ct = default) {
    await uow.BeginTransactionAsync(ct, IsolationLevel.Serializable);
    try {
      var d = await repo.Debts.Include(x => x.FinancialCase)
                  .FirstOrDefaultAsync(x => x.Id == c.DebtId, ct);
      if (d is null || d.Status != PatientDebtStatus.Unpaid) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinanceIdResponse>.Failure(
            "بدهی پرداخت‌نشده یافت نشد");
      }
      var hasPendingCommitment =
          await repo.Cheques.AnyAsync(
              x => x.PatientFinancialCaseId == d.PatientFinancialCaseId &&
                   x.Status == PatientChequeStatus.Pending,
              ct) ||
          await repo.PromissoryNotes.AnyAsync(
              x => x.PatientFinancialCaseId == d.PatientFinancialCaseId &&
                   x.Status == PatientPromissoryNoteStatus.Pending,
              ct);
      if (hasPendingCommitment) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinanceIdResponse>.Failure(
            "تا تعیین تکلیف همه چک‌ها و سفته‌های در گردش، تسویه کامل بدهی امکان‌پذیر نیست");
      }
      var st = d.SourceType == PatientDebtSourceType.Cheque
                   ? PatientFinancialTransactionSourceType.Cheque
                   : PatientFinancialTransactionSourceType.PromissoryNote;
      if (await repo.Transactions.AnyAsync(
              x => x.SourceType == st && x.SourceId == d.SourceId, ct)) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinanceIdResponse>.Failure(
            "این تعهد قبلاً پرداخت شده است");
      }
      var paid = await repo.Transactions
                       .Where(x => x.PatientFinancialCaseId ==
                                   d.PatientFinancialCaseId)
                       .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
      if (paid + d.Amount > d.FinancialCase.TotalAmount) {
        await uow.RollbackAsync(ct);
        return Result<PatientFinanceIdResponse>.Failure(
            "پرداخت از مبلغ کل درمان بیشتر " +
            "می‌شود");
      }
      if (d.SourceType == PatientDebtSourceType.Cheque) {
        var source = await repo.Cheques.FirstOrDefaultAsync(
            x => x.Id == d.SourceId &&
                 x.Status == PatientChequeStatus.Unpaid,
            ct);
        if (source is null) {
          await uow.RollbackAsync(ct);
          return Result<PatientFinanceIdResponse>.Failure(
              "چک پرداخت‌نشده مرتبط با بدهی یافت نشد");
        }
        source.Status = PatientChequeStatus.Paid;
      } else {
        var source = await repo.PromissoryNotes.FirstOrDefaultAsync(
            x => x.Id == d.SourceId &&
                 x.Status == PatientPromissoryNoteStatus.Unpaid,
            ct);
        if (source is null) {
          await uow.RollbackAsync(ct);
          return Result<PatientFinanceIdResponse>.Failure(
              "سفته پرداخت‌نشده مرتبط با بدهی یافت نشد");
        }
        source.Status = PatientPromissoryNoteStatus.Paid;
      }
      await repo.AddTransactionAsync(
          new() { PatientFinancialCaseId = d.PatientFinancialCaseId,
                  Amount = d.Amount, SourceType = st, SourceId = d.SourceId,
                  CreatedByUserId = c.ActorUserId },
          ct);
      d.Status = PatientDebtStatus.Paid;
      if (paid + d.Amount == d.FinancialCase.TotalAmount)
        d.FinancialCase.Status = PatientFinancialCaseStatus.Completed;
      await uow.CommitAsync(ct);
      return Result<PatientFinanceIdResponse>.Success(new(d.Id));
    } catch {
      await uow.RollbackAsync(ct);
      throw;
    }
  }
}
