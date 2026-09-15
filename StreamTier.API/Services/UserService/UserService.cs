using Microsoft.EntityFrameworkCore;
using StreamTier.API.Data;
using Stripe;
using Stripe.Checkout;

namespace StreamTier.API.Services.UserService;

public class UserService : IUSerService
{
    private readonly AppDbContext _appDbContext;
    
    public UserService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<string> GetStripeCustomerIdAsync(string stripeCustomerId, string userId)
    {
        var user = await _appDbContext.Users
            .Where(s => s.Id == userId)
            .FirstOrDefaultAsync();

        user?.StripeCustomerId = stripeCustomerId;

        await _appDbContext.SaveChangesAsync();

        return stripeCustomerId;
    }

    public async Task UpdateCustomerByIdAsync(Event stripeEvent)
    {
        var stripeSession = stripeEvent.Data.Object as Session;

        if (stripeSession != null)
        {
            var user = await _appDbContext.Users
                .Where(s => s.Id == stripeSession.Metadata["userId"])
                .FirstOrDefaultAsync();

            user?.StripeCustomerId = stripeSession.CustomerId;

            await _appDbContext.SaveChangesAsync();
        }
        else
        {
            throw new  NullReferenceException();
        }
        
    }
}