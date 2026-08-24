using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamTier.API.Dtos.Responses;
using StreamTier.API.Services;
using StreamTier.API.Services.PlanService;

namespace StreamTier.API;

[ApiController]
public class TierController : ControllerBase
{
    private readonly ISubscriptionPlanService _subscriptionPlanService;

    public TierController(ISubscriptionPlanService subscriptionPlanService)
    {
        _subscriptionPlanService = subscriptionPlanService;
    }
    
    
    [Authorize]
    [HttpGet]
    [Route("/plans")]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _subscriptionPlanService.GetAvailablePlans();
        return Ok(plans);
    }
}