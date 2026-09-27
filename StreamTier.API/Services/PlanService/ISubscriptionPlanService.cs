using StreamTier.API.Dtos.Responses;

namespace StreamTier.API.Services.PlanService;

public interface ISubscriptionPlanService
{
    Task SetStripePriceId(string id, string priceId);
    Task<IEnumerable<PlanResponseDto>> GetAvailablePlans();
}