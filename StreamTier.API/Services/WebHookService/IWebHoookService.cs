using StreamTier.API.Dtos;
using Stripe.Checkout;

namespace StreamTier.API.Services;

public interface IWebHookService
{
    Task Save(CheckoutSubscriptionDto subscriptionDto);
}