using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.Accountant.PatientFinance
    .Commands;
using DentalDashboard.ApplicationService.Contract.Accountant.PatientFinance
    .Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Accountant.PatientFinance.Controllers;
[ApiController, Authorize]
[Route("api/secretary")]
[Route("api/accountant")]
public sealed class PatientFinanceController(ICommandDispatcher commands,
                                             IQueryDispatcher queries)
    : ControllerBase {
  [HttpPost("patient-financial-cases")]
  public async Task<IActionResult> Create(CreatePatientFinancialCaseCommand c,
                                          CancellationToken ct) {
    if (!UserId(out var id))
      return Unauthorized();
    c.ActorUserId = id;
    return Write(await commands.DispatchAsync(c, ct));
  }
  [HttpPut("patient-financial-cases/{id:guid}")]
  public async Task<IActionResult>
  Update(Guid id, UpdatePatientFinancialCaseCommand c, CancellationToken ct) {
    c.Id = id;
    return Write(await commands.DispatchAsync(c, ct));
  }
  [HttpDelete("patient-financial-cases/{id:guid}")]
  public async Task<IActionResult>
  Cancel(Guid id, CancellationToken ct) => Write(await commands.DispatchAsync(
      new CancelPatientFinancialCaseCommand(id), ct));
  [HttpPost("patient-financial-cases/{caseId:guid}/cheques")]
  public async Task<IActionResult>
  AddCheque(Guid caseId, CreatePatientChequeDto d, CancellationToken ct) =>
      Write(await commands.DispatchAsync(
          new AddPatientChequeCommand(caseId, d.Amount, d.SayadNumber,
                                      d.OwnerName, d.DueDate),
          ct));
  [HttpPost("patient-financial-cases/{caseId:guid}/promissory-notes")]
  public async Task<IActionResult> AddNote(Guid caseId,
                                           CreatePatientPromissoryNoteDto d,
                                           CancellationToken ct) =>
      Write(await commands.DispatchAsync(
          new AddPatientPromissoryNoteCommand(caseId, d.SerialNumber, d.Amount,
                                              d.DueDate),
          ct));
  [HttpPut("patient-cheques/{id:long}/status")]
  public async Task<IActionResult>
  ChequeStatus(long id, UpdatePatientChequeStatusCommand c,
               CancellationToken ct) {
    if (!UserId(out var user))
      return Unauthorized();
    c.ChequeId = id;
    c.ActorUserId = user;
    return Write(await commands.DispatchAsync(c, ct));
  }
  [HttpPut("patient-promissory-notes/{id:long}/status")]
  public async Task<IActionResult>
  NoteStatus(long id, UpdatePatientPromissoryNoteStatusCommand c,
             CancellationToken ct) {
    if (!UserId(out var user))
      return Unauthorized();
    c.PromissoryNoteId = id;
    c.ActorUserId = user;
    return Write(await commands.DispatchAsync(c, ct));
  }
  [HttpPost("patient-debts/{id:long}/pay")]
  public async Task<IActionResult> PayDebt(long id, CancellationToken ct) {
    if (!UserId(out var user))
      return Unauthorized();
    return Write(await commands.DispatchAsync(
        new PayPatientDebtCommand { DebtId = id, ActorUserId = user }, ct));
  }
  [HttpGet("patient-financial-cases")]
  public async Task<IActionResult>
  Cases([FromQuery] GetPatientFinancialCasesQuery q,
        CancellationToken ct) => Ok(await queries.DispatchAsync(q, ct));
  [HttpGet("patient-financial-cases/{id:guid}")]
  public async Task<IActionResult> Details(Guid id, CancellationToken ct) {
    var x = await queries.DispatchAsync(
        new GetPatientFinancialCaseDetailsQuery(id), ct);
    return x is null ? NotFound(Result.Failure("پرونده یافت نشد")) : Ok(x);
  }
  [HttpGet("patient-financial-cases/{id:guid}/summary")]
  public async Task<IActionResult> CaseSummary(Guid id, CancellationToken ct) {
    var x = await queries.DispatchAsync(
        new GetPatientFinancialCaseSummaryQuery(id), ct);
    return x is null ? NotFound(Result.Failure("پرونده یافت نشد")) : Ok(x);
  }
  [HttpGet("patients/{id:guid}/financial-summary")]
  public async Task<IActionResult> PatientSummary(Guid id,
                                                  CancellationToken ct) {
    var x = await queries.DispatchAsync(new GetPatientFinancialSummaryQuery(id),
                                        ct);
    return x is null ? NotFound(Result.Failure("اطلاعات مالی بیمار یافت نشد"))
                     : Ok(x);
  }
  [HttpGet("patient-cheques")]
  public async Task<IActionResult>
  Cheques([FromQuery] GetPatientChequesQuery q,
          CancellationToken ct) => Ok(await queries.DispatchAsync(q, ct));
  [HttpGet("patient-promissory-notes")]
  public async Task<IActionResult>
  Notes([FromQuery] GetPatientPromissoryNotesQuery q,
        CancellationToken ct) => Ok(await queries.DispatchAsync(q, ct));
  [HttpGet("patient-debts")]
  public async Task<IActionResult>
  Debts([FromQuery] GetPatientDebtsQuery q,
        CancellationToken ct) => Ok(await queries.DispatchAsync(q, ct));
  [HttpGet("patient-financial-transactions")]
  public async Task<IActionResult>
  Transactions([FromQuery] GetPatientFinancialTransactionsQuery q,
               CancellationToken ct) => Ok(await queries.DispatchAsync(q, ct));
  [HttpGet("patient-financial-commitments/due")]
  public async Task<IActionResult>
  Due([FromQuery] GetDuePatientFinancialCommitmentsQuery q,
      CancellationToken ct) => Ok(await queries.DispatchAsync(q, ct));
  private IActionResult Write<T>(Result<T> r) => r.IsSuccess ? Ok(r)
                                                             : BadRequest(r);
  private bool UserId(out Guid id) => Guid.TryParse(
      User.FindFirstValue(ClaimTypes.NameIdentifier) ??
          User.FindFirstValue("userId") ?? User.FindFirstValue("Id"),
      out id);
}
