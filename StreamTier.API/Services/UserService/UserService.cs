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
        var stripeSession = stripeEvent.Data.Object as Session
                            ?? throw new InvalidOperationException("Expected Session object in Stripe event data.");


        if (!stripeSession.Metadata.TryGetValue("userId", out var userId))
            throw new InvalidOperationException("Stripe session metadata missing 'userId'.");
        
        var user = await _appDbContext.Users
                       .Where(s => s.Id == userId)
                       .FirstOrDefaultAsync()
                   ?? throw new InvalidOperationException(
                       $"No user found for Stripe session metadata userId '{stripeSession.Metadata["userId"]}'.");

        user.StripeCustomerId = stripeSession.CustomerId;

        await _appDbContext.SaveChangesAsync();
    }
}