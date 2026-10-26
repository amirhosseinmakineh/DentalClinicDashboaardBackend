using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Infrastracture.Services;
using Microsoft.Extensions.Logging;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Domain.DomainServices;
using DentalDashboard.Domain.IDomainService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Json;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using DentalDashboard.Controllers;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Entities;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Infrastracture.Repository;
using DentalDashboard.LeadManagement.Application;
using DentalDashboard.ApplicationService.Handlers.QueryHandlers.Lead;
using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

// Each run owns a disposable database. Never accepts an arbitrary database name.
public sealed class ConsultationTests : IAsyncLifetime
{
    private string connection = "";
    private long serviceId;
    private DentalContext Db() => new(new DbContextOptionsBuilder<DentalContext>().UseSqlServer(connection).Options);
    private ConsultationRequestsController Controller(DentalContext db) => new(db, new LeadAssignmentRepository(db));
    private ConsultationRequest Request(string phone = "09123456789") => new("امیر حسین", phone, serviceId, "مشاوره اولیه", Guid.NewGuid());
    public async Task InitializeAsync()
    {
        var source = Environment.GetEnvironmentVariable("CONSULTATION_TEST_SQL")
            ?? throw new InvalidOperationException("Set CONSULTATION_TEST_SQL to an isolated SQL Server test instance.");
        var builder = new SqlConnectionStringBuilder(source) { InitialCatalog = "ConsultationTests_" + Guid.NewGuid().ToString("N") };
        connection = builder.ConnectionString;
        await using var db = Db();
        await db.Database.EnsureCreatedAsync();
        var service = new SecretarySaleService { Title = "خدمت آزمایشی", IsActive = true };
        db.SecretarySaleServices.Add(service);
        await db.SaveChangesAsync();
        serviceId = service.Id;
    }
    public async Task DisposeAsync() { await using var db = Db(); await db.Database.EnsureDeletedAsync(); }

    [Fact]
    public async Task WebsitePersistsRealLeadWithServiceAndCanBeReadByExistingPanelQueries()
    {
        await using var db = Db();
        Assert.IsType<OkObjectResult>(await Controller(db).Create(Request("+98 912-345-6789"), default));
        db.ChangeTracker.Clear();
        var lead = await db.LeadAssignments.SingleAsync();
        Assert.Equal("09123456789", lead.PhoneNumber);
        Assert.Equal(LeadSourceType.Website, lead.SourceType);
        Assert.Equal(LeadAssignmentState.New, lead.LeadAssignmentState);
        Assert.Null(lead.ConsultantProfileId);
        Assert.Equal(serviceId, lead.ConsultationServiceId);
        Assert.Equal("مشاوره اولیه", lead.ConsultationNotes);
        var repository = new LeadAssignmentRepository(db);
        Assert.Equal(lead.Id, (await repository.GetCurrentRealtimeLeadForDispatchAsync(TimeSpan.Zero, LeadSourceType.Yektanet))?.Id);
        var handler = new GetAllLeadsAssignmentQueryHandler(repository, new ReservationRepository(db));
        var result = await handler.HandleAsync(new GetAllLeadsQuery { SourceType = LeadSourceType.Website });
        Assert.Equal("خدمت آزمایشی", Assert.Single(result.Items).ConsultationServiceTitle);
        Assert.Empty((await handler.HandleAsync(new GetAllLeadsQuery { SourceType = LeadSourceType.AdminSheet })).Items);
    }

    [Fact]
    public async Task RetryDoesNotInsertTwiceAndDifferentPayloadCannotReuseKey()
    {
        var request = Request();
        await using var db = Db();
        Assert.IsType<OkObjectResult>(await Controller(db).Create(request, default));
        Assert.IsType<OkObjectResult>(await Controller(db).Create(request, default));
        Assert.IsType<ConflictObjectResult>(await Controller(db).Create(request with { Notes = "تغییر" }, default));
        Assert.IsType<ConflictObjectResult>(await Controller(db).Create(Request(), default));
        Assert.Equal(1, await db.LeadAssignments.CountAsync());
    }

