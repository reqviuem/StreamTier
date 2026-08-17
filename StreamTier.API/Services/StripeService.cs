using Microsoft.EntityFrameworkCore;
using StreamTier.API.Data;
using StreamTier.API.Models;

namespace StreamTier.API.Services;

public class StripeService : IStripeService
{
    private readonly AppDbContext _appContext;


    public StripeService(AppDbContext appContext)
    {
        _appContext = appContext;
    }
    
    public async Task<SubscriptionPlan?> GetActivePlanByIdAsync(string plan)
    {
        var foundPlan =  await _appContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == plan && p.IsActive);

        return foundPlan;
    }
    
    
}