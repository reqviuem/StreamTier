using Microsoft.AspNetCore.Mvc;
using StreamTier.API.Dtos;
using StreamTier.API.Models;
using StreamTier.API.Services;
using Stripe;
using Stripe.Checkout;

namespace StreamTier.API;

[ApiController]
public class WebHookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IWebHookService _hookService;

    public WebHookController(IConfiguration config, IWebHookService webHookService)
    {
        _config = config;
        _hookService = webHookService;
    }

    [HttpPost]
    [Route("/webhooks/stripe")]
    public async Task<IActionResult> Webhooks()
    {
        var stripeEvent = GetStripeEvent().Result;


        if (stripeEvent.Type == "checkout.session.completed")
        {
            SaveSubscription();

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

    private void SaveSubscription()
    {
        var session = GetStripeEvent().Result.Data.Object as Session;

        var subscription = new CheckoutSubscriptionDto()
        {
            UserId = session.Metadata?["userId"],
            PlanId = session.Metadata?["planId"],
            Status = Status.Active,
            StripeCustomerId = session.CustomerId,
            StripeSubscriptionId = session.SubscriptionId,
            CurrentPeriodStart = session.Created,
            CurrentPeriodEnd = session.ExpiresAt,
            CreatedAt = session.Created
        };

        _hookService.Save(subscription);
    }


    private async Task<Event> GetStripeEvent()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();

        var stripeSignature = Request.Headers["Stripe-Signature"];


        var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _config["Stripe:WebhookSecret"]);

        return stripeEvent;
    }
}