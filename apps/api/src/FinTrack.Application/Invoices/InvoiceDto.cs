namespace FinTrack.Application.Invoices;

public sealed record InvoiceItemDto(Guid InstallmentId, Guid PurchaseId, string Description,
    int Number, int InstallmentCount, decimal Amount);
public sealed record InvoiceDto(Guid? Id, Guid CreditCardId, DateOnly ClosingDate, DateOnly DueDate,
    decimal Total, IReadOnlyList<InvoiceItemDto> Items);
public sealed record CreditCardInvoiceSummaryDto(Guid CreditCardId, DateOnly AsOf,
    decimal Limit, decimal UsedLimit, decimal AvailableLimit, InvoiceDto CurrentInvoice,
    IReadOnlyList<InvoiceDto> FutureInvoices);
