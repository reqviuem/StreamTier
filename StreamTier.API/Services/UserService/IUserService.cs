using Microsoft.AspNetCore.Identity;
using StreamTier.API.Models;
using Stripe;

namespace StreamTier.API.Services.UserService;

public interface IUserService
{
    Task UpdateCustomerByIdAsync(Event stripeEvent);

    Task<IdentityResult> CreateAsync(User user, string password);
    Task<User?> FindByEmailAsync(string email);
    Task<User?> GetUserByIdAsync(string userId);
    Task<bool> CheckPasswordAsync(User user, string password);

    Task<IList<string>> GetRolesAsync(User user);
    Task OnUserDelete(string stripeCustomerId);
}