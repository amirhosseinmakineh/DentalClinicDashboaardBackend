using DentalDashboard.ApplicationService.Contract.Accountant.DTOs;
using DentalDashboard.ApplicationService.Contract.Accountant.Queries;

namespace DentalDashboard.ApplicationService.Contract.Accountant.Services;

public interface IFinancialTransactionReceiptService
{
    FinancialTransactionReceiptResponse Create(FinancialTransactionDto transaction);
}
