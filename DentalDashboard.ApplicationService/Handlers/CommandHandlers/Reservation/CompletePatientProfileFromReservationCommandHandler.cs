using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Patient.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.PatientResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Utilities.Hasher;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation
{
    public class CompletePatientProfileFromReservationCommandHandler : ICommandHandler<CompletePatientProfileFromReservationCommand, CompletePatientProfileFromReservationResponse>
    {
        private const string PatientRoleName = "Patient";

        private readonly IReservationRepository reservationRepository;
        private readonly IUserRepository userRepository;
        private readonly IPatientProfileRepository patientProfileRepository;
        private readonly IRoleService roleService;
        private readonly IUnitOfWork unitOfWork;

        public CompletePatientProfileFromReservationCommandHandler(
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

        public async Task<Result<CompletePatientProfileFromReservationResponse>> HandleAsync(CompletePatientProfileFromReservationCommand command, CancellationToken cancellationToken = default)
        {
            var reservation = await reservationRepository.GetAll()
                .Include(x => x.LeadAssignment)
                .FirstOrDefaultAsync(x => x.Id == command.ReservationId && !x.IsDeleted && !x.IsCanceled, cancellationToken);

            if (reservation == null)
                return Result<CompletePatientProfileFromReservationResponse>.Failure("رزرو فعال یافت نشد");

            var lead = reservation.LeadAssignment;
            if (lead.ReportSubmittedAt == null || (lead.CallResult != LeadCallResult.Contacted && lead.CallResult != LeadCallResult.Converted))
                return Result<CompletePatientProfileFromReservationResponse>.Failure("فقط رزرو لیدهای دارای تماس موفق قابل تشکیل پرونده است");

            if (string.IsNullOrWhiteSpace(command.FirstName) || string.IsNullOrWhiteSpace(command.LastName))
                return Result<CompletePatientProfileFromReservationResponse>.Failure("نام و نام خانوادگی بیمار الزامی است");

            if (string.IsNullOrWhiteSpace(command.PasswordHash))
                return Result<CompletePatientProfileFromReservationResponse>.Failure("رمز عبور بیمار الزامی است");

            if (string.IsNullOrWhiteSpace(command.NationalCode) || string.IsNullOrWhiteSpace(command.Address))
                return Result<CompletePatientProfileFromReservationResponse>.Failure("کد ملی و آدرس بیمار الزامی است");

            await unitOfWork.BeginTransactionAsync();

            try
            {
                var phoneNumber = lead.PhoneNumber.Trim();
                var user = await userRepository.GetAll()
                    .Include(x => x.PatientProfile)
                    .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber && !x.IsDeleted, cancellationToken);

                if (user == null)
                {
                    user = new User
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
                        IsCompleteProfile = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await userRepository.AddAsync(user);
                }
                else
                {
                    if (user.PatientProfile != null && !user.PatientProfile.IsDeleted)
                    {
                        await unitOfWork.RollbackAsync();
                        return Result<CompletePatientProfileFromReservationResponse>.Failure("برای این شماره موبایل قبلا پرونده بیمار تشکیل شده است");
                    }

                    user.FirstName = command.FirstName.Trim();
                    user.LastName = command.LastName.Trim();
                    user.BirthDate = command.BirthDate;
                    user.Gender = command.Gender;
                    user.AvatarImageName = command.AvatarImageName;
                    user.IsActive = true;
                    user.IsCompleteProfile = true;
                    user.UpdatedAt = DateTime.UtcNow;
                    userRepository.Update(user);
                }

                var patientProfile = new PatientProfile
                {
                    UserId = user.Id,
                    NationalCode = command.NationalCode.Trim(),
                    Address = command.Address.Trim(),
                    EmergencyPhoneNumber = command.EmergencyPhoneNumber?.Trim(),
                    InsuranceName = command.InsuranceName?.Trim(),
                    Notes = command.Notes?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                await patientProfileRepository.AddAsync(patientProfile);
                await roleService.AddRoleToUser(user.Id, PatientRoleName);
                await unitOfWork.CommitAsync();

                return Result<CompletePatientProfileFromReservationResponse>.Success(new CompletePatientProfileFromReservationResponse
                {
                    UserId = user.Id,
                    PatientProfileId = patientProfile.Id,
                    ReservationId = reservation.Id,
                    LeadAssignmentId = lead.Id,
                    PhoneNumber = phoneNumber,
                    RoleName = PatientRoleName
                }, "پرونده بیمار با موفقیت تشکیل شد");
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                return Result<CompletePatientProfileFromReservationResponse>.Failure($"خطا در تشکیل پرونده بیمار: {ex.Message}");
            }
        }
    }
}
