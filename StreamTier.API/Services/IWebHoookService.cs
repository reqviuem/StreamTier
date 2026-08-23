using Stripe.Checkout;

namespace StreamTier.API.Services;

public interface IWebHookService
{
    Task Save(Session session);
}