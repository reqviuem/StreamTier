using StreamTier.API.Dtos;

namespace StreamTier.API.Models;

public class Invoice
{
    public Guid Id { get; set; }

    public required string UserId { get; set; } = null!;

    public required string SubscriptionId { get; set; } = null!;
    
    public required string StripeInvoiceId { get; set; } = null!;

    public required long AmountPaidInCents { get; set; }

    public required string Currency { get; set; } = null!;

    
    public static Invoice FromDto(CreateInvoiceDto dto) => new()
    {
        UserId = dto.UserId,
        AmountPaidInCents = dto.AmountPaidInCents,
        Currency = dto.Currency,
        StripeInvoiceId = dto.StripeInvoiceId,
        SubscriptionId = dto.SubscriptionId
    };
}

