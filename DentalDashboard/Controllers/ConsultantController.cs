using DentalDashboard.ApplicationService.Contract.Requests.Consultant;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DentalDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConsultantController : ControllerBase
    {
        private readonly ICommandDispatcher dispatcher;
        private readonly IQueryDispatcher queryDispatcher;
        public ConsultantController(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher)
        {
            dispatcher = commandDispatcher;
            this.queryDispatcher = queryDispatcher;
        }
        [HttpGet("GetConsultants")]
        public async Task<IActionResult> GetConsultants([FromQuery]GetConsultantQuery query)
        {
            var result = await queryDispatcher.DispatchAsync(query);
            return Ok(result);
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

        [HttpGet("GetDashboardStatus")]
        public async Task<IActionResult> GetDashboardStatus([FromQuery] GetConsultantDashboardStatusQuery query)
        {
            var result = await queryDispatcher.DispatchAsync(query);
            return Ok(result);
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

        [HttpGet("GetLeads")]
        public async Task<IActionResult> GetLeads([FromQuery]GetLeadsQuery query)
        {
            var result = await queryDispatcher.DispatchAsync(query);
            return Ok(result);
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

        [HttpGet("GetBroadcastingLeads")]
        public async Task<IActionResult> GetBroadcastingLeads([FromQuery] GetBroadcastingLeadsQuery query)
        {
            var result = await queryDispatcher.DispatchAsync(query);
            return Ok(result);
        }

        [HttpPost("AcceptLead")]
        public async Task<IActionResult> AcceptLead(AcceptLeadCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            if (!result.IsSuccess)
                return Conflict(result);

            return Ok(result);
        }

        [HttpPost("RejectBroadcast")]
        public async Task<IActionResult> RejectBroadcast(RejectBroadcastCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpPost("SeedTestBroadcastLead")]
        public async Task<IActionResult> SeedTestBroadcastLead()
        {
            var result = await dispatcher.DispatchAsync(new SeedTestBroadcastLeadCommand());
            return Ok(result);
        }
    }
}
