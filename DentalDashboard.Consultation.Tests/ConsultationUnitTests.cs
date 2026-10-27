using DentalDashboard.Controllers;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Infrastracture.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class ConsultationUnitTests
{
    private DentalContext Db() => new(new DbContextOptionsBuilder<DentalContext>().UseSqlServer(
        "Server=127.0.0.1,1;Database=NeverConnected;User Id=sa;Password=NeverUsed_4829!;Connect Timeout=1;TrustServerCertificate=true").Options);

    [Fact]
    public void PublicRequestContractContainsOnlyNamePhoneAndInternalRequestId()
    {
        Assert.Equal(new[] { "FullName", "PhoneNumber", "RequestId" },
            typeof(ConsultationRequest).GetProperties().Select(x => x.Name).OrderBy(x => x).ToArray());
        Assert.Equal(1, (int)LeadSourceType.Yektanet);
        Assert.Equal(2, (int)LeadSourceType.Clinic);
        Assert.Equal(3, (int)LeadSourceType.AdminSheet);
        Assert.Equal(4, (int)LeadSourceType.Website);
    }

    [Theory]
    [InlineData("ا", "09123456789", false)]
    [InlineData("امیر حسین", "bad", false)]
    [InlineData("امیر حسین", "09123456789", true)]
    public async Task InvalidInputRejectedBeforeAnyDatabaseAccess(string name, string phone, bool emptyId)
    {
        await using var db = Db();
        var controller = new ConsultationRequestsController(db, new LeadAssignmentRepository(db));
        Assert.IsType<BadRequestObjectResult>(await controller.Create(
            new ConsultationRequest(name, phone, emptyId ? Guid.Empty : Guid.NewGuid()), default));
    }

    [Fact]
    public void RegeneratedSnapshotMatchesTargetModelAndKeepsOperationalLeadIndex()
    {
        using var db = Db();
        var current = db.GetService<IDesignTimeModel>().Model;
        var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot!.Model;
        var initialized = db.GetService<IModelRuntimeInitializer>().Initialize(snapshot, designTime: true);
        Assert.Empty(db.GetService<IMigrationsModelDiffer>().GetDifferences(initialized.GetRelationalModel(), current.GetRelationalModel()));
        var lead = current.FindEntityType(typeof(DentalDashboard.Domain.Models.LeadAssignment))!;
        Assert.Contains(lead.GetIndexes(), x => x.GetDatabaseName() == "IX_LeadAssignments_Consultant_Active_AssignedAt_Id");
        Assert.Contains(lead.GetIndexes(), x => x.Properties.SingleOrDefault(p => p.Name == "ConsultationRequestId") != null && x.IsUnique);
    }
}
