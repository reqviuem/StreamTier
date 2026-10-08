using Microsoft.EntityFrameworkCore;
using StreamTier.API.Data;
using StreamTier.API.Dtos.Responses;
using StreamTier.API.Models;

namespace StreamTier.API.Services.SubscriptionPlanService;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly AppDbContext _dbContext;

    public SubscriptionPlanService(AppDbContext appDbContext)
    {
        _dbContext = appDbContext;
    }

    public async Task<IEnumerable<PlanResponseDto>> GetAvailablePlans()
    {
        var plans = await _dbContext.SubscriptionPlans.Where(p => p.IsActive && p.Id != "FreePlan")
            .Select(p => new PlanResponseDto
            {
                Id = p.Id,
                BillingInterval = p.BillingInterval,
                Currency = p.Currency,
                MaxResolution = p.MaxResolution,
                MaxScreens = p.MaxScreens,
                Name = p.Name,
                PriceInCents = p.PriceInCents
            }).ToListAsync();

        return plans;
    }

    public async Task SetStripePriceId(string planId, string priceId)
    {
        var planRow = await _dbContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId)
                      ?? throw new InvalidOperationException("Plan not found");

        planRow.StripePriceId = priceId;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<SubscriptionPlan?> GetActivePlanByIdAsync(string planId)
    {
        return await _dbContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);
    }
}