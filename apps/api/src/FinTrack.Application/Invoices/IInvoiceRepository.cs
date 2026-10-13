namespace FinTrack.Application.Invoices;

public interface IInvoiceRepository
{
    Task<IReadOnlyList<InvoiceDto>> ListAsync(Guid creditCardId, CancellationToken cancellationToken);
}
