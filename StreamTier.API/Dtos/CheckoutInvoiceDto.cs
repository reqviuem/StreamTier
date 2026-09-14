namespace StreamTier.API.Dtos;

public record CheckoutInvoiceDto
{
    public required string UserId { get; set; } = null!;

    public required string SubscriptionId { get; set; } = null!;

    public required string StripeInvoiceId { get; set; } = null!;

    public required long AmountPaidInCents { get; set; }

    public required string Currency { get; set; } = null!;
}