using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation
{
    public class ReviewReservationAttendanceCommandHandler : ICommandHandler<ReviewReservationAttendanceCommand>
    {
        private readonly IReservationRepository reservationRepository;
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly IScoreLogRepository scoreLogRepository;
        private readonly IConsultantScoreDomainService consultantScoreDomainService;

        public ReviewReservationAttendanceCommandHandler(
            IReservationRepository reservationRepository,
            IConsultantProfileRepository consultantProfileRepository,
            IScoreLogRepository scoreLogRepository,
            IConsultantScoreDomainService consultantScoreDomainService)
        {
            this.reservationRepository = reservationRepository;
            this.consultantProfileRepository = consultantProfileRepository;
            this.scoreLogRepository = scoreLogRepository;
            this.consultantScoreDomainService = consultantScoreDomainService;
        }

        public async Task<Result> HandleAsync(ReviewReservationAttendanceCommand command, CancellationToken cancellationToken = default)
        {
            var reservation = await reservationRepository.GetAll().FirstOrDefaultAsync(x => x.Id == command.ReservationId, cancellationToken);
            if (reservation == null || reservation.IsDeleted)
                return Result.Failure("رزرو یافت نشد");

            if (reservation.ConsultantSaysPatientAttended == null)
                return Result.Failure("ابتدا مشاور باید حضور یا عدم حضور بیمار را تایید کند");

            if (reservation.IsAttendanceScoreApplied)
                return Result.Failure("امتیاز این بررسی قبلا اعمال شده است");

            var profile = await consultantProfileRepository.GetAll()
                .Include(x => x.ScoreLogs)
                .FirstOrDefaultAsync(x => x.Id == reservation.ConsultantProfileId, cancellationToken);
            if (profile == null || profile.IsDeleted)
                return Result.Failure("پروفایل مشاور یافت نشد");

            var scoreValue = consultantScoreDomainService.GetReservationAttendanceEventScore(command.Approved);
            var reason = command.Approved ? ScoreReason.ReservationAttendanceConfirmed : ScoreReason.ReservationAttendanceRejected;

            reservation.SecretaryUserId = command.SecretaryUserId;
            reservation.SecretaryApprovedConsultantConfirmation = command.Approved;
            reservation.SecretaryReviewedAt = DateTime.UtcNow;
            reservation.SecretaryReviewNote = command.Note;
            reservation.AttendanceConfirmationStatus = command.Approved
                ? ReservationAttendanceConfirmationStatus.SecretaryApproved
                : ReservationAttendanceConfirmationStatus.SecretaryRejected;
            reservation.IsAttendanceScoreApplied = true;
            reservation.AttendanceScoreValue = scoreValue;
            reservation.AttendanceScoreAppliedAt = DateTime.UtcNow;
            reservation.UpdatedAt = DateTime.UtcNow;

            var scoreLog = new ScoreLog
            {
                ConsultantProfileId = reservation.ConsultantProfileId,
                Source = ScoreSource.System,
                Reason = reason,
                ScoreValue = scoreValue,
                Description = command.Note,
                LeadAssignmentId = reservation.LeadAssignmentId,
                CreatedByUserId = command.SecretaryUserId,
                UserId = profile.UserId,
                CreatedAt = DateTime.UtcNow
            };

            consultantScoreDomainService.ApplyScoreEvent(profile, scoreLog);

            await scoreLogRepository.AddAsync(scoreLog);
            reservationRepository.Update(reservation);
            consultantProfileRepository.Update(profile);
            await scoreLogRepository.SaveChange();

            return Result.Success("بررسی منشی ثبت و امتیاز مشاور اعمال شد");
        }
    }
}
