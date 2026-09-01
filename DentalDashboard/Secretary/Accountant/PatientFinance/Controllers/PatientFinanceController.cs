
using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Commands;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Secretary.Accountant.PatientFinance.Controllers;

[ApiController]
[Authorize]
[Route("api/secretary")]
public sealed class PatientFinanceController(ICommandDispatcher commands, IQueryDispatcher queries) : ControllerBase
{
    [HttpPost("patient-financial-cases")]
    public async Task<IActionResult> Create(CreatePatientFinancialCaseCommand command, CancellationToken cancellationToken)
    {
        if (!UserId(out var userId))
            return Unauthorized();

        command.ActorUserId = userId;

        return Write(await commands.DispatchAsync(command, cancellationToken));
    }

    [HttpPut("patient-financial-cases/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePatientFinancialCaseCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;

        return Write(await commands.DispatchAsync(command, cancellationToken));
    }

    [HttpDelete("patient-financial-cases/{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        return Write(await commands.DispatchAsync(new CancelPatientFinancialCaseCommand(id), cancellationToken));
    }

    [HttpPost("patient-financial-cases/{caseId:guid}/cheques")]
    public async Task<IActionResult> AddCheque(Guid caseId, CreatePatientChequeDto dto, CancellationToken cancellationToken)
    {
        var command = new AddPatientChequeCommand(
            caseId,
            dto.Amount,
            dto.SayadNumber,
            dto.OwnerName,
            dto.DueDate);

        return Write(await commands.DispatchAsync(command, cancellationToken));
    }

    [HttpPost("patient-financial-cases/{caseId:guid}/promissory-notes")]
    public async Task<IActionResult> AddNote(Guid caseId, CreatePatientPromissoryNoteDto dto, CancellationToken cancellationToken)
    {
        var command = new AddPatientPromissoryNoteCommand(
            caseId,
            dto.SerialNumber,
            dto.Amount,
            dto.DueDate);

        return Write(await commands.DispatchAsync(command, cancellationToken));
    }

    [HttpPut("patient-cheques/{id:long}/status")]
    public async Task<IActionResult> ChequeStatus(long id, UpdatePatientChequeStatusCommand command, CancellationToken cancellationToken)
    {
        if (!UserId(out var userId))
            return Unauthorized();

        command.ChequeId = id;
        command.ActorUserId = userId;

        return Write(await commands.DispatchAsync(command, cancellationToken));
    }

    [HttpPut("patient-cheques/{id:long}")]
    public async Task<IActionResult> UpdateCheque(long id, UpdatePatientChequeCommand command, CancellationToken cancellationToken)
    {
        command.ChequeId = id;
        return Write(await commands.DispatchAsync(command, cancellationToken));
    }

    [HttpPut("patient-promissory-notes/{id:long}")]
    public async Task<IActionResult> UpdateNote(long id, UpdatePatientPromissoryNoteCommand command, CancellationToken cancellationToken)
    {
        command.PromissoryNoteId = id;
        return Write(await commands.DispatchAsync(command, cancellationToken));
    }

    [HttpPut("patient-promissory-notes/{id:long}/status")]
    public async Task<IActionResult> NoteStatus(long id, UpdatePatientPromissoryNoteStatusCommand command, CancellationToken cancellationToken)
    {
        if (!UserId(out var userId))
            return Unauthorized();

        command.PromissoryNoteId = id;
        command.ActorUserId = userId;

        return Write(await commands.DispatchAsync(command, cancellationToken));
    }

    [HttpPost("patient-debts/{id:long}/pay")]
    public async Task<IActionResult> PayDebt(long id, CancellationToken cancellationToken)
    {
        if (!UserId(out var userId))
            return Unauthorized();

        var command = new PayPatientDebtCommand
        {
            DebtId = id,
            ActorUserId = userId
        };

        return Write(await commands.DispatchAsync(command, cancellationToken));
    }

    [HttpGet("patient-financial-cases")]
    public async Task<IActionResult> Cases([FromQuery] GetPatientFinancialCasesQuery query, CancellationToken cancellationToken)
    {
        return Ok(await queries.DispatchAsync(query, cancellationToken));
    }

    [HttpGet("patient-financial-cases/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var result = await queries.DispatchAsync(new GetPatientFinancialCaseDetailsQuery(id), cancellationToken);

        return result is null
            ? NotFound(Result.Failure("پرونده یافت نشد"))
            : Ok(result);
    }

    [HttpGet("patient-financial-cases/{id:guid}/summary")]
    public async Task<IActionResult> CaseSummary(Guid id, CancellationToken cancellationToken)
    {
        var result = await queries.DispatchAsync(new GetPatientFinancialCaseSummaryQuery(id), cancellationToken);

        return result is null
            ? NotFound(Result.Failure("پرونده یافت نشد"))
            : Ok(result);
    }

    [HttpGet("patients/{id:guid}/financial-summary")]
    public async Task<IActionResult> PatientSummary(Guid id, CancellationToken cancellationToken)
    {
        var result = await queries.DispatchAsync(new GetPatientFinancialSummaryQuery(id), cancellationToken);

        return result is null
            ? NotFound(Result.Failure("اطلاعات مالی بیمار یافت نشد"))
            : Ok(result);
    }

    [HttpGet("patient-cheques")]
    public async Task<IActionResult> Cheques([FromQuery] GetPatientChequesQuery query, CancellationToken cancellationToken)
    {
        return Ok(await queries.DispatchAsync(query, cancellationToken));
    }

    [HttpGet("patient-promissory-notes")]
    public async Task<IActionResult> Notes([FromQuery] GetPatientPromissoryNotesQuery query, CancellationToken cancellationToken)
    {
        return Ok(await queries.DispatchAsync(query, cancellationToken));
    }

    [HttpGet("patient-debts")]
    public async Task<IActionResult> Debts([FromQuery] GetPatientDebtsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await queries.DispatchAsync(query, cancellationToken));
    }

    [HttpGet("patient-financial-transactions")]
    public async Task<IActionResult> Transactions([FromQuery] GetPatientFinancialTransactionsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await queries.DispatchAsync(query, cancellationToken));
    }

    [HttpGet("patient-financial-commitments/due")]
    public async Task<IActionResult> Due([FromQuery] GetDuePatientFinancialCommitmentsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await queries.DispatchAsync(query, cancellationToken));
    }

    private IActionResult Write<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result);

        if (result.Message.Contains("یافت نشد"))
            return NotFound(result);

        if (result.Message.Contains("قابل ویرایش نیست") ||
            result.Message.Contains("قابل لغو") ||
            result.Message.Contains("روز سررسید") ||
            result.Message.Contains("قبلاً تعیین شده") ||
            result.Message.Contains("تسویه کامل بدهی امکان‌پذیر نیست"))
            return Conflict(result);

        return BadRequest(result);
    }

    private bool UserId(out Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("userId")
                     ?? User.FindFirstValue("Id");

        return Guid.TryParse(userId, out id);
    }
}
