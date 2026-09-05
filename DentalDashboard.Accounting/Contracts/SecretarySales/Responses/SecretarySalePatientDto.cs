using DentalDashboard.Accounting.Domain.SecretarySales.Enums;

namespace DentalDashboard.Accounting.Contracts.SecretarySales;

public sealed record SecretarySalePatientDto(Guid PatientUserId, string FirstName, string LastName, string PhoneNumber);
