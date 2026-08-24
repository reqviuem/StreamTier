
using StreamTier.API.Models;

namespace StreamTier.API.Services.StripeService;

public interface IStripeService
{
    Task<SubscriptionPlan?> GetActivePlanByIdAsync(string plan);
}