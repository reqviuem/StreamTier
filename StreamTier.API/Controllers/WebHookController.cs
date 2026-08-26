using Microsoft.AspNetCore.Mvc;
using StreamTier.API.Dtos;
using StreamTier.API.Models;
using StreamTier.API.Services;
using StreamTier.API.Services.SubscriptionService;
using Stripe;
using Stripe.Checkout;
using SubscriptionService = Stripe.SubscriptionService;

namespace StreamTier.API;

[ApiController]
public class WebHookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ISubscriptionService _subscriptionService;

    public WebHookController(IConfiguration config, ISubscriptionService subscriptionService)
    {
        _config = config;
        _subscriptionService = subscriptionService;
    }

    [HttpPost]
    [Route("/webhooks/stripe")]
    public async Task<IActionResult> Webhooks()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();

        var stripeSignature = Request.Headers["Stripe-Signature"];

        var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _config["Stripe:WebhookSecret"]);
        
        if (stripeEvent.Type == "checkout.session.completed")
        {
            SaveSubscription(stripeEvent);

            return Ok();
        }

        if (stripeEvent.Type == "invoice.paid")
        {
            
        }

        if (stripeEvent.Type == "invoice.payment_failed")
        {
        }

        if (stripeEvent.Type == "customer.subscription.deleted")
        {
        }

        return BadRequest("No events captured");
    }

    private void SaveSubscription(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;

        var stripeSubscriptionService = new SubscriptionService();
        
        var stripeSubscription = stripeSubscriptionService.Get($"{session?.SubscriptionId}");
        
        var subscription = new CheckoutSubscriptionDto()
        {
            UserId = session.Metadata?["userId"],
            PlanId = session.Metadata?["planId"],
            Status = Status.Active,
            StripeCustomerId = session.CustomerId,
            StripeSubscriptionId = session.SubscriptionId,
            CurrentPeriodStart = stripeSubscription.Items.Data[0].CurrentPeriodStart,
            CurrentPeriodEnd = stripeSubscription.Items.Data[0].CurrentPeriodEnd,
            CreatedAt = session.Created
        };

        _subscriptionService.Save(subscription);
    }


    private void SaveInvoice(Event stripeEvent)
    {
        
    }
}