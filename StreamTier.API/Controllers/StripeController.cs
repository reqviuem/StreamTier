using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamTier.API.Dtos.Requests;
using StreamTier.API.Services.SubscriptionPlanService;
using StreamTier.API.Services.SubscriptionService;
using StreamTier.API.Services.UserService;
using Stripe.Checkout;

namespace StreamTier.API.Controllers;

[ApiController]
public class StripeController : ControllerBase
{
    private readonly ISubscriptionPlanService _planService;

    private readonly IConfiguration _config;

    private readonly ISubscriptionService _subscriptionService;

    private readonly IUserService _userService;

    public StripeController(ISubscriptionPlanService planService, IConfiguration config, ISubscriptionService subscriptionService, IUserService userService)
    {
        _planService = planService;
        _config = config;
        _subscriptionService = subscriptionService;
        _userService = userService;
    }

    [Authorize(Roles = "User")]
    [HttpPost]
    [Route("/checkout/session")]
    public async Task<IActionResult> CheckoutSession(StripeCheckoutRequestDto stripeCheckoutRequestDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (userId is null)
        {
            return Unauthorized();
        }

        var stripeSubscriptionId = await _subscriptionService.GetStripeSubscriptionId(userId);

        if (stripeSubscriptionId != null)
        {
            return BadRequest("User already has paid subscription!");
        }

        var plan = await _planService.GetActivePlanByIdAsync(stripeCheckoutRequestDto.PlanId);

        if (plan is null)
        {
            return BadRequest("Plan not found, Try again");
        }

        if (plan.Id == "FreePlan")
        {
            return BadRequest("The free plan cannot be purchased.");
        }

        var user = await _userService.GetUserByIdAsync(userId);

        if (user is null)
        {
            return Unauthorized();
        }

        var stripeSessionService = new SessionService();
        var stripeCheckoutSession = await stripeSessionService.CreateAsync(new SessionCreateOptions
        {
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId,
                ["planId"] = plan.Id
            },

            Mode = "subscription",
            SubscriptionData = new SessionSubscriptionDataOptions()
            {
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId }
                }
            },
            PaymentMethodTypes = ["card"],
            ClientReferenceId = userId,
            SuccessUrl = _config["Stripe:SuccessUrl"],
            CancelUrl = _config["Stripe:CancelUrl"],
            Customer = user.StripeCustomerId,
            CustomerEmail = user.StripeCustomerId is null ? userEmail : null,
            LineItems = new()
            {
                new()
                {
                    Price = plan.StripePriceId,
                    Quantity = 1
                }
            }
        });

        return Ok(new { stripeCheckoutSession.Url });
    }
}