using Microsoft.AspNetCore.Mvc;
using StreamTier.API.Dtos.Responses;

namespace StreamTier.API;

[ApiController]
public class TierController : ControllerBase
{
    [HttpGet]
    [Route("/plans")]
    public IActionResult GetPlans()
    {
        return Ok();
    }
}