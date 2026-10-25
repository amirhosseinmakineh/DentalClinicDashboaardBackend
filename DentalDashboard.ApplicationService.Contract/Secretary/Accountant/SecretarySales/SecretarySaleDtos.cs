using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;

public sealed record SecretarySaleServiceDto(long Id, string Title, decimal Price, decimal SecretaryReward, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record SecretarySalePatientDto(Guid PatientUserId, string FirstName, string LastName, string PhoneNumber);
public sealed record SecretarySaleDto(long SaleId, Guid SecretaryUserId, string SecretaryName, Guid PatientUserId, string PatientName, string PatientPhoneNumber, long ServiceId, string ServiceTitle, decimal SalePrice, decimal SecretaryReward, SecretarySaleStatus Status, DateTime CreatedAt, DateTime? ReviewedAt);
public sealed record SecretaryWalletDto(decimal Balance, decimal TotalRewards, int ApprovedSalesCount);
public sealed record SecretaryWalletTransactionDto(long Id, decimal Amount, SecretaryWalletTransactionType TransactionType, string Description, DateTime CreatedAt, long? SaleId, string? ServiceTitle, string? PatientName);
