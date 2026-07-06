using System.Globalization;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Services;
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
                x.FirstName,
                x.LastName,
                x.PhoneNumber,
                RoleName = x.UserRoles
                    .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
                    .Select(ur => ur.Role!.RoleName)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var lines = new List<string>
        {
            CsvExportHelper.JoinRow("نام", "نام خانوادگی", "شماره تماس", "نقش")
        };

        foreach (var row in rows)
        {
            lines.Add(CsvExportHelper.JoinRow(
                row.FirstName,
                row.LastName,
                row.PhoneNumber,
                AdminReportPersianLabels.ToPersianRole(row.RoleName)));
        }

        return CsvExportHelper.BuildFile(lines.ToArray());
    }
}
