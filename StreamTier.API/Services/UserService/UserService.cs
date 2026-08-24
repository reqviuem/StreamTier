using Microsoft.EntityFrameworkCore;
using StreamTier.API.Data;

namespace StreamTier.API.Services.UserService;

public class UserService : IUSerService
{
    private readonly AppDbContext _appDbContext;
    
    public UserService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<string> GetStripeCustomerId(string stripeCustomerId, string userId)
    {
        var user = await _appDbContext.Users
            .Where(s => s.Id == userId)
            .FirstOrDefaultAsync();

        user?.StripeCustomerId = stripeCustomerId;

        await _appDbContext.SaveChangesAsync();

        return stripeCustomerId;
    }
}