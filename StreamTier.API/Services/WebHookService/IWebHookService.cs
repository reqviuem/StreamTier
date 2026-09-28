using Stripe;

namespace StreamTier.API.Services.WebHookService;

public interface IWebHookService
{
    Task OnSessionCompleteSubscription(Event stripeEvent);
    Task OnInvoicePaid(Event stripeEvent);

    Task OnSubscriptionDelete(Event stripeEvent);

    Task OnPaymentFailed(Event stripeEvent);
}