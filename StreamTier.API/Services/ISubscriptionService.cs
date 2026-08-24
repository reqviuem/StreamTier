namespace StreamTier.API.Services;

public interface ISubscriptionService
{
    Task<bool> IsActive(string id);
}