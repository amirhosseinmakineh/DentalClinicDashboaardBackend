using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Utilities.Hasher;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation
{
    public class CompleteReservationPatientProfileCommandHandler : ICommandHandler<CompleteReservationPatientProfileCommand, CompleteReservationPatientProfileResponse>
    {
        private const string PatientRoleName = "Patient";
        private readonly IReservationRepository reservationRepository;
        private readonly IUserRepository userRepository;
        private readonly IPatientProfileRepository patientProfileRepository;
        private readonly IRoleService roleService;
        private readonly IUnitOfWork unitOfWork;

        public CompleteReservationPatientProfileCommandHandler(
            IReservationRepository reservationRepository,
            IUserRepository userRepository,
            IPatientProfileRepository patientProfileRepository,
            IRoleService roleService,
            IUnitOfWork unitOfWork)
        {
            this.reservationRepository = reservationRepository;
            this.userRepository = userRepository;
            this.patientProfileRepository = patientProfileRepository;
            this.roleService = roleService;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Result<CompleteReservationPatientProfileResponse>> HandleAsync(CompleteReservationPatientProfileCommand command, CancellationToken cancellationToken = default)
        {
            var reservation = await reservationRepository.GetAll()
                .Include(x => x.LeadAssignment)
                .FirstOrDefaultAsync(x => x.Id == command.ReservationId && !x.IsCanceled && !x.IsDeleted, cancellationToken);

            if (reservation == null)
                return Result<CompleteReservationPatientProfileResponse>.Failure("رزرو فعال یافت نشد");

            if (reservation.PatientUserId.HasValue)
                return Result<CompleteReservationPatientProfileResponse>.Failure("برای این رزرو قبلا پرونده بیمار تشکیل شده است");

            if (reservation.LeadAssignment == null)
                return Result<CompleteReservationPatientProfileResponse>.Failure("اطلاعات لید این رزرو یافت نشد");

            if (string.IsNullOrWhiteSpace(command.PhoneNumber))
                return Result<CompleteReservationPatientProfileResponse>.Failure("شماره موبایل بیمار الزامی است");

            var phoneNumber = command.PhoneNumber.Trim();

            if (reservation.LeadAssignment.PhoneNumber != phoneNumber)
                return Result<CompleteReservationPatientProfileResponse>.Failure("شماره موبایل بیمار باید با شماره لید رزرو شده یکسان باشد");

            if (await userRepository.ExistsAsync(x => x.PhoneNumber == phoneNumber))
                return Result<CompleteReservationPatientProfileResponse>.Failure("کاربری با این شماره موبایل قبلاً ثبت شده است");

            await unitOfWork.BeginTransactionAsync();
            try
            {
                var user = new Domain.Models.User
                {
                    Id = Guid.NewGuid(),
                    FirstName = command.FirstName.Trim(),
                    LastName = command.LastName.Trim(),
                    PhoneNumber = phoneNumber,
                    PasswordHash = PasswordHasher.HashPassword(command.PasswordHash),
                    BirthDate = command.BirthDate,
                    Gender = command.Gender,
                    AvatarImageName = command.AvatarImageName,
                    IsActive = true,
                    IsCompleteProfile = true
                };

                await userRepository.AddAsync(user);
                await roleService.AddRoleToUser(user.Id, PatientRoleName);

                var patientProfile = new PatientProfile
                {
                    UserId = user.Id,
                    NationalCode = string.Empty,
                    EmergencyPhoneNumber = command.EmergencyPhoneNumber,
                    InsuranceName = command.InsuranceName,
                    Notes = command.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                await patientProfileRepository.AddAsync(patientProfile);
                reservation.PatientUserId = user.Id;
                reservation.UpdatedAt = DateTime.UtcNow;
                reservationRepository.Update(reservation);

                await unitOfWork.CommitAsync();

                return Result<CompleteReservationPatientProfileResponse>.Success(new CompleteReservationPatientProfileResponse
                {
                    ReservationId = reservation.Id,
                    PatientUserId = user.Id,
                    PatientProfileId = patientProfile.Id,
                    LeadAssignmentId = reservation.LeadAssignmentId,
                    ConsultantProfileId = reservation.ConsultantProfileId,
                    ReservationAt = reservation.ReservationAt,
                    PatientName = $"{user.FirstName} {user.LastName}",
                    PatientPhoneNumber = user.PhoneNumber,
                    IsCompleteProfile = user.IsCompleteProfile,
                    RoleName = PatientRoleName
                }, "پرونده بیمار برای رزرو با موفقیت تشکیل شد");
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                return Result<CompleteReservationPatientProfileResponse>.Failure($"خطا در تشکیل پرونده بیمار: {ex.Message}");
            }
        }
    }
}
