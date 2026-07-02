using System.Globalization;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Convertor;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public class UsersExportService
{
    private readonly DentalContext context;

    public UsersExportService(DentalContext context) => this.context = context;

    public async Task<byte[]> ExportCsvAsync(CancellationToken cancellationToken = default)
    {
        var rows = await context.Users.AsNoTracking()
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Select(x => new
            {
                x.Id,
                x.FirstName,
                x.LastName,
                x.PhoneNumber,
                RoleName = x.UserRoles
                    .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
                    .Select(ur => ur.Role!.RoleName)
                    .FirstOrDefault(),
                x.Gender,
                x.BirthDate,
                x.IsActive,
                x.IsCompleteProfile,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var lines = new List<string>
        {
            CsvExportHelper.JoinRow(
                "شناسه",
                "نام",
                "نام خانوادگی",
                "موبایل",
                "نقش",
                "جنسیت",
                "تاریخ تولد",
                "وضعیت فعال",
                "پروفایل تکمیل شده",
                "تاریخ ثبت‌نام")
        };

        foreach (var row in rows)
        {
            lines.Add(CsvExportHelper.JoinRow(
                row.Id.ToString(),
                row.FirstName,
                row.LastName,
                row.PhoneNumber,
                AdminReportPersianLabels.ToPersianRole(row.RoleName),
                row.Gender.ToPersian(),
                DateConvertor.ToPersianDateString(row.BirthDate),
                AdminReportPersianLabels.ToYesNo(row.IsActive),
                AdminReportPersianLabels.ToYesNo(row.IsCompleteProfile),
                DateConvertor.ToPersianDateTimeString(row.CreatedAt)));
        }

        return CsvExportHelper.BuildFile(lines.ToArray());
    }
}
