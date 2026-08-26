using Microsoft.EntityFrameworkCore;
using StreamTier.API.Data;
using StreamTier.API.Dtos;
using StreamTier.API.Models;

namespace StreamTier.API.Services.SubscriptionService;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _appDbContext;

    public SubscriptionService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }
    

    public async Task<bool> IsActive(string id)
    {
        var subscription = await _appDbContext.Subscriptions.FirstOrDefaultAsync(s => s.UserId == id);

        if (subscription != null && subscription.Status == Status.Active)
        {
            return true;
        }

        return false;
    }
    
    
    public async Task Save(CheckoutSubscriptionDto subscriptionDto)
    {
        var subscription = new Subscription()
        {
            UserId = subscriptionDto.UserId,
            PlanId = subscriptionDto.PlanId,
            Status = Status.Active,
            StripeCustomerId = subscriptionDto.StripeCustomerId,
            StripeSubscriptionId = subscriptionDto.StripeSubscriptionId,
            CurrentPeriodStart = subscriptionDto.CurrentPeriodStart,
            CurrentPeriodEnd = subscriptionDto.CurrentPeriodEnd,
            CreatedAt = subscriptionDto.CreatedAt
        };

        await _appDbContext.Subscriptions.AddAsync(subscription);
        
        await _appDbContext.SaveChangesAsync();
    }
}