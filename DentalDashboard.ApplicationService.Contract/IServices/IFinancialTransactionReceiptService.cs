using DentalDashboard.ApplicationService.Contract.Secretary.Account.DTOs;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.Queries;

namespace DentalDashboard.ApplicationService.Contract.IServices;

public interface IFinancialTransactionReceiptService
{
    FinancialTransactionReceiptResponse Create(SecretaryFinancialTransactionDto transaction);
}
