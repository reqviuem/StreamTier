using Microsoft.EntityFrameworkCore;
using StreamTier.API.Data;

namespace StreamTier.API.Services;

public class StripeService : IStripeService
{
    private readonly AppDbContext _appContext;


    public StripeService(AppDbContext appContext)
    {
        _appContext = appContext;
    }
    
    public  async Task<bool> CheckPlan(string plan)
    {
        var foundPlan =  await _appContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == plan);

        if (foundPlan != null)
        {
            return true;
        }

        return false;
    }
}