    [Theory]
    [InlineData("bad", 0)]
    [InlineData("09123456789", -1)]
    public async Task InvalidPhoneOrServiceCannotPersist(string phone, int invalidService)
    {
        await using var db = Db();
        var request = Request(phone);
        if (invalidService != 0) request = request with { ServiceId = invalidService };
        Assert.IsType<BadRequestObjectResult>(await Controller(db).Create(request, default));
        Assert.Empty(await db.LeadAssignments.ToListAsync());
    }

    [Fact]
    public async Task YektanetAndAdminSheetRemainCompatibleAndWebsiteRejectsExistingPhone()
    {
        await using var db = Db();
        var repo = new LeadAssignmentRepository(db);
        var lead = new LeadAssignment { UserName = "یکتانت", PhoneNumber = "09123456789", SourceType = LeadSourceType.Yektanet,
            LeadAssignmentState = LeadAssignmentState.New, AssignmentType = LeadAssignmentType.RealTime, CreatedAt = DateTime.Now };
        Assert.Equal(1, await repo.AddNewLeadsIfPhoneDoesNotExistAsync([lead]));
        Assert.IsType<ConflictObjectResult>(await Controller(db).Create(Request(), default));
        var sheets = new AdminLeadSheetService(db);
        var sheet = await sheets.CreateSheetAsync("آزمایش");
        await sheets.AddLeadAsync(sheet.Id, "09123456780", "نام", "خانوادگی");
        Assert.Equal(1, await sheets.ProcessPendingAsync());
        Assert.Equal(LeadSourceType.AdminSheet, (await db.LeadAssignments.SingleAsync(x => x.PhoneNumber == "09123456780")).SourceType);
        Assert.Equal(2, await db.LeadAssignments.CountAsync());
    }

    [Fact]
    public async Task ConcurrentPickupsHaveOnlyOneWinnerAndConsultantPanelContainsRequestMetadata()
    {
        long id;
        long[] consultants;
        await using (var db = Db())
        {
            Assert.IsType<OkObjectResult>(await Controller(db).Create(Request(), default));
            id = (await db.LeadAssignments.SingleAsync()).Id;
            for (int i = 0; i < 2; i++) db.ConsultantProfiles.Add(new ConsultantProfile {
                NationalCode = "000000000" + i, Address = "test", IsCompleteProfile = true,
                User = new User { Id = Guid.NewGuid(), FirstName = "Test", LastName = "Consultant", PhoneNumber = "0900000000" + i, PasswordHash = "test-only", BirthDate = new DateTime(1990, 1, 1) }
            });
            await db.SaveChangesAsync();
            consultants = await db.ConsultantProfiles.Select(x => x.Id).ToArrayAsync();
        }
        var winners = await Task.WhenAll(consultants.Select(async consultant => {
            await using var db = Db();
            return await new LeadAssignmentRepository(db).TryPickupLeadAsync(id, consultant, default);
        }));
        Assert.Equal(1, winners.Count(x => x));
        await using var read = Db();
        var lead = await read.LeadAssignments.SingleAsync();
        var handler = new GetLeadsAssignmentQueryHandler(new LeadAssignmentRepository(read), new ReservationRepository(read));
        var response = await handler.HandleAsync(new GetLeadsQuery { ProfileId = lead.ConsultantProfileId!.Value });
        Assert.Equal("مشاوره اولیه", Assert.Single(response.Items).ConsultationNotes);
    }


