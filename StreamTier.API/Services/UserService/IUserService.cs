using Microsoft.AspNetCore.Identity;
using StreamTier.API.Models;
using Stripe;
using Stripe.Checkout;

namespace StreamTier.API.Services.UserService;

public interface IUserService
{
    Task<string> GetStripeCustomerIdAsync(string stripeCustomerId, string userId);
    Task UpdateCustomerByIdAsync(Event stripeSession);
    
    Task<IdentityResult> CreateAsync(User user, string password);
    Task<User?> FindByEmailAsync(string email);
    Task<bool> CheckPasswordAsync(User user, string password);
    Task<List<User>> GetAllUsersAsync();
}