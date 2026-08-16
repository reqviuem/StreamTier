using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamTier.API.Dtos;
using StreamTier.API.Services;
using Stripe;
using Stripe.Checkout;
using Stripe.Terminal;

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

        if (!_service.CheckPlan(stripeCheckoutRequest.PlanId).Result)
        {
            return BadRequest("Plan not found, Try again");
        }

        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

        var priceService = new PriceService();
        var prices = priceService.List(new PriceListOptions()
        {
            Product = ""
        });

        var priceId = prices.Data[0].Id;
        
        var stripeSessionService = new SessionService();
        var stripeCheckoutSession = await stripeSessionService.CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription",
            ClientReferenceId = userId,
            SuccessUrl = _config["Stripe:SuccessUrl"],
            LineItems = new()
            {
                new()
                {
                    Price = priceId,
                    Quantity = 1
                }
            }
        });


        return Ok(new { stripeCheckoutSession.Url });
    }
}