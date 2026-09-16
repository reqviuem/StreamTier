using StreamTier.API.Models;

namespace StreamTier.API.Dtos;

public record CreateSubscriptionDto
{
    public required string UserId { get; init; }
    public required string PlanId { get; init; }
    public required string StripeCustomerId { get; init; }
    public required string StripeSubscriptionId { get; init; }
    public required DateTime CurrentPeriodStart { get; init; }
    public required DateTime CurrentPeriodEnd { get; init; }
    public required DateTime CreatedAt { get; init; }
    
    public required Status Status { get; init; }
}
