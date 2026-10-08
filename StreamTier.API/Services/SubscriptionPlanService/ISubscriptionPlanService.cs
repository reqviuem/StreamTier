using StreamTier.API.Dtos.Responses;
using StreamTier.API.Models;

namespace StreamTier.API.Services.SubscriptionPlanService;

public interface ISubscriptionPlanService
{
    Task SetStripePriceId(string id, string priceId);
    Task<IEnumerable<PlanResponseDto>> GetAvailablePlans();
    Task<SubscriptionPlan?> GetActivePlanByIdAsync(string planId);
}