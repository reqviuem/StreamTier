using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamTier.API.Dtos;
using StreamTier.API.Services;

namespace StreamTier.API;

[ApiController]
public class StripeController : ControllerBase
{
    private readonly IStripeService _service;
    
    

    public StripeController(IStripeService service)
    {
        _service = service;
    }
    
    [HttpPost]
    [Route("/checkout/session")]
    public async Task<IActionResult> CheckoutSession(StripeCheckoutRequest stripeCheckoutRequest)
    {
        if (!_service.CheckPlan(stripeCheckoutRequest.PlanId).Result)
        {
            return BadRequest("Plan not found, Try again");
        }

        return Ok();
    }
}