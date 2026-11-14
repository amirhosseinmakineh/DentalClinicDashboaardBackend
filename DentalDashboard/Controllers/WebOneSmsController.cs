using System.Text.RegularExpressions;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/sms/webone")]
public sealed class WebOneSmsController(
    ILeadAssignmentRepository leads,
    IConfiguration configuration,
    ILogger<WebOneSmsController> logger) : ControllerBase
{
    private static readonly HashSet<string> AcceptedTexts = ["3", "5", "8"];

    [HttpGet("receive")]
    public async Task<IActionResult> Receive(
        [FromQuery(Name = "text")] string? text,
        [FromQuery(Name = "from")] string? sender,
        [FromQuery(Name = "to")] string? receiver,
        CancellationToken cancellationToken)
    {
        var dedicatedLine = configuration["WebOneSms:DedicatedLine"];
        if (!string.IsNullOrWhiteSpace(dedicatedLine) &&
            !string.Equals(receiver?.Trim(), dedicatedLine.Trim(), StringComparison.Ordinal))
            return Ok(new { success = true, created = false });

        if (!AcceptedTexts.Contains(NormalizeDigits(text?.Trim())))
            return Ok(new { success = true, created = false });

        var phone = ConsultationRequestsController.NormalizePhone(sender);
        if (!Regex.IsMatch(phone, @"^09[0-9]{9}$"))
            return Ok(new { success = true, created = false });

        try
        {
            var lead = new LeadAssignment
            {
                UserName = "پنل پیامکی",
                PhoneNumber = phone,
                SourceType = LeadSourceType.SmsPanel,
                CreatedAt = DateTime.Now,
                AssignmentType = LeadAssignmentType.RealTime,
                RequiresThreeMinuteCall = true,
                LeadAssignmentState = LeadAssignmentState.New
            };

            var inserted = await leads.AddNewLeadsIfPhoneDoesNotExistAsync([lead], cancellationToken);
            return Ok(new { success = true, created = inserted > 0 });
        }
        catch (Exception exception) when (exception is DbUpdateException or Microsoft.Data.SqlClient.SqlException)
        {
            logger.LogError(exception, "WebOne inbound lead persistence failed.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static string NormalizeDigits(string? value) =>
        string.Concat((value ?? string.Empty).Select(c =>
            c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') :
            c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') : c));
}
