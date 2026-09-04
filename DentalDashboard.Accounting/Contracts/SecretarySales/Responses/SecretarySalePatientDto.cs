using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;

public sealed record SecretarySalePatientDto(Guid PatientUserId, string FirstName, string LastName, string PhoneNumber);
