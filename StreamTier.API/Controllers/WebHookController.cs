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
            try
            {
                await _service.OnSessionCompleteSubscription(stripeEvent);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e.Message);
            }
            
            // Must be in different place 
            await _uSerService.UpdateCustomerByIdAsync(stripeEvent);

            return Ok();
        }

        if (stripeEvent.Type == "invoice.paid")
        {
            try
            {
                await _service.OnInvoicePaid(stripeEvent);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

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
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }
        }

        if (stripeEvent.Type == "customer.deleted")
        {
        }

        return NotFound("Event not found");
    }
}