    private static WebPushNotificationService Push(DentalContext db) => new(new PushSubscriptionRepository(db),
        new ConfigurationBuilder().Build(), NullLogger<WebPushNotificationService>.Instance);
    private static LeadAssignmentService AssignmentService(DentalContext db)
    {
        var leads = new LeadAssignmentRepository(db);
        var consultants = new ConsultantProfileRepository(db);
        var settings = new LeadAssignmentSettingRepository(db);
        return new LeadAssignmentService(new HttpClient(new YektanetWorkbook()), leads, new TestWorkingWindow(), consultants,
            new LeadAssignmentLimitService(leads, consultants), Push(db), new ServiceLogRepository(db),
            new LeadAssignmentCandidateProvider(leads, settings, consultants), settings);
    }
    // The production work window is unchanged; the fixture runs daytime rules at any CI time.
    private sealed class TestWorkingWindow : ILeadDomainService
    {
        private readonly LeadDomainService real = new();
        public bool IsWorkingTime(DateTime now) => true;
        public bool IsNightTime(DateTime now) => false;
        public bool IsAfterWorkEnd(DateTime now) => false;
        public IEnumerable<LeadAssignment> GetNewLeads(IEnumerable<LeadAssignment> oldNumbers, IEnumerable<LeadAssignment> newNumbers) => real.GetNewLeads(oldNumbers, newNumbers);
    }
    private sealed class YektanetWorkbook : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            using var book = new XLWorkbook();
            var sheet = book.AddWorksheet("Leads");
            sheet.Cell(1, 1).Value = "نام و نام خانوادگی"; sheet.Cell(1, 2).Value = "موبایل";
            sheet.Cell(2, 1).Value = "تست یکتانت"; sheet.Cell(2, 2).Value = "09123456781";
            using var stream = new MemoryStream(); book.SaveAs(stream);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(stream.ToArray()) });
        }
    }
    private async Task<long[]> SeedConsultants()
    {
        await using var db = Db();
        foreach (var role in Enum.GetValues<ConsultantRole>())
            db.ConsultantProfiles.Add(new ConsultantProfile { NationalCode = Guid.NewGuid().ToString("N"), Address = "test", IsCompleteProfile = true,
                IsOnline = true, IsAvailable = true, ConsultantRole = role,
                User = new User { Id = Guid.NewGuid(), FirstName = "Test", LastName = role.ToString(), PhoneNumber = "090000000" + ((int)role).ToString("00"), PasswordHash = "test-only", BirthDate = new DateTime(1990, 1, 1) } });
        await db.SaveChangesAsync();
        return await db.ConsultantProfiles.Select(x => x.Id).ToArrayAsync();
    }
    [Fact]
    public async Task RealYektanetImporterAndCoordinatorPersistBothSources()
    {
        await using var db = Db();
        var sheets = new AdminLeadSheetService(db);
        var sheet = await sheets.CreateSheetAsync("fixture");
        await sheets.AddLeadAsync(sheet.Id, "09123456782", "تست", "ادمین");
        var coordinator = new LeadImportCoordinator(AssignmentService(db), sheets);
        await coordinator.ProcessCycleAsync();
        Assert.Equal(2, await db.LeadAssignments.CountAsync());
        Assert.Equal(LeadSourceType.Yektanet, (await db.LeadAssignments.SingleAsync(x => x.PhoneNumber == "09123456781")).SourceType);
        Assert.Equal(LeadSourceType.AdminSheet, (await db.LeadAssignments.SingleAsync(x => x.PhoneNumber == "09123456782")).SourceType);
    }
    [Fact]
    public async Task ConcurrentBackgroundStepsBroadcastWebsiteThroughExistingServiceThenOnlyOnePickupWins()
    {
        await using (var db = Db()) Assert.IsType<OkObjectResult>(await Controller(db).Create(Request(), default));
        var consultants = await SeedConsultants();
        await Task.WhenAll(Enumerable.Range(0, 3).Select(async step => {
            await using var db = Db();
            var service = AssignmentService(db);
            if (step == 0) await service.AssignLeadToTestConsultant();
            else if (step == 1) await service.AssignLeadToSellerConsultant();
            else await service.AssignLeadToTopSellertConsultant();
        }));
        long leadId;
        await using (var db = Db()) {
            var lead = await db.LeadAssignments.SingleAsync();
            Assert.True(lead.NotificationSent); Assert.NotNull(lead.LastDispatchAt);
            Assert.Null(lead.ConsultantProfileId); leadId = lead.Id;
        }
        var results = await Task.WhenAll(consultants.Select(async consultant => {
            await using var db = Db();
            var leads = new LeadAssignmentRepository(db); var profiles = new ConsultantProfileRepository(db);
            return await new PickUpService(leads, profiles, new LeadAssignmentLimitService(leads, profiles), Push(db), new UnitOfWork(db))
                .PickupLeadAsync(leadId, consultant, default);
        }));
        Assert.Equal(1, results.Count(x => x.Status == DentalDashboard.ApplicationService.Contract.Responses.LeadResponse.PickupLeadStatus.Success));
    }


    [Fact]
    public async Task ExistingCapacityLimitStillBlocksWebsitePickup()
    {
        var consultantId = (await SeedConsultants())[0];
        await using var db = Db();
        var profile = await db.ConsultantProfiles.FindAsync(consultantId);
        profile!.LimitNumber = 0; await db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>(await Controller(db).Create(Request(), default));
        var lead = await db.LeadAssignments.SingleAsync();
        var leads = new LeadAssignmentRepository(db); var profiles = new ConsultantProfileRepository(db);
        var result = await new PickUpService(leads, profiles, new LeadAssignmentLimitService(leads, profiles), Push(db), new UnitOfWork(db))
            .PickupLeadAsync(lead.Id, consultantId, default);
        Assert.Equal(DentalDashboard.ApplicationService.Contract.Responses.LeadResponse.PickupLeadStatus.DailyLimitReached, result.Status);
        db.ChangeTracker.Clear(); Assert.Null((await db.LeadAssignments.SingleAsync()).ConsultantProfileId);
    }

    private async Task<WebApplication> StartHttpApp(bool staticFiles = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(ConsultationRequestsController).Assembly);
        builder.Services.AddAuthorization();
        builder.Services.AddAuthentication("ConsultationTest").AddScheme<AuthenticationSchemeOptions, ConsultationTestAuthentication>("ConsultationTest", _ => { });
        builder.Services.AddScoped<IQueryDispatcher, QueryDispatcher>();
        builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        builder.Services.AddScoped<DentalDashboard.ApplicationService.Contract.IServices.ISecretaryAccessService, SecretaryAccessService>();
        builder.Services.AddScoped<DentalDashboard.Domain.IRepositories.IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<DentalDashboard.Domain.IRepositories.IConsultantProfileRepository, ConsultantProfileRepository>();
        builder.Services.AddScoped<DentalDashboard.Domain.IRepositories.IReservationRepository, ReservationRepository>();
        builder.Services.AddScoped<IQueryHandler<GetNewLeadsQuery, DentalDashboard.ApplicationService.Contract.Responses.PaginatedResult<LeadsAssignmentItemsResponse>>, GetNewLeadsQueryHandler>();
        builder.Services.AddScoped<IQueryHandler<GetLeadsQuery, DentalDashboard.ApplicationService.Contract.Responses.PaginatedResult<LeadsAssignmentItemsResponse>>, GetLeadsAssignmentQueryHandler>();
        builder.Services.AddConsultationRateLimiting();
        builder.Services.AddDbContext<DentalContext>(options => options.UseSqlServer(connection));
        builder.Services.AddScoped<DentalDashboard.Domain.IRepositories.ILeadAssignmentRepository, LeadAssignmentRepository>();
        var app = builder.Build();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        if (staticFiles)
        {
            // Test-host-only setup: exercise the existing allocation and pickup services.
            // These routes and the authentication scheme are never in the application.
            app.MapPost("/test/pickup/{phone}", async (string phone) =>
            {
                await using var db = Db();
                var lead = await db.LeadAssignments.SingleAsync(x => x.PhoneNumber == phone);
                await AssignmentService(db).AssignRealTimeLeadsAsync();
                var profile = await db.ConsultantProfiles.Include(x => x.User)
                    .FirstAsync(x => !db.LeadAssignments.Any(l => l.ConsultantProfileId == x.Id));
                var leads = new LeadAssignmentRepository(db); var profiles = new ConsultantProfileRepository(db);
                var pickup = await new PickUpService(leads, profiles, new LeadAssignmentLimitService(leads, profiles), Push(db), new UnitOfWork(db))
                    .PickupLeadAsync(lead.Id, profile.Id, default);
                Assert.Equal(PickupLeadStatus.Success, pickup.Status);
                return Results.Ok(new { profileId = profile.Id, userId = profile.UserId, leadId = lead.Id });
            });
            var root = Environment.GetEnvironmentVariable("CONSULTATION_FRONTEND_DIST")!;
            app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(root) });
            var index = File.Exists(Path.Combine(root, "index.csr.html")) ? "index.csr.html" : "index.html";
            app.MapFallback(async ctx => await ctx.Response.SendFileAsync(Path.Combine(root, index)));
        }
        await app.StartAsync();
        return app;
    }
    private static string Address(WebApplication app) => app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();

    [Fact]
    public async Task PublicHttpEndpointCommitsAndRateLimits()
    {
        await using var app = await StartHttpApp();
        using var client = new HttpClient { BaseAddress = new Uri(Address(app)) };
        var services = await client.GetAsync("/api/consultation-requests/services");
        Assert.Equal(HttpStatusCode.OK, services.StatusCode);
        var request = Request();
        for (int i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/consultation-requests", request)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/consultation-requests", request)).StatusCode);
        await using var db = Db();
        Assert.Equal(1, await db.LeadAssignments.CountAsync());
    }

    [Fact]
    public async Task SaveFailureNeverReturnsSuccessOrPersistsLead()
    {
        await using var db = new DentalContext(new DbContextOptionsBuilder<DentalContext>().UseSqlServer(connection).AddInterceptors(new FailSave()).Options);
        var result = Assert.IsType<ObjectResult>(await Controller(db).Create(Request(), default));
        Assert.Equal(503, result.StatusCode);
        await using var read = Db();
        Assert.Empty(await read.LeadAssignments.ToListAsync());
    }
    private sealed class FailSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken ct = default) => throw new DbUpdateException("Synthetic database failure");
    }

    [Fact]
    public async Task AdditiveMigrationPreservesExistingLeadAndOrigin()
    {
        await using var db = Db();
        var old = new LeadAssignment { UserName = "قدیمی", PhoneNumber = "09123456780", CreatedAt = DateTime.Now, SourceType = LeadSourceType.AdminSheet };
        db.Add(old); await db.SaveChangesAsync();
        var migration = new DentalDashboard.Infrastracture.Migrations.AddWebsiteConsultationMetadata();
        var generator = db.GetService<IMigrationsSqlGenerator>();
        foreach (var sql in generator.Generate(migration.DownOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(sql.CommandText);
        foreach (var sql in generator.Generate(migration.UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(sql.CommandText);
        db.ChangeTracker.Clear();
        var preserved = await db.LeadAssignments.SingleAsync();
        Assert.Equal(old.Id, preserved.Id);
        Assert.Equal(LeadSourceType.AdminSheet, preserved.SourceType);
        Assert.IsType<OkObjectResult>(await Controller(db).Create(Request(), default));
    }

    [BrowserFact]
    public async Task BrowserSubmitsRealFormOnDesktopAndMobile()
    {
        await SeedConsultants();
        await using var app = await StartHttpApp(true);
        var script = Environment.GetEnvironmentVariable("CONSULTATION_BROWSER_SCRIPT")!;
        var start = new ProcessStartInfo("node") { RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(script); start.ArgumentList.Add(Address(app));
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.True(process.ExitCode == 0, await output + await error);
        await using var db = Db();
        Assert.Equal(2, await db.LeadAssignments.CountAsync(x => x.SourceType == LeadSourceType.Website));
        Assert.All(await db.LeadAssignments.ToListAsync(), x => Assert.Equal(serviceId, x.ConsultationServiceId));
        var created = await db.LeadAssignments.ToListAsync();
        Assert.All(created, lead => Assert.NotNull(lead.ConsultantProfileId));
        Assert.Equal(2, created.Select(lead => lead.ConsultantProfileId).Distinct().Count());
    }
}

public sealed class BrowserFactAttribute : FactAttribute
{
    public BrowserFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("CONSULTATION_FRONTEND_DIST") == null)
            Skip = "Set CONSULTATION_FRONTEND_DIST and CONSULTATION_BROWSER_SCRIPT to run real browser tests.";
    }
}

// Isolated test host only. Actual controller role/ownership checks still execute.
public sealed class ConsultationTestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Guid.TryParse(Request.Headers["X-Consultation-Test-User"], out var userId))
            return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, "Consultant")], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
