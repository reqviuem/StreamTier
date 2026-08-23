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

    private readonly IUSerService _uSerService;

    public StripeController(IStripeService service, IConfiguration config, IUSerService uSerService)
    {
        _service = service;
        _config = config;
        _uSerService = uSerService;
    }


    [Authorize]
    [HttpPost]
    [Route("/checkout/session")]
    public async Task<IActionResult> CheckoutSession(StripeCheckoutRequest stripeCheckoutRequest)
    {
        var customerService = new CustomerService();
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        
        var plan = await _service.GetActivePlanByIdAsync(stripeCheckoutRequest.PlanId);

        if (plan is null)
        {
            return BadRequest("Plan not found, Try again");
        }

        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

        var customerCreateOptions = new CustomerCreateOptions()
        {
            Email = userEmail
        };

        var stripeCustomer = await customerService.CreateAsync(customerCreateOptions);

        var stripeCustomerId = await _uSerService.GetStripeCustomerId(stripeCustomer.Id, userId);
        
        var stripeSessionService = new SessionService();
        var stripeCheckoutSession = await stripeSessionService.CreateAsync(new SessionCreateOptions
        {
            Customer = stripeCustomerId,
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