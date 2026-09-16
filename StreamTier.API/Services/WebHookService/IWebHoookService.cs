using StreamTier.API.Dtos;
using Stripe;
using Stripe.Checkout;

namespace StreamTier.API.Services;

public interface IWebHookService
{
    Task OnSessionCompleteSubscription(Event stripeEvent);
     Task OnInvoicePaid(Event stripeEvent);

     Task OnSubscriptionDelete(Event stripeEvent);
}