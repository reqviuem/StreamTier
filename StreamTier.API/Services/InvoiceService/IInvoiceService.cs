using StreamTier.API.Dtos;

namespace StreamTier.API.Services.InvoiceService;

public interface IInvoiceService
{
    Task SaveAsync(CreateInvoiceDto invoiceDto);
     Task<InvoiceExistsDto?> GetByStripeInvoiceId(string stripeId);
}