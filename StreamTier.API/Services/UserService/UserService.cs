using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StreamTier.API.Data;
using StreamTier.API.Models;
using Stripe;
using Stripe.Checkout;

namespace StreamTier.API.Services.UserService;

public class UserService : IUserService
{
    private readonly AppDbContext _appDbContext;
    private readonly UserManager<User> _userManager;

    public UserService(AppDbContext appDbContext, UserManager<User> userManager)
    {
        _appDbContext = appDbContext;
        _userManager = userManager;
    }

    public async Task<string> GetStripeCustomerIdAsync(string stripeCustomerId, string userId)
    {
        var user = await _appDbContext.Users
            .Where(s => s.Id == userId)
            .FirstOrDefaultAsync();

        user?.StripeCustomerId = stripeCustomerId;

        await _appDbContext.SaveChangesAsync();

        return stripeCustomerId;
    }

    public async Task UpdateCustomerByIdAsync(Event stripeEvent)
    {
        var stripeSession = stripeEvent.Data.Object as Session
                            ?? throw new InvalidOperationException("Expected Session object in Stripe event data.");


        if (!stripeSession.Metadata.TryGetValue("userId", out var userId))
            throw new InvalidOperationException("Stripe session metadata missing 'userId'.");
        
        var user = await _appDbContext.Users
                       .Where(s => s.Id == userId)
                       .FirstOrDefaultAsync()
                   ?? throw new InvalidOperationException(
                       $"No user found for Stripe session metadata userId '{stripeSession.Metadata["userId"]}'.");

        user.StripeCustomerId = stripeSession.CustomerId;

        await _appDbContext.SaveChangesAsync();
    }
    
    public Task<IdentityResult> CreateAsync(User user, string password)
        => _userManager.CreateAsync(user, password);

    public Task<User?> FindByEmailAsync(string email)
        => _userManager.FindByEmailAsync(email);

    public Task<bool> CheckPasswordAsync(User user, string password)
        => _userManager.CheckPasswordAsync(user, password);

    public Task<List<User>> GetAllUsersAsync()
        => _userManager.Users.ToListAsync();
}