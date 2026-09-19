namespace StreamTier.API.Dtos.Request;

public record StripeCheckoutRequestDto
{
    public required string PlanId { get; set; } = null!;
}