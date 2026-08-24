using Microsoft.EntityFrameworkCore;
using StreamTier.API.Data;
using StreamTier.API.Models;

namespace StreamTier.API.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _appDbContext;

    public SubscriptionService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }
    

    public async Task<bool> IsActive(string id)
    {
        var subscription = await _appDbContext.Subscriptions.FirstOrDefaultAsync(s => s.UserId == id);

        if (subscription != null && subscription.Status == Status.Active)
        {
            return true;
        }

        return false;
    }
}