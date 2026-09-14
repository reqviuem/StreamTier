using StreamTier.API.Dtos;
using Stripe;
using Stripe.Checkout;

namespace StreamTier.API.Services;

public interface IWebHookService
{
    void SaveSubscription(Event stripeEvent);
     void SaveInvoice(Event stripeEvent);

     void OnSubscriptionDelete(Event stripeEvent);
}