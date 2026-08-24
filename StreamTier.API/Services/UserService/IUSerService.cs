namespace StreamTier.API.Services.UserService;

public interface IUSerService
{
    Task<string> GetStripeCustomerId(string stripeCustomerId, string userId);
}