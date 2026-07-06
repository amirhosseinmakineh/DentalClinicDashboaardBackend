using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository attendanceRepository;

    public AttendanceService(IAttendanceRepository attendanceRepository)
    {
        this.attendanceRepository = attendanceRepository;
    }

    public async Task RecordCheckInAsync(
        long consultantProfileId,
        DateTime occurredAt,
        CancellationToken cancellationToken = default)
    {
        var attendanceDate = DateOnly.FromDateTime(occurredAt);
        var checkInTime = TimeOnly.FromDateTime(occurredAt);

        var existing = await attendanceRepository.GetAll()
            .Where(x => !x.IsDeleted &&
                        x.ConsultantProfileId == consultantProfileId &&
                        x.AttendanceDate == attendanceDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            if (!existing.CheckInTime.HasValue)
                existing.CheckInTime = checkInTime;

            existing.CheckOutTime = null;
            existing.Status = AttendanceStatus.Present;
            existing.UpdatedAt = DateTime.UtcNow;
            attendanceRepository.Update(existing);
        }
        else
        {
            await attendanceRepository.AddAsync(new Attendance
            {
                ConsultantProfileId = consultantProfileId,
                AttendanceDate = attendanceDate,
                CheckInTime = checkInTime,
                Status = AttendanceStatus.Present,
                CreatedAt = DateTime.UtcNow
            });
        }

        await attendanceRepository.SaveChange();
    }

    public async Task RecordCheckOutAsync(
        long consultantProfileId,
        DateTime occurredAt,
        CancellationToken cancellationToken = default)
    {
        var attendanceDate = DateOnly.FromDateTime(occurredAt);
        var checkOutTime = TimeOnly.FromDateTime(occurredAt);

        var existing = await attendanceRepository.GetAll()
            .Where(x => !x.IsDeleted &&
                        x.ConsultantProfileId == consultantProfileId &&
                        x.AttendanceDate == attendanceDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            existing.CheckOutTime = checkOutTime;
            existing.UpdatedAt = DateTime.UtcNow;
            attendanceRepository.Update(existing);
        }
        else
        {
            await attendanceRepository.AddAsync(new Attendance
            {
                ConsultantProfileId = consultantProfileId,
                AttendanceDate = attendanceDate,
                CheckOutTime = checkOutTime,
                Status = AttendanceStatus.Present,
                CreatedAt = DateTime.UtcNow
            });
        }

        await attendanceRepository.SaveChange();
    }
}
