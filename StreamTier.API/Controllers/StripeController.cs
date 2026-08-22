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


        var plan = await _service.GetActivePlanByIdAsync(stripeCheckoutRequest.PlanId);

        if (plan is null)
        {
            return BadRequest("Plan not found, Try again");
        }

        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

        var stripeSessionService = new SessionService();
        var stripeCheckoutSession = await stripeSessionService.CreateAsync(new SessionCreateOptions
        {
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId!,
                ["planId"] = plan.Id
            },
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