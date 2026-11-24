using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.PatientReferrals;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Infrastracture.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class LeadPatientNameTests
{
    [Theory]
    [InlineData("  علی   رضا مقدم  ", "علی", "رضا مقدم")]
    [InlineData("مریم", "مریم", "")]
    [InlineData("علی\tمقدم", "علی", "مقدم")]
    public void NameIsNormalizedWithoutLosingCompoundSurname(string value, string first, string last)
    {
        Assert.True(LeadPatientName.TryCreate(value, out var name));
        Assert.Equal(first, name!.FirstName);
        Assert.Equal(last, name.LastName);
    }

    [Fact]
    public void EmptyOrOversizedNameIsRejected()
    {
        Assert.False(LeadPatientName.TryCreate("  ", out _));
        Assert.False(LeadPatientName.TryCreate(new string('a', 101), out _));
        Assert.False(LeadPatientName.TryCreate("Ali " + new string('a', 101), out _));
    }

    [Fact]
    public async Task RenameUpdatesRelatedSourcesTogetherAndLeavesOtherPhonesAndStaffAlone()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new NameContext(new DbContextOptionsBuilder<DentalContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var lead = new LeadAssignment { UserName = "Old", PhoneNumber = "09120000001" };
        var duplicate = new LeadAssignment { UserName = "Old duplicate", PhoneNumber = lead.PhoneNumber };
        var other = new LeadAssignment { UserName = "Other", PhoneNumber = "09120000002" };
        var patient = new User { Id = Guid.NewGuid(), FirstName = "Old", LastName = "Patient", PhoneNumber = lead.PhoneNumber, PasswordHash = "test" };
        patient.PatientProfile = new PatientProfile { UserId = patient.Id, NationalCode = "test" };
        var staff = new User { Id = Guid.NewGuid(), FirstName = "Staff", LastName = "Name", PhoneNumber = lead.PhoneNumber, PasswordHash = "test" };
        var sheet = new AdminSheetLead { FirstName = "Old", LastName = "Sheet", PhoneNumber = lead.PhoneNumber };
        var file = new PatientFile { FirstName = "Old", LastName = "File", PhoneNumber = lead.PhoneNumber };
        var referral = new PatientReferral { ReferredFirstName = "Old", ReferredLastName = "Referral", ReferredPhoneNumber = lead.PhoneNumber };
        db.AddRange(lead, duplicate, other, patient, staff, sheet, file, referral);
        await db.SaveChangesAsync();
        Assert.True(LeadPatientName.TryCreate("علی رضا مقدم", out var name));
        await new LeadAssignmentRepository(db).SynchronizePatientNameAsync(lead, name!);
        Assert.True(db.ChangeTracker.HasChanges());
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.Equal(2, await db.LeadAssignments.CountAsync(x => x.UserName == name!.FullName));
        Assert.Equal("Other", (await db.LeadAssignments.FindAsync(other.Id))!.UserName);
        Assert.Equal("Staff", (await db.Users.FindAsync(staff.Id))!.FirstName);
        Assert.Equal("رضا مقدم", (await db.Users.FindAsync(patient.Id))!.LastName);
        Assert.Equal("رضا مقدم", (await db.AdminSheetLeads.SingleAsync()).LastName);
        Assert.Equal("رضا مقدم", (await db.PatientFiles.SingleAsync()).LastName);
        Assert.Equal("رضا مقدم", (await db.PatientReferrals.SingleAsync()).ReferredLastName);
    }

    private sealed class NameContext(DbContextOptions<DentalContext> options) : DentalContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            Type[] types = [typeof(LeadAssignment), typeof(AdminSheetLead), typeof(PatientFile), typeof(User), typeof(PatientProfile), typeof(UserRole), typeof(Role), typeof(PatientReferral)];
            foreach (var type in types)
            {
                var entity = builder.Entity(type);
                foreach (var property in type.GetProperties())
                {
                    var keep = type == typeof(User) && property.Name is "PatientProfile" or "UserRoles"
                        || type == typeof(PatientProfile) && property.Name == "User"
                        || type == typeof(UserRole) && property.Name is "User" or "Role";
                    if (!keep && property.PropertyType != typeof(string) && !property.PropertyType.IsValueType)
                        entity.Ignore(property.Name);
                }
            }
            foreach (var type in builder.Model.GetEntityTypes().Select(x => x.ClrType).ToArray())
                if (!types.Contains(type)) builder.Ignore(type);
            builder.Entity<User>().HasOne(x => x.PatientProfile).WithOne(x => x.User).HasForeignKey<PatientProfile>(x => x.UserId);
        }
    }
}
