using Stripe;
using Stripe.Checkout;

namespace StreamTier.API.Services.UserService;

public interface IUSerService
{
    Task<string> GetStripeCustomerIdAsync(string stripeCustomerId, string userId);
    Task UpdateCustomerByIdAsync(Event stripeSession);
}