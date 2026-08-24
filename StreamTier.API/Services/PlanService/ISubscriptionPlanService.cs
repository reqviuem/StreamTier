using StreamTier.API.Dtos.Responses;

namespace StreamTier.API.Services.PlanService;

public interface ISubscriptionPlanService
{
    Task<IEnumerable<PlanResponseDto>> GetAvailablePlans();
}