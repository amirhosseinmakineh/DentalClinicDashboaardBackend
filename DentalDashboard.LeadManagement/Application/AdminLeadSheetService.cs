using System.Text.RegularExpressions;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.LeadManagement.Contract;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.LeadManagement.Application;

public sealed class AdminLeadSheetService(DentalContext db) : IAdminLeadSheetService
{
    public async Task<IReadOnlyList<AdminLeadSheet>> GetSheetsAsync(CancellationToken cancellationToken = default)
    {
        return await db.AdminLeadSheets.AsNoTracking().Where(x => x.IsActive).OrderByDescending(x => x.Id).ToListAsync(cancellationToken);
    }

    public async Task<AdminLeadSheet> CreateSheetAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Sheet name is required.", nameof(name));

        var sheet = new AdminLeadSheet { Name = name.Trim(), CreatedAt = DateTime.UtcNow };
        db.AdminLeadSheets.Add(sheet);
        await db.SaveChangesAsync(cancellationToken);
        return sheet;
    }

    public async Task<AdminSheetLead> AddLeadAsync(long sheetId, string phoneNumber, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        var sheetExists = await db.AdminLeadSheets.AnyAsync(x => x.Id == sheetId && x.IsActive, cancellationToken);
        if (!sheetExists)
            throw new KeyNotFoundException("Sheet not found.");

        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        if (!Regex.IsMatch(normalizedPhone, "^09\\d{9}$"))
            throw new ArgumentException("Invalid Iranian mobile number.", nameof(phoneNumber));
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("FirstName and LastName are required.");

        var duplicateExists = await db.LeadAssignments
            .AnyAsync(x => x.PhoneNumber == normalizedPhone && !x.IsDeleted, cancellationToken);
        if (duplicateExists)
            throw new InvalidOperationException("این شماره قبلاً در لیدها ثبت شده است.");

        var row = new AdminSheetLead
        {
            AdminLeadSheetId = sheetId,
            PhoneNumber = normalizedPhone,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.AdminSheetLeads.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return row;
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.AdminSheetLeads.Where(x => x.ProcessingStatus == 0).OrderBy(x => x.Id).Take(200).ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            try
            {
                var alreadyExists = await db.LeadAssignments.AnyAsync(x => x.PhoneNumber == row.PhoneNumber && !x.IsDeleted, cancellationToken);
                if (!alreadyExists)
                {
                    var lead = new LeadAssignment
                    {
                        UserName = $"{row.FirstName} {row.LastName}".Trim(),
                        PhoneNumber = row.PhoneNumber,
                        CreatedAt = DateTime.UtcNow,
                        AssignmentType = LeadAssignmentType.RealTime,
                        RequiresThreeMinuteCall = true,
                        LeadAssignmentState = LeadAssignmentState.New,
                        SourceType = LeadSourceType.AdminSheet
                    };
                    db.LeadAssignments.Add(lead);
                    await db.SaveChangesAsync(cancellationToken);
                    row.LeadAssignmentId = lead.Id;
                }
                row.ProcessingStatus = 1;
                row.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception exception)
            {
                row.ProcessingStatus = 2;
                row.ErrorMessage = exception.Message;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }

    private static string NormalizePhoneNumber(string? value)
    {
        var phoneNumber = (value ?? string.Empty).Trim().Replace(" ", string.Empty).Replace("-", string.Empty);
        if (phoneNumber.StartsWith("+98", StringComparison.Ordinal))
            phoneNumber = "0" + phoneNumber[3..];
        else if (phoneNumber.StartsWith("98", StringComparison.Ordinal) && phoneNumber.Length == 12)
            phoneNumber = "0" + phoneNumber[2..];
        else if (phoneNumber.StartsWith("9", StringComparison.Ordinal) && phoneNumber.Length == 10)
            phoneNumber = "0" + phoneNumber;
        return phoneNumber;
    }
}
