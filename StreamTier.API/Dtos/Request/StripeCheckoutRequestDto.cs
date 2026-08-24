namespace StreamTier.API.Dtos;

public record StripeCheckoutRequestDto
{
    public required string PlanId { get; set; } = null!;
}