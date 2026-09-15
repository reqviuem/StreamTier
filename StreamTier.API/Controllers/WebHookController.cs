using Microsoft.AspNetCore.Mvc;
using StreamTier.API.Services;
using StreamTier.API.Services.UserService;
using Stripe;

namespace StreamTier.API.Controllers;

[ApiController]
public class WebHookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IWebHookService _service;
    private readonly IUSerService _uSerService;

    public WebHookController(IConfiguration config, IWebHookService service, IUSerService uSerService)
    {
        _config = config;
        _service = service;
        _uSerService = uSerService;
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
             await _service.OnSessionCompleteSubscription(stripeEvent);

            await _uSerService.UpdateCustomerByIdAsync(stripeEvent);

            return Ok();
        }

        if (stripeEvent.Type == "invoice.paid")
        {
            await _service.OnInvoiceCreate(stripeEvent);

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
                await _service.OnSubscriptionDelete(stripeEvent);
            }
            catch (NullReferenceException e)
            {
                return BadRequest("Subscription not found");
            }
        }

        return BadRequest("Event not found");
    }
}