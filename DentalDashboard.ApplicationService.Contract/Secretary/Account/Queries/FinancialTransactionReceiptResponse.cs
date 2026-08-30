namespace DentalDashboard.ApplicationService.Contract.Secretary.Account.Queries;

public sealed record FinancialTransactionReceiptResponse(
    byte[] Content,
    string ContentType,
    string FileName);
