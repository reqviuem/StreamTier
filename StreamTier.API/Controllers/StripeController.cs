using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamTier.API.Dtos;
using StreamTier.API.Services;
using Stripe;
using Stripe.Checkout;

namespace StreamTier.API;

[ApiController]
public class StripeController : ControllerBase
{
    private readonly IStripeService _service;

    private readonly IConfiguration _config;

    public StripeController(IStripeService service, IConfiguration config)
    {
        _service = service;
        _config = config;
    }


    [Authorize]
    [HttpPost]
    [Route("/checkout/session")]
    public async Task<IActionResult> CheckoutSession(StripeCheckoutRequest stripeCheckoutRequest)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        
        
        if (await _service.GetActivePlanByIdAsync(stripeCheckoutRequest.PlanId) == null)
        {
            return BadRequest("Plan not found, Try again");
        }

        var plan = await _service.GetActivePlanByIdAsync(stripeCheckoutRequest.PlanId);
        
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
        
        var stripeSessionService = new SessionService();
        var stripeCheckoutSession = await stripeSessionService.CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription",
            PaymentMethodTypes = ["card"],
            ClientReferenceId = userId,
            SuccessUrl = _config["Stripe:SuccessUrl"],
            CancelUrl = _config["Stripe:CancelUrl"],
            CustomerEmail = userEmail,
            LineItems = new()
            {
                new()
                {
                    Price = plan?.StripePriceId,
                    Quantity = 1
                }
            }
        });


        return Ok(new { stripeCheckoutSession.Url });
    }
}