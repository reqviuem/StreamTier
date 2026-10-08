
namespace StreamTier.API.Models;

public class SubscriptionPlan
{
    public string Id { get; set; } = null!;

    public required string Name { get; set; }

    public required int PriceInCents { get; set; }

    public required string Currency { get; set; }

    public  required string BillingInterval { get; set; }

    public required int MaxScreens { get; set; }

    public required string MaxResolution { get; set; }

    public string? StripePriceId { get; set; }

    public required bool IsActive { get; set; }
}