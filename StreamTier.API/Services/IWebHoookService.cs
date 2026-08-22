using Stripe.Checkout;

namespace StreamTier.API.Services;

public interface IWebHookService
{
    void Save(Session session);
}