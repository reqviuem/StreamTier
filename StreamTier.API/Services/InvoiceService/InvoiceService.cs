using Microsoft.EntityFrameworkCore;
using StreamTier.API.Data;

namespace StreamTier.API.Services.InvoiceService;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _appDbContext;
    
    public InvoiceService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public void Save()
    {
        var invoice = _appDbContext.Invoices.FirstOrDefaultAsync();
    }
}