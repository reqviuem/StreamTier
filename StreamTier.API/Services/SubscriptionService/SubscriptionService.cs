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
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(AppDbContext appDbContext, ILogger<SubscriptionService> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
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

        await using var transaction = await _appDbContext.Database.BeginTransactionAsync();

        // Canceling the free subscription.
        if (existingActive is not null)
        {
            existingActive.Status = Status.Canceled;
            await _appDbContext.SaveChangesAsync();
        }

        await _appDbContext.Subscriptions.AddAsync(subscription);

        await _appDbContext.SaveChangesAsync();

        await transaction.CommitAsync();
    }

    public async Task DowngradeSubscriptionAsync(string id)
    {
        var subscriptionToBeCanceled =
            await _appDbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == id) ??
            throw new PermanentWebhookException("Subscription not found.");

        if (subscriptionToBeCanceled.Status == Status.Canceled)
            return;

        await using var transaction = await _appDbContext.Database.BeginTransactionAsync();

        subscriptionToBeCanceled.Status = Status.Canceled;
        await _appDbContext.SaveChangesAsync();

        await _appDbContext.AddAsync(BuildFreeSubscription(subscriptionToBeCanceled.UserId));

        await _appDbContext.SaveChangesAsync();

        await transaction.CommitAsync();
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

    // Mirrors Stripe's view of the subscription into row.
    public async Task SyncFromStripeAsync(string stripeSubscriptionId, string stripeStatus,
        DateTime periodStart, DateTime periodEnd)
    {
        var subscription =
            await _appDbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);

        if (subscription is null)
        {
            _logger.LogWarning(
                "Ignoring update for Stripe subscription {StripeSubscriptionId}: no matching local subscription.",
                stripeSubscriptionId);
            return;
        }

        if (subscription.Status == Status.Canceled)
        {
            _logger.LogInformation(
                "Ignoring update for Stripe subscription {StripeSubscriptionId}: local subscription is already canceled.",
                stripeSubscriptionId);
            return;
        }

        switch (stripeStatus)
        {
            case "active":
                subscription.Status = Status.Active;
                break;
            case "past_due":
                subscription.Status = Status.PastDue;
                break;
        }

        subscription.CurrentPeriodStart = periodStart;
        subscription.CurrentPeriodEnd = periodEnd;

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