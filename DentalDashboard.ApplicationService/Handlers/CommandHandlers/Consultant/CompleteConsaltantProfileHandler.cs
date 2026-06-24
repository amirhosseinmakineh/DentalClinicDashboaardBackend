using DentalDashboard.ApplicationService.Contract.Requests.Consultant;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class CompleteConsaltantProfileHandler : ICommandHandler<CompleteConsultantProfileCommand,long>
    {
        private readonly IUserRepository userRepository;
        private readonly IConsultantProfileRepository consultantProfileRepository;
        public CompleteConsaltantProfileHandler(IUserRepository userRepository, IConsultantProfileRepository consultantProfileRepository)
        {
            this.userRepository = userRepository;
            this.consultantProfileRepository = consultantProfileRepository;
        }
        public async Task<Result<long>> HandleAsync(CompleteConsultantProfileCommand command,CancellationToken cancellationToken = default)
        {

            var consultant = await userRepository.GetAll()
                .Include(x => x.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(
                    x => x.UserRoles.Any(ur => ur.Role.RoleName == "Consultant"),
                    cancellationToken);

            if (consultant == null)
                return Result<long>.Failure("مشاور یافت نشد");

            var profile = new ConsultantProfile()
            {
                Address = command.Address,
                CreatedAt = DateTime.Now,
                DeletedAt = null,
                IsAvailable = false,
                IsCompleteProfile = true,
                IsDeleted = false,
                IsOnline = false,
                LastOfflineAt = null,
                LastOnlineAt = null,
                NationalCode = command.NationalityCode,
                Notes = null,
                WorkStartTime = TimeSpan.Zero,
                WorkEndTime = TimeSpan.Zero,
                UserId = consultant.Id
            };
            await consultantProfileRepository.AddAsync(profile);
            await consultantProfileRepository.SaveChange();

            return Result<long>.Success(profile.Id,"اطلاعات مشاور با موفقیت تکمیل شد");
        }
    }
}
