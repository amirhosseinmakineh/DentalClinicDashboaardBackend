using System.Security.Cryptography;
using System.Text;
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
    [HttpGet("receive")]
    public async Task<IActionResult> Receive(
        [FromQuery(Name = "text")] string? text,
        [FromQuery(Name = "from")] string? sender,
        [FromQuery(Name = "to")] string? receiver,
        [FromQuery(Name = "key")] string? key,
        CancellationToken cancellationToken)
    {
        var secret = configuration["WebOneSms:WebhookSecret"];
        var dedicatedLine = configuration["WebOneSms:DedicatedLine"];

        // Fail closed when the integration has not been configured.
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(dedicatedLine))
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        if (string.IsNullOrEmpty(key) ||
            !CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(key)),
                SHA256.HashData(Encoding.UTF8.GetBytes(secret))))
            return Unauthorized();

        if (!string.Equals(receiver?.Trim(), dedicatedLine.Trim(), StringComparison.Ordinal))
            return Ok(new { success = true, ignored = true });

        var normalizedText = NormalizeDigits(text?.Trim());
        if (normalizedText != "5")
            return Ok(new { success = true, ignored = true });

        var phone = ConsultationRequestsController.NormalizePhone(sender);
        if (!Regex.IsMatch(phone, @"^09[0-9]{9}$"))
            return Ok(new { success = true, ignored = true });

        try
        {
            var lead = new LeadAssignment
            {
                UserName = "پنل پیامکی(5)",
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
            logger.LogError(exception, "WebOne SMS lead persistence failed.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static string NormalizeDigits(string? text) =>
        string.Concat((text ?? string.Empty).Select(c =>
            c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') :
            c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') : c));
}
