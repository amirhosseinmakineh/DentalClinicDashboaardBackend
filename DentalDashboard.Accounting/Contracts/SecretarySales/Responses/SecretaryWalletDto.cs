using DentalDashboard.Accounting.Domain.SecretarySales.Enums;

namespace DentalDashboard.Accounting.Contracts.SecretarySales;

public sealed record SecretaryWalletDto(decimal Balance, decimal TotalRewards, int ApprovedSalesCount);
