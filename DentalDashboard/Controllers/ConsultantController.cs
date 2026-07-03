using DentalDashboard.ApplicationService.Contract.Requests.Consultant;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> CreateConsultantPatientLead(CreateConsultantPatientLeadCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }
    }
}
