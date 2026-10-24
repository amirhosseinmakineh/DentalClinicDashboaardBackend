using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Domain.DomainServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Entities;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Infrastracture.Migrations;
using DentalDashboard.LeadManagement.Contract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Abstractions;

namespace DentalDashboard.IntegrationTests;

public class ConsultationWorkflowTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Real_sql_http_import_dispatch_pickup_panels_and_browser_workflow()
    {
        var sql = Environment.GetEnvironmentVariable("CLINIC_TEST_SQL_CONNECTION")
            ?? throw new InvalidOperationException("CLINIC_TEST_SQL_CONNECTION must target a disposable loopback SQL Server.");
        var connection = new SqlConnectionStringBuilder(sql);
        if (!connection.DataSource.StartsWith("127.0.0.1,", StringComparison.Ordinal))
            throw new InvalidOperationException("Integration tests only allow loopback SQL Server.");
        connection.InitialCatalog = "ClinicWebsiteTests_" + Guid.NewGuid().ToString("N");
        await using var factory = new ClinicFactory(connection.ConnectionString);
        factory.UseKestrel(0);
        using var http = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DentalContext>();
        try
        {
            await db.Database.EnsureCreatedAsync();
            var service = new SecretarySaleService { Title = "خدمت تست یکپارچه", Price = 100, SecretaryReward = 1 };
            db.SecretarySaleServices.Add(service);
            var consultants = Enumerable.Range(1, 3).Select(i => new ConsultantProfile
            {
                User = new User { Id = Guid.NewGuid(), FirstName = "مشاور", LastName = "آزمایش", PhoneNumber = $"0911999999{i}", PasswordHash = "test-only", IsActive = true },
                NationalCode = $"000000000{i}", Address = "test", IsCompleteProfile = true, IsAvailable = true, IsOnline = true,
                ConsultantRole = (ConsultantRole)i, LimitNumber = 10,
            }).ToArray();
            consultants[2].LimitNumber = 0;
            db.ConsultantProfiles.AddRange(consultants);
            await db.SaveChangesAsync();

            // Execute just this additive migration against a baseline matching the
            // current pre-feature model, with an existing lead and service preserved.
            var historic = new LeadAssignment { UserName = "لید قبلی", PhoneNumber = "09120000001", SourceType = LeadSourceType.Clinic };
            db.LeadAssignments.Add(historic); await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("DROP TABLE LeadRequestedService; DROP INDEX IX_LeadAssignments_ConsultationRequestId ON LeadAssignments; ALTER TABLE LeadAssignments DROP COLUMN ConsultationDescription, ConsultationRequestHash, ConsultationRequestId;");
            var migration = new WebsiteConsultationRequests { ActiveProvider = "Microsoft.EntityFrameworkCore.SqlServer" };
            var generator = db.GetService<IMigrationsSqlGenerator>();
            foreach (var command in generator.Generate(migration.UpOperations))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
            db.ChangeTracker.Clear();
            Assert.Equal(LeadSourceType.Clinic, (await db.LeadAssignments.FindAsync(historic.Id))!.SourceType);
            output.WriteLine("PASS additive migration preserves existing lead origin and service catalog");

            var key = Guid.NewGuid();
            object Body(string phone = "+989123456789", long? serviceId = null) => new
            { fullName = "  علی   احمدی  ", phoneNumber = phone, serviceIds = new[] { serviceId ?? service.Id }, description = "توضیحات مراجعه‌کننده" };
            async Task<HttpResponseMessage> Post(object body, Guid? id = null)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/consultation-requests") { Content = JsonContent.Create(body) };
                request.Headers.Add("Idempotency-Key", (id ?? Guid.NewGuid()).ToString());
                return await http.SendAsync(request);
            }
            var catalog = await http.GetFromJsonAsync<JsonElement>("/api/consultation-requests/services");
            Assert.Contains(catalog.EnumerateArray(), item => item.GetProperty("id").GetInt64() == service.Id);
            Assert.DoesNotContain("price", catalog.GetRawText(), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(HttpStatusCode.OK, (await Post(Body(), key)).StatusCode);
            var website = await db.LeadAssignments.Include(x => x.RequestedServices).SingleAsync(x => x.ConsultationRequestId == key);
            Assert.Equal("علی احمدی", website.UserName); Assert.Equal("09123456789", website.PhoneNumber);
            Assert.Equal(LeadSourceType.Website, website.SourceType); Assert.Equal(LeadAssignmentState.New, website.LeadAssignmentState);
            Assert.Null(website.ConsultantProfileId); Assert.Single(website.RequestedServices);
            output.WriteLine("PASS public API saves normalized Website lead + services + notes to real SQL Server");
            Assert.Equal(HttpStatusCode.OK, (await Post(Body(), key)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await Post(Body("09120000002"), key)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await Post(Body())).StatusCode);
            Assert.Equal(1, await db.LeadAssignments.CountAsync(x => x.PhoneNumber == website.PhoneNumber));
            Assert.Equal(HttpStatusCode.BadRequest, (await Post(Body("bad-phone"))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Post(Body("09120000002", long.MaxValue))).StatusCode);
            output.WriteLine("PASS retry, reused key, duplicate phone, invalid phone and invalid service");

            var coordinator = scope.ServiceProvider.GetRequiredService<ILeadImportCoordinator>();
            var sheets = scope.ServiceProvider.GetRequiredService<IAdminLeadSheetService>();
            var sheet = await sheets.CreateSheetAsync("تست یکپارچه");
            await sheets.AddLeadAsync(sheet.Id, "09123456781", "لید", "ادمین");
            await coordinator.ProcessCycleAsync();
            Assert.Equal(LeadSourceType.Yektanet, (await db.LeadAssignments.SingleAsync(x => x.PhoneNumber == "09123456780")).SourceType);
            Assert.Equal(LeadSourceType.AdminSheet, (await db.LeadAssignments.SingleAsync(x => x.PhoneNumber == "09123456781")).SourceType);
            Assert.Equal(HttpStatusCode.Conflict, (await Post(Body("+989123456780"))).StatusCode);
            await coordinator.ProcessCycleAsync();
            Assert.Equal(1, await db.LeadAssignments.CountAsync(x => x.PhoneNumber == "09123456780"));
            output.WriteLine("PASS existing Yektanet workbook import + AdminSheet coordinator + cross-source duplicate history");

            // Simultaneous clicks/retries. Deadlock victims may return 500 and retry
            // the same key; there must never be two persisted identities.
            var concurrentKey = Guid.NewGuid();
            var concurrent = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Post(Body("09123456782"), concurrentKey)));
            Assert.Contains(concurrent, r => r.StatusCode == HttpStatusCode.OK);
            Assert.Equal(HttpStatusCode.OK, (await Post(Body("09123456782"), concurrentKey)).StatusCode);
            Assert.Equal(1, await db.LeadAssignments.CountAsync(x => x.PhoneNumber == "09123456782"));
            output.WriteLine("PASS simultaneous form retries persist one lead");

            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectWebsiteTest ON LeadAssignments AFTER INSERT AS IF EXISTS (SELECT 1 FROM inserted WHERE SourceType = 4) THROW 51001, 'Injected test failure', 1;");
            try
            {
                Assert.Equal(HttpStatusCode.InternalServerError, (await Post(Body("09123456783"))).StatusCode);
                Assert.False(await db.LeadAssignments.AnyAsync(x => x.PhoneNumber == "09123456783"));
            }
            finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectWebsiteTest"); }
            output.WriteLine("PASS database save failure never acknowledges success and rolls back graph");

            // Make this Website record the current candidate using the same queue
            // sort fields, then run all three actual allocation methods concurrently.
            await db.LeadAssignments.Where(x => x.Id != website.Id).ExecuteUpdateAsync(u => u.SetProperty(x => x.IsDeleted, true));
            var notifications = factory.Push;
            await Task.WhenAll(Enumerable.Range(0, 3).Select(async i =>
            {
                await using var workerScope = factory.Services.CreateAsyncScope();
                var allocator = workerScope.ServiceProvider.GetRequiredService<ILeadAssignmentService>();
                if (i == 0) await allocator.AssignLeadToTestConsultant();
                else if (i == 1) await allocator.AssignLeadToSellerConsultant();
                else await allocator.AssignLeadToTopSellertConsultant();
            }));
            Assert.Contains(website.Id.ToString(), notifications.LeadIds);
            Assert.DoesNotContain(consultants[2].User.Id, notifications.NotifiedUsers);
            Assert.Equal(DentalDashboard.ApplicationService.Contract.Responses.LeadResponse.PickupLeadStatus.DailyLimitReached,
                (await scope.ServiceProvider.GetRequiredService<IPickupService>().PickupLeadAsync(website.Id, consultants[2].Id, CancellationToken.None)).Status);
            output.WriteLine("PASS concurrent existing role workers broadcast Website and preserve consultant capacity limit");
            var pickups = await Task.WhenAll(consultants.Take(2).Select(async consultant =>
            {
                await using var pickupScope = factory.Services.CreateAsyncScope();
                return await pickupScope.ServiceProvider.GetRequiredService<IPickupService>()
                    .PickupLeadAsync(website.Id, consultant.Id, CancellationToken.None);
            }));
            Assert.Equal(1, pickups.Count(x => x.Status == DentalDashboard.ApplicationService.Contract.Responses.LeadResponse.PickupLeadStatus.Success));
            db.ChangeTracker.Clear(); website = await db.LeadAssignments.SingleAsync(x => x.Id == website.Id);
            var owner = consultants.Single(x => x.Id == website.ConsultantProfileId);
            Assert.Equal(LeadAssignmentState.Assigned, website.LeadAssignmentState); Assert.True(website.PickUp);
            Assert.NotNull(website.CallDeadlineAt);
            output.WriteLine("PASS concurrent pickup has one owner and existing three-minute deadline");

            http.DefaultRequestHeaders.Authorization = new("Bearer", factory.Token(owner.User.Id, "Consultant"));
            var consultantLeads = await http.GetFromJsonAsync<JsonElement>("/api/Consultant/GetLeads");
            var displayed = consultantLeads.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetInt64() == website.Id);
            Assert.Equal(4, displayed.GetProperty("sourceType").GetInt32());
            Assert.Equal("توضیحات مراجعه‌کننده", displayed.GetProperty("consultationDescription").GetString());
            Assert.Equal(service.Title, displayed.GetProperty("requestedServices")[0].GetProperty("title").GetString());
            http.DefaultRequestHeaders.Authorization = new("Bearer", factory.Token(owner.User.Id, "Admin"));
            var adminLeads = await http.GetFromJsonAsync<JsonElement>("/api/LeadAssignment?SourceType=4");
            Assert.All(adminLeads.GetProperty("items").EnumerateArray(), item => Assert.Equal(4, item.GetProperty("sourceType").GetInt32()));
            Assert.Contains(adminLeads.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetInt64() == website.Id);
            output.WriteLine("PASS existing authenticated consultant/admin APIs expose source, services, notes and source filter");

            // Optional cross-repository browser check, using the real API and SQL above.
            var browserScript = Environment.GetEnvironmentVariable("CLINIC_BROWSER_TEST_SCRIPT");
            if (!string.IsNullOrEmpty(browserScript))
            {
                var start = new ProcessStartInfo("node") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
                start.ArgumentList.Add(browserScript); start.Environment["CLINIC_TEST_API_URL"] = http.BaseAddress!.ToString();
                start.Environment["CLINIC_TEST_CONSULTANT_SESSION"] = JsonSerializer.Serialize(new
                {
                    token = factory.Token(owner.User.Id, "Consultant"),
                    user = new { firstName = "مشاور", lastName = "آزمایش", role = "consultant", roles = new[] { "consultant" }, roleName = "Consultant",
                        userId = owner.User.Id, profileId = owner.Id, consultantProfileId = owner.Id, isCompleteProfile = true }
                });
                start.Environment["CLINIC_TEST_ADMIN_SESSION"] = JsonSerializer.Serialize(new
                {
                    token = factory.Token(owner.User.Id, "Admin"),
                    user = new { firstName = "مدیر", lastName = "آزمایش", role = "admin", roles = new[] { "admin" }, roleName = "Admin",
                        userId = owner.User.Id, isCompleteProfile = true }
                });
                using var browser = Process.Start(start)!;
                var stdout = browser.StandardOutput.ReadToEndAsync(); var stderr = browser.StandardError.ReadToEndAsync();
                await browser.WaitForExitAsync(); output.WriteLine(await stdout); output.WriteLine(await stderr);
                Assert.Equal(0, browser.ExitCode);
                Assert.True(await db.LeadAssignments.AnyAsync(x => x.SourceType == LeadSourceType.Website && x.PhoneNumber == "09123456784"));
                output.WriteLine("PASS desktop/mobile browser submits to real API and SQL Server");
            }
            else output.WriteLine("Browser test not requested; set CLINIC_BROWSER_TEST_SCRIPT to run cross-repository E2E.");

            // Start an independent real host with the production-default rate limit.
            await using var limited = new ClinicFactory(connection.ConnectionString, 5);
            limited.UseKestrel(0);
            using var limitedHttp = limited.CreateClient();
            HttpStatusCode last = 0;
            for (var i = 0; i < 6; i++)
            {
                using var response = await limitedHttp.PostAsJsonAsync("/api/consultation-requests", Body("bad-phone")); last = response.StatusCode;
            }
            Assert.Equal(HttpStatusCode.TooManyRequests, last);
            output.WriteLine("PASS sixth public POST is rate limited");
        }
        finally
        {
            // The guard above and random database prefix prevent touching application databases.
            SqlConnection.ClearAllPools();
            await db.Database.EnsureDeletedAsync();
        }
    }
}

