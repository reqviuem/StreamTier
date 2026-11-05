using StreamTier.API.Dtos;

namespace StreamTier.API.Models;

public class Subscription
{
    public Guid Id { get; set; }

    public required string UserId { get; set; }

    public required string PlanId { get; set; }

    public required Status Status { get; set; }

    public  string? StripeCustomerId { get; set; }

    public string? StripeSubscriptionId { get; set; }

    public required DateTime CurrentPeriodStart { get; set; }

    public DateTime? CurrentPeriodEnd { get; set; }

    public required DateTime CreatedAt { get; set; }

    public static Subscription FromDto(CreateSubscriptionDto dto) => new()
    {
        UserId = dto.UserId,
        PlanId = dto.PlanId,
        Status = Status.Active,
        StripeCustomerId = dto.StripeCustomerId,
        StripeSubscriptionId = dto.StripeSubscriptionId,
        CurrentPeriodStart = dto.CurrentPeriodStart,
        CurrentPeriodEnd = dto.CurrentPeriodEnd,
        CreatedAt = dto.CreatedAt
    };

    // Null - Stripe's status has no local equivalent
    public static Status? StatusFromStripe(string stripeStatus) => stripeStatus switch
    {
        "active" or "trialing" => Status.Active,
        "past_due" or "unpaid" or "paused" or "incomplete" => Status.PastDue,
        _ => null
    };
}