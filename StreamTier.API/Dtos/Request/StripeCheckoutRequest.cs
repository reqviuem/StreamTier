namespace StreamTier.API.Dtos;

public record StripeCheckoutRequest
{
    public string PlanId { get; set; } = null!;
}