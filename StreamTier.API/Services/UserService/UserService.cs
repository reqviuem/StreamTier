using Microsoft.AspNetCore.Identity;
using StreamTier.API.Exceptions;
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

    public async Task UpdateCustomerByIdAsync(Event stripeEvent)
    {
        var stripeSession = stripeEvent.Data.Object as Session
                            ?? throw new PermanentWebhookException("Expected Session object in Stripe event data.");

        if (!stripeSession.Metadata.TryGetValue("userId", out var userId))
            throw new PermanentWebhookException("Stripe session metadata missing 'userId'.");

        var user = await _appDbContext.Users
                       .Where(s => s.Id == userId)
                       .FirstOrDefaultAsync()
                   ?? throw new PermanentWebhookException(
                       $"No user found for Stripe session metadata userId '{stripeSession.Metadata["userId"]}'.");

        user.StripeCustomerId = stripeSession.CustomerId;

        await _appDbContext.SaveChangesAsync();
    }

    public async Task OnUserDelete(string stripeCustomerId)
    {
        var user = await _appDbContext.Users
                       .Where(s => s.StripeCustomerId == stripeCustomerId)
                       .FirstOrDefaultAsync()
                   ?? throw new PermanentWebhookException(
                       "No user found with the provided Id.");

        user.StripeCustomerId = null;

        await _appDbContext.SaveChangesAsync();
    }

    public async Task<IdentityResult> CreateAsync(User user, string password)
    {
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return result;
        }

        return await _userManager.AddToRoleAsync(user, "User");
    }

    public Task<User?> FindByEmailAsync(string email)
        => _userManager.FindByEmailAsync(email);

    public Task<User?> GetUserByIdAsync(string userId)
        => _userManager.FindByIdAsync(userId);

    public Task<bool> CheckPasswordAsync(User user, string password)
        => _userManager.CheckPasswordAsync(user, password);

    public Task<IList<string>> GetRolesAsync(User user) =>
        _userManager.GetRolesAsync(user);
}