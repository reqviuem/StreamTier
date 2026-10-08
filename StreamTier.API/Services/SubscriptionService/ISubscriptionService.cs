using StreamTier.API.Dtos;
using StreamTier.API.Models;

namespace StreamTier.API.Services.SubscriptionService;

public interface ISubscriptionService
{
    Task CreateFreeSubscriptionAsync(string userId);

    Task ActivatePaidSubscriptionAsync(Subscription subscription);

    Task DowngradeSubscriptionAsync(string id);

    Task<SubscriptionExistsDto?> GetByStripeSubscriptionId(string stripeId);

    Task OnPaymentFailed(string subscriptionId);

    Task OnPaymentSucceeded(string subscriptionId);

    Task<string?> GetStripeSubscriptionId(string id);
}