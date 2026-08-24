using Microsoft.EntityFrameworkCore;
using StreamTier.API.Data;
using StreamTier.API.Dtos.Responses;

namespace StreamTier.API.Services.PlanService;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly AppDbContext _dbContext;

    public SubscriptionPlanService(AppDbContext appDbContext)
    {
        _dbContext = appDbContext;
    }

    public async Task<IEnumerable<PlanResponseDto>> GetAvailablePlans()
    {
        var plans = await _dbContext.SubscriptionPlans
            .Select(note => new PlanResponseDto
            {
                Id = note.Id,
                BillingInterval = note.BillingInterval,
                Currency = note.Currency,
                MaxResolution = note.MaxResolution,
                MaxScreens = note.MaxScreens,
                Name = note.Name,
                PriceInCents = note.PriceInCents
            }).ToListAsync();

        return plans;
    }
}