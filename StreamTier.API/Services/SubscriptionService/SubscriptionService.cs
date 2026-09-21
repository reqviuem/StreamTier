using Microsoft.EntityFrameworkCore;
using StreamTier.API.Data;
using StreamTier.API.Dtos;
using StreamTier.API.Models;
using Subscription = StreamTier.API.Models.Subscription;

namespace StreamTier.API.Services.SubscriptionService;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _appDbContext;

    public SubscriptionService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }


    public async Task<bool> IsActiveAsync(string id)
    {
        var subscription = await _appDbContext.Subscriptions.FirstOrDefaultAsync(s => s.UserId == id);

        if (subscription != null && subscription.Status == Status.Active)
        {
            return true;
        }

        return false;
    }

    public async Task<SubscriptionExistsDto?> GetByStripeSubscriptionId(string stripeId)
    {
        var subscription =
            await _appDbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeId);

        if (subscription != null)
        {
            var foundSubscription = new SubscriptionExistsDto()
            {
                Id = subscription.Id
            };

            return foundSubscription;
        }

        return null;
    }


    public async Task Save(Subscription subscription)
    {

        var hasActive =
            await _appDbContext.Subscriptions.AnyAsync(s => s.UserId == subscription.UserId && s.Status == Status.Active);

        if (hasActive)
        {
            throw new InvalidOperationException("User already has an active subscription.");
        }
        
        await _appDbContext.Subscriptions.AddAsync(subscription);

        await _appDbContext.SaveChangesAsync();
    }


    public async Task DeleteAsync(string stripeId)
    {
        var subscription =
            await _appDbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeId);

        if (subscription == null)
            return; 
        
        _appDbContext.Subscriptions.Remove(subscription);
        await _appDbContext.SaveChangesAsync();
        
    }
}