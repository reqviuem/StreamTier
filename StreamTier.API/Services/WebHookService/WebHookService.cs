using StreamTier.API.Data;
using StreamTier.API.Dtos;
using StreamTier.API.Models;
using Stripe.Checkout;

namespace StreamTier.API.Services;

public class WebHookService : IWebHookService
{
    private readonly AppDbContext _appDbContext;

    public WebHookService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
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