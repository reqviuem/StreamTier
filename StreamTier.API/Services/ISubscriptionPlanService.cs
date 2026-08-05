using StreamTier.API.Dtos.Responses;

namespace StreamTier.API.Services;

public interface ISubscriptionPlanService
{
    Task<IEnumerable<PlanResponseDto>> GetAvailablePlans();
}