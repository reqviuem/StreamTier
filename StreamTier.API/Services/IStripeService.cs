
using StreamTier.API.Models;

namespace StreamTier.API.Services;

public interface IStripeService
{
    Task<SubscriptionPlan?> GetActivePlanByIdAsync(string plan);
}