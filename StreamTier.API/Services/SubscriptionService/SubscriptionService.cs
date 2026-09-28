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


    public async Task SaveAsync(Subscription subscription)
    {
        var existingActive = await _appDbContext.Subscriptions
            .FirstOrDefaultAsync(s =>
                s.UserId == subscription.UserId
                && (s.Status == Status.Active || s.Status == Status.PastDue));
        
        if (existingActive is not null && existingActive.PlanId != "FreePlan")
        {
            throw new InvalidOperationException("User already has an active paid subscription.");
        }

        if (existingActive is not null)
        {
            existingActive.Status = Status.Canceled;
        }

        await _appDbContext.Subscriptions.AddAsync(subscription);

        await _appDbContext.SaveChangesAsync();
    }

    public async Task DowngradeSubscriptionAsync(string id)
    {
        var subscriptionToBeCanceled =
            await _appDbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == id) ??
            throw new InvalidOperationException("Subscription not found.");

        if (subscriptionToBeCanceled.Status == Status.Canceled)
            return;

        subscriptionToBeCanceled.Status = Status.Canceled;

        var subscription = new CreateSubscriptionDto
        {
            UserId = subscriptionToBeCanceled.UserId,
            CreatedAt = DateTime.UtcNow,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = null,
            PlanId = "FreePlan",
            Status = Status.Active,
            StripeCustomerId = null,
            StripeSubscriptionId = null
        };

        await _appDbContext.AddAsync(Subscription.FromDto(subscription));

        await _appDbContext.SaveChangesAsync();
    }

    public async Task OnPaymentFailed(string subscriptionId)
    {
        var subscription =
            await _appDbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId)
            ?? throw new InvalidOperationException("Subscription not found.");

        if (subscription.Status != Status.Active)
            return;

        subscription.Status = Status.PastDue;

        await _appDbContext.SaveChangesAsync();
    }

    public async Task OnPaymentSucceeded(string subscriptionId)
    {
        var subscription =
            await _appDbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

        if (subscription is null || subscription.Status != Status.PastDue)
            return;

        subscription.Status = Status.Active;

        await _appDbContext.SaveChangesAsync();
    }
}