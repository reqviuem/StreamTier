using StreamTier.API.Dtos;
using StreamTier.API.Models;

namespace StreamTier.API.Services.SubscriptionService;

public interface ISubscriptionService
{
    Task<bool> IsActiveAsync(string id);
    Task Save(Subscription subscription);

     Task DeleteAsync(string id);

     Task<SubscriptionExistsDto?> GetByStripeSubscriptionId(string stripeId);
}