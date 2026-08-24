using StreamTier.API.Data;
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


    public async Task Save(Session session)
    {
        var subscription = new Subscription()
        {
            UserId = session.Metadata?["userId"],
            PlanId = session.Metadata?["planId"],
            Status = Status.Active,
            StripeCustomerId = session.CustomerId,
            StripeSubscriptionId = session.SubscriptionId,
            CurrentPeriodStart = session.Created,
            CurrentPeriodEnd = session.ExpiresAt,
            CreatedAt = session.Created
        };

        await _appDbContext.Subscriptions.AddAsync(subscription);
        
        await _appDbContext.SaveChangesAsync();
    }
}