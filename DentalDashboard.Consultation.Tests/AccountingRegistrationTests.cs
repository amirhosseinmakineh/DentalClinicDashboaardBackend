using DentalDashboard.ApplicationService;
using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Infrastracture.Registration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class AccountingRegistrationTests
{
    [Fact]
    public void AccountingHandlersResolveThroughTheExistingApplicationAndInfrastructureRegistrations()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=not_connected;Integrated Security=true;TrustServerCertificate=true"
        }).Build());
        services.AddScoped<IAccountingConsultantRewards, Rewards>();
        services.AddScoped<IFileStorageService, Storage>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandHandler<AccountingOperationCommand, AccountingOperationResponse>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandHandler<SetAccountantPermissionsCommand, AccountingOperationResponse>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IQueryHandler<AccountingOverviewQuery, AccountingOverview>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IQueryHandler<AccountingExportQuery, AccountingExportResponse>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IQueryHandler<AccountingReceiptQuery, AccountingFileResponse?>>());
    }
    private sealed class Rewards : IAccountingConsultantRewards
    {
        public Task<AccountingOperationResponse> ReviewAsync(long reservationId, Guid actor, bool approved, string? reason, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class Storage : IFileStorageService
    {
        public Task<string> SaveAsync(Stream stream, string extension, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Delete(string? relativePath) => throw new NotSupportedException();
        public Task<string> SavePatientDocumentAsync(Stream stream, string extension, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Stream? OpenPatientDocument(string path) => throw new NotSupportedException();
        public void DeletePatientDocument(string? path) => throw new NotSupportedException();
    }
}
