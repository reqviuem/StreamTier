namespace StreamTier.API.Dtos.Requests;

public record StripeCheckoutRequestDto
{
    public required string PlanId { get; set; }
}