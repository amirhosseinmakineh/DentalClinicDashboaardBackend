using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.DTOs;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Queries;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Services;

public interface IFinancialTransactionReceiptService
{
    FinancialTransactionReceiptResponse Create(SecretaryFinancialTransactionDto transaction);
}