internal sealed class ClinicFactory : WebApplicationFactory<Program>
{
    internal readonly TestPush Push = new();
    private readonly string secret = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    internal ClinicFactory(string connection, int permitLimit = 1000)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connection);
        Environment.SetEnvironmentVariable("JwtSettings__SecretKey", secret);
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "clinic-integration");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "clinic-integration");
        Environment.SetEnvironmentVariable("ConsultationRequests__PermitLimit", permitLimit.ToString());
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IPushNotificationService>(); services.AddSingleton<IPushNotificationService>(Push);
            services.RemoveAll<ILeadDomainService>(); services.AddSingleton<ILeadDomainService, WorkingHoursFixture>();
            services.AddHttpClient<ILeadAssignmentService, LeadAssignmentService>()
                .ConfigurePrimaryHttpMessageHandler(() => new WorkbookFixture());
        });
    }
    internal string Token(Guid id, string role) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
        issuer: "clinic-integration", audience: "clinic-integration",
        claims: [new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Role, role)],
        expires: DateTime.UtcNow.AddHours(1), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256)));
}

internal sealed class TestPush : IPushNotificationService
{
    internal readonly System.Collections.Concurrent.ConcurrentBag<string> LeadIds = [];
    internal readonly System.Collections.Concurrent.ConcurrentBag<Guid> NotifiedUsers = [];
    public Task<bool> SendAsync(Guid userId, string title, string body, IReadOnlyDictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    { if (data?.TryGetValue("leadId", out var id) == true) { LeadIds.Add(id); NotifiedUsers.Add(userId); } return Task.FromResult(true); }
}
internal sealed class WorkingHoursFixture : ILeadDomainService
{
    private readonly LeadDomainService actual = new();
    public IEnumerable<LeadAssignment> GetNewLeads(IEnumerable<LeadAssignment> oldNumbers, IEnumerable<LeadAssignment> newNumbers) => actual.GetNewLeads(oldNumbers, newNumbers);
    public bool IsWorkingTime(DateTime now) => true;
    public bool IsNightTime(DateTime now) => false;
    public bool IsAfterWorkEnd(DateTime now) => false;
}
internal sealed class WorkbookFixture : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(); var sheet = workbook.AddWorksheet("Leads");
        sheet.Cell(1, 1).Value = "نام و نام خانوادگی"; sheet.Cell(1, 2).Value = "شماره تماس";
        sheet.Cell(2, 1).Value = "لید یکتانت"; sheet.Cell(2, 2).Value = "09123456780";
        using var stream = new MemoryStream(); workbook.SaveAs(stream);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(stream.ToArray()) });
    }
}
