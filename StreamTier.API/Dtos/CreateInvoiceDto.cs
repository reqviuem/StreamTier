namespace StreamTier.API.Dtos;

public record CreateInvoiceDto
{
    public required string UserId { get; init; }
    public required long AmountPaidInCents { get; init; }
    public required string Currency { get; init; }
    public required string StripeInvoiceId { get; init; }
    public required string SubscriptionId { get; init; }
}