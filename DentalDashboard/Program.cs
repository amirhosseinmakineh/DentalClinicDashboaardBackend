using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.CreateUser;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.DeleteUser;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.UpddateUser;
using DentalDashboard.ApplicationService.Contract.Requests.User.Queries.User;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.Users;
using DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth;
using DentalDashboard.ApplicationService.Handlers.CommandHandlers.User;
using DentalDashboard.ApplicationService.Handlers.QueryHandlers.User;
using DentalDashboard.ApplicationService.Services;
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
builder.Services.AddTransient<ICommandHandler<CreateUserCommand,object>,CreateUserCommandHandler>();
builder.Services.AddTransient<ICommandHandler<UpdateUserCommand>,UpdateUserCommandHandler>();
builder.Services.AddTransient<ICommandHandler<DeleteUserCommand,object>,DeleteUserCommandHandler>();
builder.Services.AddTransient<IQueryHandler<GetUsersQuery, PaginatedResult<UserItemResponse>>,GetUsersQueryHandler>();
builder.Services.AddTransient<ITokenGenerator,TokenGenerator>();
builder.Services.AddScoped<IQueryDispatcher, QueryDispatcher>();

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