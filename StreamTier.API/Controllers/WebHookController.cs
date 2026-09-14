using Microsoft.AspNetCore.Mvc;
using StreamTier.API.Dtos;
using StreamTier.API.Models;
using StreamTier.API.Services;
using StreamTier.API.Services.InvoiceService;
using StreamTier.API.Services.SubscriptionService;
using Stripe;
using Stripe.Checkout;
using Invoice = Stripe.Invoice;
using InvoiceService = Stripe.InvoiceService;
using Subscription = Stripe.Subscription;
using SubscriptionService = Stripe.SubscriptionService;

namespace StreamTier.API;

[ApiController]
public class WebHookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IWebHookService _service;
    
    public WebHookController(IConfiguration config, IWebHookService service)
    {
        _config = config;
        _service = service;
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
            _service.SaveSubscription(stripeEvent);

            return Ok();
        }

        if (stripeEvent.Type == "invoice.paid")
        {
            _service.SaveInvoice(stripeEvent);
            
            return Ok();
        }

        if (stripeEvent.Type == "invoice.payment_failed")
        {
            return BadRequest("Payment failed, try again!");
        }

        if (stripeEvent.Type == "customer.subscription.deleted")
        {
            try
            {
                _service.OnSubscriptionDelete(stripeEvent);
            }
            catch (NullReferenceException e)
            {
                return BadRequest("Subscription not found");
            }
            
        }

        return BadRequest();
    }

    
}