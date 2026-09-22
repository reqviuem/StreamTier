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
    private readonly IUserService _userService;
    private readonly ILogger<WebHookController> _logger;
    
    public WebHookController(IConfiguration config, IWebHookService service, IUserService userService, ILogger<WebHookController> logger)
    {
        _config = config;
        _service = service;
        _userService = userService;
        _logger = logger;
    }

    [HttpPost]
    [Route("/webhooks/stripe")]
    public async Task<IActionResult> Webhooks()
    {

        var stripeEvent = await GetStripeEvent();
        
        var result = stripeEvent.Type switch
        {
            "checkout.session.completed" => await OnSessionComplete(stripeEvent),
            "invoice.paid" => await OnInvoicePaid(stripeEvent),
            "invoice.payment_failed" => Ok("Payment failed, try again!"),
            "customer.subscription.deleted" => await OnSubscriptionDelete(stripeEvent),
            _ => Ok()
        };

        return result;
    }

    private async Task<IActionResult> OnSessionComplete(Event stripeEvent)
    {
        try
        {
            await _service.OnSessionCompleteSubscription(stripeEvent);
            await _userService.UpdateCustomerByIdAsync(stripeEvent);
        }
        catch (InvalidOperationException e)
        {
            
            _logger.LogError(e, "Failed to process Stripe session.completed for event {EventId}: {Reason}", stripeEvent.Id, e.Message);
            return StatusCode(500);
        }
        
        return Ok();
    }

    private async Task<IActionResult> OnInvoicePaid(Event stripeEvent)
    {
        try
        {
            await _service.OnInvoicePaid(stripeEvent);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to process Stripe session.completed for event {EventId}: {Reason}", stripeEvent.Id, e.Message);
            return StatusCode(500);
        }

        return Ok();
    }

    private async Task<IActionResult> OnSubscriptionDelete(Event stripeEvent)
    {
        try
        {
            await _service.OnSubscriptionDelete(stripeEvent);
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e, "Failed to process Stripe session.completed for event {EventId}: {Reason}", stripeEvent.Id, e.Message);
            return StatusCode(500);
        }

        return Ok();
    }

    private async Task<Event> GetStripeEvent()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();

        var stripeSignature = Request.Headers["Stripe-Signature"];
        
        var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _config["Stripe:WebhookSecret"]);

        return stripeEvent;
    }
}