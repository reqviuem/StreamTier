using StreamTier.API.Dtos;

namespace StreamTier.API.Services.SubscriptionService;

public interface ISubscriptionService
{
    Task<bool> IsActive(string id);
    Task Save(CheckoutSubscriptionDto subscriptionDto);
}