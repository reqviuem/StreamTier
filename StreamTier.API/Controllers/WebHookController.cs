using Microsoft.AspNetCore.Mvc;
using StreamTier.API.Services;
using Stripe;
using Stripe.Checkout;

namespace StreamTier.API;

[ApiController]
public class WebHookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IWebHookService _hookService;

    public WebHookController(IConfiguration config, IWebHookService webHookService )
    {
        _config = config;
        _hookService = webHookService;
    }
    
    [HttpPost]
    [Route("/webhooks/stripe")]
    public async Task<IActionResult> Webhooks()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();

        var stripeSignature = Request.Headers["Stripe-Signature"];
        

        var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _config["Stripe:WebhookSecret"]);

        if (stripeEvent.Type == "charge.succeeded")
        {
            
        }
        
        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Session;
            
            if (session != null)
            {
              _hookService.Save(session);

               return Ok();
            }
            
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
        return BadRequest();
    }
}