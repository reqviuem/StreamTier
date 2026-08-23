namespace StreamTier.API.Services;

public interface IUSerService
{
    Task<string> GetStripeCustomerId(string stripeCustomerId, string userId);
}