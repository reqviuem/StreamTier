using StreamTier.API.Dtos;
using StreamTier.API.Models;

namespace StreamTier.API.Services.SubscriptionService;

public interface ISubscriptionService
{
    Task<bool> IsActiveAsync(string id);
    Task SaveAsync(Subscription subscription);

     Task DowngradeSubscriptionAsync(string id);

     Task<SubscriptionExistsDto?> GetByStripeSubscriptionId(string stripeId);
}