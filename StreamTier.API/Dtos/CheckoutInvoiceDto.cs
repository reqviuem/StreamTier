namespace StreamTier.API.Dtos;

public record CheckoutInvoiceDto
{
    public  Guid Id { get; set; }

    public string UserId { get; set; } = null!;

    public Guid SubscriptionId { get; set; }

    public string StripeInvoiceId { get; set; } = null!;

    public int AmountPaidInCents { get; set; }

    public string Currency { get; set; } = null!;
}