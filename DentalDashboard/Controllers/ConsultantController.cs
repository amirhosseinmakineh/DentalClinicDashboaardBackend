using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Domain.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConsultantController : ControllerBase
    {
        private readonly ICommandDispatcher dispatcher;
        private readonly IQueryDispatcher queryDispatcher;
        private readonly ISecretaryAccessService secretaryAccessService;
        private readonly IConsultantProfileRepository consultantProfileRepository;
        public ConsultantController(
            ICommandDispatcher commandDispatcher,
            IQueryDispatcher queryDispatcher,
            ISecretaryAccessService secretaryAccessService,
            IConsultantProfileRepository consultantProfileRepository)
        {
            dispatcher = commandDispatcher;
            this.queryDispatcher = queryDispatcher;
            this.secretaryAccessService = secretaryAccessService;
            this.consultantProfileRepository = consultantProfileRepository;
        }
        [HttpGet("GetConsultants")]
        [Authorize]
        public async Task<IActionResult> GetConsultants(
            [FromQuery]GetConsultantQuery query,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            var access = await secretaryAccessService.GetAccessAsync(userId, cancellationToken);
            var isConsultant = await consultantProfileRepository.GetAll()
                .AnyAsync(x => x.UserId == userId && !x.IsDeleted, cancellationToken);
            if (access.IsSecretary && !isConsultant &&
                !await secretaryAccessService.HasPermissionAsync(userId,
                    DentalDashboard.Domain.Enums.SecretaryPermissionType.CreateReservation,
                    cancellationToken))
                return Forbid();

            var result = await queryDispatcher.DispatchAsync(query, cancellationToken);
            return Ok(result);
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                        User.FindFirstValue("userId") ?? User.FindFirstValue("Id");
            return Guid.TryParse(value, out userId);
        }

        [HttpPost]
        public async Task<IActionResult> CompleteProfile(CompleteConsultantProfileCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }
        [HttpPost("SetAvalableConsultant")]
        public async Task<IActionResult> SetAvalableConsultant(SetAvailableCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }
        [HttpPost("SetOnlineOfflineConsultant")]
        public async Task<IActionResult> SetOnlineOfflineConsultant(SetOnlineOfflineCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("RegisterPushToken")]
        public async Task<IActionResult> RegisterPushToken(RegisterPushTokenCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("SendTestPushNotification")]
        public async Task<IActionResult> SendTestPushNotification(
            SendTestPushNotificationCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpGet("WebPushPublicKey")]
        public IActionResult WebPushPublicKey([FromServices] IConfiguration configuration)
        {
            var publicKey = configuration["WebPush:VapidPublicKey"]
                            ?? Environment.GetEnvironmentVariable("WEBPUSH_VAPID_PUBLIC_KEY");

            if (string.IsNullOrWhiteSpace(publicKey))
            {
                return Ok(Result<string>.Failure("کلید عمومی Web Push پیکربندی نشده است"));
            }

            return Ok(Result<string>.Success(publicKey.Trim()));
        }

        [HttpGet("WebPushHealth")]
        public IActionResult WebPushHealth([FromServices] IConfiguration configuration)
        {
            var publicKey = configuration["WebPush:VapidPublicKey"]
                            ?? Environment.GetEnvironmentVariable("WEBPUSH_VAPID_PUBLIC_KEY");
            var privateKey = configuration["WebPush:VapidPrivateKey"]
                             ?? Environment.GetEnvironmentVariable("WEBPUSH_VAPID_PRIVATE_KEY");

            var ready =
                !string.IsNullOrWhiteSpace(publicKey) &&
                !string.IsNullOrWhiteSpace(privateKey);

            return Ok(Result<object>.Success(new
            {
                ready,
                publicKeyConfigured = !string.IsNullOrWhiteSpace(publicKey),
                privateKeyConfigured = !string.IsNullOrWhiteSpace(privateKey),
            }));
        }

        [HttpGet("GetDashboardStatus")]
        public async Task<IActionResult> GetDashboardStatus([FromQuery] GetConsultantDashboardStatusQuery query)
        {
            var result = await queryDispatcher.DispatchAsync(query);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("CanPickupLead")]
        public async Task<IActionResult> CanPickupLead(
            [FromQuery] long profileId,
            [FromServices] ILeadAssignmentLimitService leadAssignmentLimitService)
        {
            var limitStatus = await leadAssignmentLimitService
                .GetDailyLimitStatusAsync(profileId);

            return Ok(Result<object>.Success(new
            {
                canPickup = limitStatus.CanPickup,
                dailyLimit = limitStatus.EffectiveDailyLimit,
                todayPickupCount = limitStatus.TodayPickupCount,
                message = limitStatus.CanPickup
                    ? null
                    : limitStatus.DailyLimitReachedMessage
            }));
        }

        [HttpPost("SubmitLeadCallReport")]
        public async Task<IActionResult> SubmitLeadCallReport(SubmitLeadCallReportCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpPost("ExpireLeadNoCall")]
        public async Task<IActionResult> ExpireLeadNoCall(ExpireLeadNoCallCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpPost("RecordLeadCallInitiated")]
        public async Task<IActionResult> RecordLeadCallInitiated(RecordLeadCallInitiatedCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpPost("UpdateLeadCallReport")]
        public async Task<IActionResult> UpdateLeadCallReport(UpdateLeadCallReportCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpGet("GetLeads")]
        public async Task<IActionResult> GetLeads([FromQuery]GetLeadsQuery query)
        {
            var result = await queryDispatcher.DispatchAsync(query);
            return Ok(result);
        }

        [HttpGet("GetBroadcastRealtimeLeads")]
        public async Task<IActionResult> GetBroadcastRealtimeLeads(
            [FromQuery] GetBroadcastRealtimeLeadsQuery query)
        {
            var result = await queryDispatcher.DispatchAsync(query);
            return Ok(Result<object>.Success(result));
        }

        [HttpPost("CreateConsultantPatientLead")]
        public async Task<IActionResult> CreateConsultantPatientLead(AddConsultantPatientLeadCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpPost("AddPatientLead")]
        public async Task<IActionResult> AddPatientLead(AddConsultantPatientLeadCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }
    }
}
