using Microsoft.EntityFrameworkCore;
using StreamTier.API.Exceptions;
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

    // Stripe id of the user's current paid subscription, or null if the user is on the free plan.
    public async Task<string?> GetStripeSubscriptionId(string userId)
    {
        return await _appDbContext.Subscriptions
            .Where(s => s.UserId == userId
                        && (s.Status == Status.Active || s.Status == Status.PastDue)
                        && s.StripeSubscriptionId != null)
            .Select(s => s.StripeSubscriptionId)
            .FirstOrDefaultAsync();
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

    public async Task CreateFreeSubscriptionAsync(string userId)
    {
        await _appDbContext.Subscriptions.AddAsync(BuildFreeSubscription(userId));

        await _appDbContext.SaveChangesAsync();
    }

    // replaces the free plan with a paid one.
    public async Task ActivatePaidSubscriptionAsync(Subscription subscription)
    {
        var existingActive = await _appDbContext.Subscriptions
            .FirstOrDefaultAsync(s =>
                s.UserId == subscription.UserId
                && (s.Status == Status.Active || s.Status == Status.PastDue));

        if (existingActive is not null && existingActive.PlanId != "FreePlan")
        {
            throw new PermanentWebhookException("User already has an active paid subscription.");
        }

        // Canceling the free subscription
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
            throw new PermanentWebhookException("Subscription not found.");

        if (subscriptionToBeCanceled.Status == Status.Canceled)
            return;

        subscriptionToBeCanceled.Status = Status.Canceled;

        await _appDbContext.AddAsync(BuildFreeSubscription(subscriptionToBeCanceled.UserId));

        await _appDbContext.SaveChangesAsync();
    }

    private static Subscription BuildFreeSubscription(string userId)
    {
        var dto = new CreateSubscriptionDto
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = null,
            PlanId = "FreePlan",
            StripeCustomerId = null,
            StripeSubscriptionId = null
        };

        return Subscription.FromDto(dto);
    }

    public async Task OnPaymentFailed(string subscriptionId)
    {
        var subscription =
            await _appDbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId)
            ?? throw new PermanentWebhookException("Subscription not found.");

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