namespace StreamTier.API.Services.SubscriptionService;

public interface ISubscriptionService
{
    Task<bool> IsActive(string id);
}