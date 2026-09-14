using StreamTier.API.Dtos;

namespace StreamTier.API.Services.InvoiceService;

public interface IInvoiceService
{
    Task Save(CheckoutInvoiceDto invoiceDto);
}