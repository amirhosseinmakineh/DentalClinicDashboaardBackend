using System.Text.Json;
using DentalDashboard.Controllers;
using DentalDashboard.Domain.Models;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class PatientPaperNationalCodeTests
{
    [Theory]
    [InlineData("0012345678")]
    [InlineData("۰۰۱۲۳۴۵۶۷۸")]
    [InlineData(null)]
    public void NationalCodeSurvivesApiBindingStorageAndReload(string? code)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(new {
            intake = new { nationalCode = code, medicalHistory = new { } },
            treatments = Array.Empty<object>(), dentalChart = new { }
        }, options);
        var request = JsonSerializer.Deserialize<PatientPaperRecordRequest>(json, options)!;
        Assert.Equal(code, request.Intake.NationalCode);
        var stored = JsonSerializer.Serialize(request.Intake);
        var loaded = JsonSerializer.Deserialize<PatientPaperIntake>(stored)!;
        Assert.Equal(code, loaded.NationalCode);
        var response = JsonSerializer.SerializeToElement(loaded, options);
        Assert.Equal(code, response.GetProperty("nationalCode").GetString());
    }

    [Fact]
    public void ExistingRecordsWithoutNationalCodeStillLoad()
    {
        var intake = JsonSerializer.Deserialize<PatientPaperIntake>("{\"HomePhone\":\"02112345678\"}")!;
        Assert.Null(intake.NationalCode);
        Assert.Equal("02112345678", intake.HomePhone);
    }
}
