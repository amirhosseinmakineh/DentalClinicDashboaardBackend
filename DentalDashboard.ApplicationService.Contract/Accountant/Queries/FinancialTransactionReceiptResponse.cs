namespace DentalDashboard.ApplicationService.Contract.Accountant.Queries;

public sealed record FinancialTransactionReceiptResponse(
    byte[] Content,
    string ContentType,
    string FileName);
