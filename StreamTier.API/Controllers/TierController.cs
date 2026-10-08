using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamTier.API.Services.SubscriptionPlanService;

namespace StreamTier.API.Controllers;

[ApiController]
public class TierController : ControllerBase
{
    private readonly ISubscriptionPlanService _planService;

    public TierController(ISubscriptionPlanService planService)
    {
        _planService = planService;
    }

    [Authorize(Roles = "User")]
    [HttpGet]
    [Route("/plans")]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _planService.GetAvailablePlans();
        return Ok(plans);
    }
}