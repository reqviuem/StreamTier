using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamTier.API.Dtos;
using StreamTier.API.Dtos.Request;
using StreamTier.API.Services.StripeService;
using StreamTier.API.Services.SubscriptionService;
using StreamTier.API.Services.UserService;
using Stripe;
using Stripe.Checkout;

namespace StreamTier.API;

[ApiController]
public class StripeController : ControllerBase
{
    private readonly IStripeService _service;

    private readonly IConfiguration _config;

    private readonly IUSerService _uSerService;

    private readonly ISubscriptionService _subscriptionService;

    public StripeController(IStripeService service, IConfiguration config, IUSerService uSerService, ISubscriptionService subscriptionService)
    {
        _service = service;
        _config = config;
        _uSerService = uSerService;
        _subscriptionService = subscriptionService;
    }


    [Authorize]
    [HttpPost]
    [Route("/checkout/session")]
    public async Task<IActionResult> CheckoutSession(StripeCheckoutRequestDto stripeCheckoutRequestDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        
        var plan = await _service.GetActivePlanByIdAsync(stripeCheckoutRequestDto.PlanId);

        if (plan is null)
        {
            return BadRequest("Plan not found, Try again");
        }

        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

        // if (await _subscriptionService.IsActive(userId))
        // {
        //     return BadRequest($"{userEmail} already has an active subscription");
        // }
        
        var stripeSessionService = new SessionService();
        var stripeCheckoutSession = await stripeSessionService.CreateAsync(new SessionCreateOptions
        {
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId!,
                ["planId"] = plan.Id
            },
            
            Mode = "subscription",
            SubscriptionData = new SessionSubscriptionDataOptions()
            {
              Metadata  = new Dictionary<string, string>
              {
                  {"userId", userId!}
              }
            },
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