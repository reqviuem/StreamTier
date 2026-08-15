
namespace StreamTier.API.Services;

public interface IStripeService
{
    Task<bool> CheckPlan(string plan);
}