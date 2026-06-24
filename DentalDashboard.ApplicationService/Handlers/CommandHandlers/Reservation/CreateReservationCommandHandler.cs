using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation
{
    public class CreateReservationCommandHandler : ICommandHandler<CreateReservationCommand, CreateReservationResponse>
    {
        private const int MaxReservationsPerConsultantAtSameTime = 10;
        private readonly IReservationRepository reservationRepository;
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly IConsultantProfileRepository consultantProfileRepository;

        public CreateReservationCommandHandler(IReservationRepository reservationRepository, ILeadAssignmentRepository leadAssignmentRepository, IConsultantProfileRepository consultantProfileRepository)
        {
            this.reservationRepository = reservationRepository;
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.consultantProfileRepository = consultantProfileRepository;
        }

        public async Task<Result<CreateReservationResponse>> HandleAsync(CreateReservationCommand command, CancellationToken cancellationToken = default)
        {
            if (command.ReservationAt <= DateTime.Now)
                return Result<CreateReservationResponse>.Failure("زمان رزرو باید در آینده باشد");

            var consultant = await consultantProfileRepository.GetByIdAsync(command.ConsultantProfileId);
            if (consultant == null || consultant.IsDeleted)
                return Result<CreateReservationResponse>.Failure("مشاور یافت نشد");

            var lead = await leadAssignmentRepository.GetByIdAndConsultantAsync(command.LeadAssignmentId, command.ConsultantProfileId);
            if (lead == null || lead.IsDeleted)
                return Result<CreateReservationResponse>.Failure("لید برای این مشاور یافت نشد");

            if (lead.ReportSubmittedAt == null || (lead.CallResult != LeadCallResult.Contacted && lead.CallResult != LeadCallResult.Converted))
                return Result<CreateReservationResponse>.Failure("فقط لیدهای تماس موفق قابل رزرو هستند");

            if (await reservationRepository.HasActiveReservationForLeadAsync(command.LeadAssignmentId))
                return Result<CreateReservationResponse>.Failure("برای این بیمار قبلا رزرو فعال ثبت شده است");

            var sameTimeCount = await reservationRepository.CountActiveReservationsAtAsync(command.ConsultantProfileId, command.ReservationAt);
            if (sameTimeCount >= MaxReservationsPerConsultantAtSameTime)
                return Result<CreateReservationResponse>.Failure("ظرفیت این بازه زمانی برای مشاور تکمیل است");

            var reservation = new global::Reservation
            {
                LeadAssignmentId = lead.Id,
                ConsultantProfileId = command.ConsultantProfileId,
                ReservationAt = command.ReservationAt,
                Description = command.Description,
                CreatedAt = DateTime.UtcNow
            };

            await reservationRepository.AddAsync(reservation);
            await reservationRepository.SaveChange();

            return Result<CreateReservationResponse>.Success(new CreateReservationResponse
            {
                Id = reservation.Id,
                LeadAssignmentId = reservation.LeadAssignmentId,
                ConsultantProfileId = reservation.ConsultantProfileId,
                ReservationAt = reservation.ReservationAt,
                PatientName = lead.UserName,
                PatientPhoneNumber = lead.PhoneNumber
            }, "رزرو با موفقیت ثبت شد");
        }
    }
}
