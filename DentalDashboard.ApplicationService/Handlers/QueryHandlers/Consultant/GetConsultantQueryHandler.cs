using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.Attendance;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.ApplicationService.Contract.Responses.ScoreLogResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Consultant
{
    public class GetConsultantQueryHandler : IQueryHandler<GetConsultantQuery, PaginatedResult<ConsultantResponse>>
    {
        private readonly IUserRepository userRepository;
        private readonly IConsultantProfileRepository consultantProfileRepository;

        public GetConsultantQueryHandler(IUserRepository userRepository, IConsultantProfileRepository consultantProfileRepository)
        {
            this.userRepository = userRepository;
            this.consultantProfileRepository = consultantProfileRepository;
        }

        public async Task<PaginatedResult<ConsultantResponse>> HandleAsync(
            GetConsultantQuery query,
            CancellationToken cancellationToken = default)
        {
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            var consultants = userRepository.GetAll()
                .Include(x => x.ConsultantProfile)
                    .ThenInclude(cp => cp.Attendances)
                .Include(x => x.ConsultantProfile)
                    .ThenInclude(cp => cp.CallAssignments)
                .Include(x => x.ConsultantProfile)
                    .ThenInclude(cp => cp.ScoreLogs)
                .Include(x => x.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(x =>
                    x.IsActive &&
                    x.IsCompleteProfile &&
                    x.ConsultantProfile != null &&
                    x.UserRoles.Any(ur => ur.Role.RoleName == "Consultant"));
            if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
                consultants = consultants.Where(x => x.PhoneNumber == query.PhoneNumber);

            if (!string.IsNullOrWhiteSpace(query.FirstName))
                consultants = consultants.Where(x => x.FirstName.Contains(query.FirstName));
            if (!string.IsNullOrWhiteSpace(query.LastName))
                consultants = consultants.Where(x => x.LastName.Contains(query.LastName));
            if (query.AttendanceDate.HasValue)
                consultants = consultants.Where(x =>
                    x.ConsultantProfile.Attendances
                        .Any(a => a.AttendanceDate == DateOnly.FromDateTime(query.AttendanceDate.Value)));

            if (query.CheckInTime.HasValue)
                consultants = consultants.Where(x =>
                    x.ConsultantProfile.Attendances
                        .Any(a => a.CheckInTime == query.CheckInTime.Value));

            if (query.CheckOutTime.HasValue)
                consultants = consultants.Where(x =>
                    x.ConsultantProfile.Attendances
                        .Any(a => a.CheckOutTime == query.CheckOutTime.Value));

            if (query.Status.HasValue)
                consultants = consultants.Where(x =>
                    x.ConsultantProfile.Attendances
                        .Any(a => a.Status == query.Status.Value));
            if (query.LeadAssignmentState.HasValue)
                consultants = consultants.Where(x =>
                    x.ConsultantProfile.CallAssignments
                        .Any(la => la.LeadAssignmentState == query.LeadAssignmentState.Value));

            if (query.leadAssignmentType.HasValue)
                consultants = consultants.Where(x =>
                    x.ConsultantProfile.CallAssignments
                        .Any(la => la.AssignmentType == query.leadAssignmentType.Value));
            if (query.ScoreType.HasValue || query.ScoreValue != 0)
            {
                consultants = consultants.Where(x =>
                    x.ConsultantProfile.ScoreLogs
                        .Any(s =>
                            (!query.ScoreType.HasValue || s.ScoreType == query.ScoreType.Value) &&
                            (query.ScoreValue == 0 || s.ScoreValue == query.ScoreValue)
                        ));
            }

            consultants = consultants.OrderByDescending(x =>
                x.ConsultantProfile.ScoreLogs.Sum(s => s.ScoreValue));
            var totalCount = await consultants.CountAsync(cancellationToken);

            var result = await consultants
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ConsultantResponse
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    PhoneNumber = x.PhoneNumber,
                    ProfileId = x.ConsultantProfile.Id,

                    AttendanceResponse = new PaginatedResult<AttendanceResponse>
                    {
                        Items = x.ConsultantProfile.Attendances
                            .OrderByDescending(a => a.AttendanceDate)
                            .ThenByDescending(a => a.CheckInTime)
                            .Select(a => new AttendanceResponse
                            {
                                Id = a.Id,
                                AttendanceDate = a.AttendanceDate,
                                CheckInTime = a.CheckInTime,
                                CheckOutTime = a.CheckOutTime,
                                Status = a.Status,
                                Description = a.Description
                            }).ToList(),
                        TotalCount = x.ConsultantProfile.Attendances.Count
                    },

                    ScoreLogResponse = new PaginatedResult<ScoreLogResponse>
                    {
                        Items = x.ConsultantProfile.ScoreLogs
                            .OrderByDescending(s => s.Id)
                            .Select(s => new ScoreLogResponse
                            {
                                Id = s.Id,
                                ScoreType = s.ScoreType,
                                ScoreValue = s.ScoreValue,
                                Description = s.Description
                            }).ToList(),
                        TotalCount = x.ConsultantProfile.ScoreLogs.Count
                    },

                    LeadsAssignmentItemsResponse = new PaginatedResult<LeadsAssignmentItemsResponse>
                    {
                        Items = x.ConsultantProfile.CallAssignments
                            .OrderByDescending(l => l.Id)
                            .Select(l => new LeadsAssignmentItemsResponse
                            {
                                Id = l.Id,
                                LeadAssignmentState = l.LeadAssignmentState,
                                leadAssignmentType = l.AssignmentType,
                            }).ToList(),
                        TotalCount = x.ConsultantProfile.CallAssignments.Count
                    }
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<ConsultantResponse>
            {
                Items = result,
                TotalCount = totalCount
            };
        }
    }
}
