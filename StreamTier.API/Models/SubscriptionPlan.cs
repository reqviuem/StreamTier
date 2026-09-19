
namespace StreamTier.API.Models;

public class SubscriptionPlan
{
    public string Id { get; set; } = null!;

    public required string Name { get; set; } = null!;

    public required int PriceInCents { get; set; }

    public required string Currency { get; set; } = null!;

    public  required string BillingInterval { get; set; } = null!;
    
    public required int MaxScreens { get; set; }

    public required string MaxResolution { get; set; } = null!;

    public required string StripePriceId { get; set; } = null!;

    public required bool IsActive { get; set; }
}