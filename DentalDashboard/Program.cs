using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Attendance.Queryies;
using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using DentalDashboard.ApplicationService.Contract.Requests.Role;
using DentalDashboard.ApplicationService.Contract.Requests.Role.Queries;
using DentalDashboard.ApplicationService.Contract.Requests.ScoreLog.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.CreateUser;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.DeleteUser;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.UpddateUser;
using DentalDashboard.ApplicationService.Contract.Requests.User.Queries.User;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.Attendance;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.ApplicationService.Contract.Responses.RoleResponse;
using DentalDashboard.ApplicationService.Contract.Responses.User;
using DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth;
using DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;
using DentalDashboard.ApplicationService.Handlers.CommandHandlers.Role;
using DentalDashboard.ApplicationService.Handlers.CommandHandlers.ScoreLog;
using DentalDashboard.ApplicationService.Handlers.CommandHandlers.User;
using DentalDashboard.ApplicationService.Handlers.QueryHandlers.Attendance;
using DentalDashboard.ApplicationService.Handlers.QueryHandlers.Consultant;
using DentalDashboard.ApplicationService.Handlers.QueryHandlers.Lead;
using DentalDashboard.ApplicationService.Handlers.QueryHandlers.Role;
using DentalDashboard.ApplicationService.Handlers.QueryHandlers.User;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.BackgroundServices;
using DentalDashboard.Domain.DomainServices;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Infrastracture.Registration;
using DentalDashboard.Security.Generator;
var builder = WebApplication.CreateBuilder(args);

// ====================================
// Services
// ====================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddTransient<ICommandDispatcher, CommandDispatcher>();
builder.Services.AddTransient<ICommandHandler<RegisterCommand,object>,RegisterCommandHandler>();
builder.Services.AddTransient<ICommandHandler<LoginCommand,object>,LoginCommandHandler>();
builder.Services.AddTransient<ICommandHandler<CreateUserCommand,CreateUserResponse>,CreateUserCommandHandler>();
builder.Services.AddTransient<ICommandHandler<UpdateUserCommand,UpdateUserResponse>,UpdateUserCommandHandler>();
builder.Services.AddTransient<ICommandHandler<DeleteUserCommand,object>,DeleteUserCommandHandler>();
builder.Services.AddTransient<ICommandHandler<CreateRoleCommand,CreateRoleResponse>,CreateRoleCommandHandler>();
builder.Services.AddTransient<ICommandHandler<UpdateRoleCommaand,UpdateRoleResponse>,UpdateRoleCommandHandler>();
builder.Services.AddTransient<ICommandHandler<DeleteRoleCommaand>,DeleteRoleCommandHnadler>();
builder.Services.AddTransient<IQueryHandler<GetUsersQuery, PaginatedResult<UserItemResponse>>,GetUsersQueryHandler>();
builder.Services.AddTransient<ICommandHandler<SetOnlineOfflineCommand>,SetOnlineOfflineCommandHandler>();
builder.Services.AddTransient<ICommandHandler<SetAvailableCommand>,SetAvailableCommandHandler>();
builder.Services.AddTransient<ICommandHandler<SubmitLeadCallReportCommand>, SubmitLeadCallReportCommandHandler>();
builder.Services.AddTransient<IQueryHandler<GetRolesQuery, PaginatedResult<RoleItemsResponse>>,GetRolesQueryHandler>();
builder.Services.AddTransient<IQueryHandler<GetConsultantQuery, PaginatedResult<ConsultantResponse>>,GetConsultantQueryHandler>();
builder.Services.AddTransient<IQueryHandler<GetAttendancesQuery, PaginatedResult<AttendanceResponse>>,GetConsultantAttendanceCommandHandler>();
builder.Services.AddTransient<ITokenGenerator,TokenGenerator>();
builder.Services.AddScoped<IQueryDispatcher, QueryDispatcher>();
builder.Services.AddHttpClient<ILeadAssignmentService, LeadAssignmentService>();
builder.Services.AddScoped<ILeadAssignmentStrategy,ScoreBasedRoundRobinLeadAssignmentStrategy>();
builder.Services.AddScoped<IOfflineLeadAssignmentStrategy,OfflineLeadAssignmentStrategy>();
builder.Services.AddScoped<IConsultantProfileService,ConsultantProfileService>();
builder.Services.AddHostedService<LeadAssignmentBackgroundService>();
builder.Services.AddTransient<ICommandHandler<CompleteConsultantProfileCommand,long>, CompleteConsaltantProfileHandler>();
builder.Services.AddTransient<IQueryHandler<GetLeadsQuery, PaginatedResult<LeadsAssignmentItemsResponse>>,GetLeadsAssignmentQueryHandler>();
builder.Services.AddScoped<ILeadDomainService, LeadDomainService>();
builder.Services.AddScoped<ILeadReportDomainService, LeadReportDomainService>();
builder.Services.AddInfrastructure(
    builder.Configuration);

// ====================================
// App
// ====================================

var app = builder.Build();

// ====================================
// Middleware
// ====================================

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();