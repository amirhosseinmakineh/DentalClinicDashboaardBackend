using DentalDashboard.Accounting.Contracts.DTOs;
using DentalDashboard.Accounting.Contracts.Queries;

namespace DentalDashboard.Accounting.Contracts.Services;

public interface IFinancialTransactionReceiptService
{
    FinancialTransactionReceiptResponse Create(SecretaryFinancialTransactionDto transaction);
}
