using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class SetAvailableCommandHandler : ICommandHandler<SetAvailableCommand>
    {
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly IAttendanceRepository attendanceRepository;
        private readonly ILeadAssignmentService leadAssignmentService;
        private readonly ILeadDomainService leadDomainService;
        private readonly IUserPresenceService presenceService;

        public SetAvailableCommandHandler(
            IConsultantProfileRepository consultantProfileRepository,
            IAttendanceRepository attendanceRepository,
            ILeadAssignmentService leadAssignmentService,
            ILeadDomainService leadDomainService,
            IUserPresenceService presenceService)
        {
            this.consultantProfileRepository = consultantProfileRepository;
            this.attendanceRepository = attendanceRepository;
            this.leadAssignmentService = leadAssignmentService;
            this.leadDomainService = leadDomainService;
            this.presenceService = presenceService;
        }

        public async Task<Result> HandleAsync(SetAvailableCommand command,CancellationToken cancellationToken = default)
        {
            var profile = await consultantProfileRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == command.ProfileId, cancellationToken);

            if (profile == null)
                return Result.Failure("مشاوری یافت نشد");

            if (profile.IsDeleted)
                return Result.Failure("پروفایل مشاور حذف شده است");

            if (!profile.IsCompleteProfile)
                return Result.Failure("پروفایل مشاور کامل نیست");

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var currentTime = TimeOnly.FromDateTime(now);

            if (command.IsAvailable)
            {
                if (!leadDomainService.IsWorkingTime(now))
                    return Result.Failure("امکان ثبت حضور فقط بین ساعت ۹ صبح تا ۹ شب وجود دارد");

                profile.IsAvailable = true;
                profile.IsOnline = false;
                profile.WorkStartTime = now.TimeOfDay;
                profile.LastOfflineAt = now;

                consultantProfileRepository.Update(profile);
                await UpsertAttendanceCheckInAsync(profile.Id, today, currentTime, cancellationToken);
                await consultantProfileRepository.SaveChange();

                await presenceService.LogAsync(
                    profile.UserId,
                    UserPresenceEventType.CheckIn,
                    now,
                    cancellationToken: cancellationToken);

                await leadAssignmentService.AssignOfflineLeadsToConsultantAsync(profile.Id);

                return Result.Success("حضور شما ثبت شد");
            }

            profile.IsAvailable = false;
            profile.IsOnline = false;
            profile.WorkEndTime = now.TimeOfDay;
            profile.LastOfflineAt = now;

            consultantProfileRepository.Update(profile);
            await UpsertAttendanceCheckOutAsync(profile.Id, today, currentTime, cancellationToken);
            await consultantProfileRepository.SaveChange();

            await presenceService.LogAsync(
                profile.UserId,
                UserPresenceEventType.CheckOut,
                now,
                cancellationToken: cancellationToken);

            return Result.Success("عدم حضور شما ثبت شد");
        }

        private async Task UpsertAttendanceCheckInAsync(
            long consultantProfileId,
            DateOnly attendanceDate,
            TimeOnly checkInTime,
            CancellationToken cancellationToken)
        {
            var attendance = await attendanceRepository.GetAll()
                .FirstOrDefaultAsync(
                    x => !x.IsDeleted &&
                         x.ConsultantProfileId == consultantProfileId &&
                         x.AttendanceDate == attendanceDate,
                    cancellationToken);

            if (attendance is null)
            {
                await attendanceRepository.AddAsync(new Attendance
                {
                    ConsultantProfileId = consultantProfileId,
                    AttendanceDate = attendanceDate,
                    CheckInTime = checkInTime,
                    CheckOutTime = default,
                    Status = AttendanceStatus.Present,
                    CreatedAt = DateTime.UtcNow
                });
                return;
            }

            attendance.CheckInTime = checkInTime;
            attendance.Status = AttendanceStatus.Present;
            attendance.UpdatedAt = DateTime.UtcNow;
            attendanceRepository.Update(attendance);
        }

        private async Task UpsertAttendanceCheckOutAsync(
            long consultantProfileId,
            DateOnly attendanceDate,
            TimeOnly checkOutTime,
            CancellationToken cancellationToken)
        {
            var attendance = await attendanceRepository.GetAll()
                .FirstOrDefaultAsync(
                    x => !x.IsDeleted &&
                         x.ConsultantProfileId == consultantProfileId &&
                         x.AttendanceDate == attendanceDate,
                    cancellationToken);

            if (attendance is null)
            {
                await attendanceRepository.AddAsync(new Attendance
                {
                    ConsultantProfileId = consultantProfileId,
                    AttendanceDate = attendanceDate,
                    CheckInTime = default,
                    CheckOutTime = checkOutTime,
                    Status = AttendanceStatus.Absent,
                    Description = "ثبت عدم حضور بدون ورود قبلی",
                    CreatedAt = DateTime.UtcNow
                });
                return;
            }

            attendance.CheckOutTime = checkOutTime;
            attendance.UpdatedAt = DateTime.UtcNow;
            attendanceRepository.Update(attendance);
        }
    }
}
