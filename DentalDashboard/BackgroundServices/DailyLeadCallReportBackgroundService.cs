using DentalDashboard.Services;

namespace DentalDashboard.BackgroundServices;

public class DailyLeadCallReportBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<DailyLeadCallReportBackgroundService> logger;
    private readonly string reportsDirectory;

    public DailyLeadCallReportBackgroundService(IServiceScopeFactory scopeFactory, IWebHostEnvironment environment, ILogger<DailyLeadCallReportBackgroundService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
        reportsDirectory = Path.Combine(environment.ContentRootPath, "Reports", "LeadCallReports");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(reportsDirectory);
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(DateTime.Now);
            await Task.Delay(delay, stoppingToken);
            await GenerateYesterdayReportAsync(stoppingToken);
        }
    }

    private async Task GenerateYesterdayReportAsync(CancellationToken cancellationToken)
    {
        try
        {
            var reportDay = DateTime.Today.AddDays(-1);
            await using var scope = scopeFactory.CreateAsyncScope();
            var exportService = scope.ServiceProvider.GetRequiredService<LeadCallReportExportService>();
            var file = await exportService.ExportCsvAsync(reportDay, reportDay.AddDays(1), cancellationToken);
            var filePath = Path.Combine(reportsDirectory, $"lead-call-reports-{reportDay:yyyyMMdd}.csv");
            await File.WriteAllBytesAsync(filePath, file, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate daily lead call report.");
        }
    }

    private static TimeSpan GetDelayUntilNextRun(DateTime now)
    {
        var nextRun = now.Date.AddHours(22);
        if (now >= nextRun)
            nextRun = nextRun.AddDays(1);
        return nextRun - now;
    }
}
