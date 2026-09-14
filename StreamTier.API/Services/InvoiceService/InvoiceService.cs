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

    public async Task Save(CheckoutInvoiceDto invoiceDto)
    {
        var invoice = new Invoice()
        {
            UserId = invoiceDto.UserId,
            AmountPaidInCents = invoiceDto.AmountPaidInCents,
            Currency = invoiceDto.Currency,
            StripeInvoiceId = invoiceDto.StripeInvoiceId,
            SubscriptionId = invoiceDto.SubscriptionId
        };
        
        await _appDbContext.Invoices.AddAsync(invoice);
        
        await _appDbContext.SaveChangesAsync();
    }
}