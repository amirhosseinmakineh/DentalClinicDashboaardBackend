using DentalDashboard.Accountant.Application.Contracts.DTOs;
using DentalDashboard.Accountant.Application.Contracts.Queries;

namespace DentalDashboard.Accountant.Application.Contracts.Services;

public interface IFinancialTransactionReceiptService
{
    FinancialTransactionReceiptResponse Create(FinancialTransactionDto transaction);
}
