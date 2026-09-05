namespace DentalDashboard.Accounting.Contracts.Queries;

public sealed record FinancialTransactionReceiptResponse(
    byte[] Content,
    string ContentType,
    string FileName);
