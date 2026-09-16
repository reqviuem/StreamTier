using Microsoft.EntityFrameworkCore;
using StreamTier.API.Data;
using StreamTier.API.Dtos;
using StreamTier.API.Models;

namespace StreamTier.API.Services.InvoiceService;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _appDbContext;
    
    public InvoiceService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task SaveAsync(CreateInvoiceDto invoiceDto)
    {
        var invoice = Invoice.FromDto(invoiceDto);
        
        await _appDbContext.Invoices.AddAsync(invoice);
        
        await _appDbContext.SaveChangesAsync();
    }
    
    public async Task<InvoiceExistsDto?> GetByStripeInvoiceId(string stripeId)
    {
        var invoice = await _appDbContext.Invoices.FirstOrDefaultAsync(i => i.StripeInvoiceId == stripeId);

        if (invoice != null)
        {
            var foundInvoice = new InvoiceExistsDto()
            {
                Id = invoice.Id
            };

            return foundInvoice;
        }

        return null;
    }
}