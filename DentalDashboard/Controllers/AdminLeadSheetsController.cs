using DentalDashboard.LeadManagement.Contract; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;
namespace DentalDashboard.Controllers;
[ApiController,Authorize(Roles="Admin"),Route("api/admin/lead-sheets")]
public sealed class AdminLeadSheetsController(IAdminLeadSheetService service):ControllerBase{
 [HttpPost] public async Task<IActionResult>Create(CreateSheetRequest request,CancellationToken ct){try{return Ok(await service.CreateSheetAsync(request.Name,ct));}catch(ArgumentException e){return BadRequest(new{message=e.Message});}}
 [HttpPost("{sheetId:long}/leads")] public async Task<IActionResult>Add(long sheetId,AddLeadRequest request,CancellationToken ct){try{return Ok(await service.AddLeadAsync(sheetId,request.PhoneNumber,request.FirstName,request.LastName,ct));}catch(ArgumentException e){return BadRequest(new{message=e.Message});}catch(KeyNotFoundException e){return NotFound(new{message=e.Message});}}
 public sealed record CreateSheetRequest(string Name); public sealed record AddLeadRequest(string PhoneNumber,string FirstName,string LastName);
}
