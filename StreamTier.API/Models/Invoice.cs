using System.ComponentModel.DataAnnotations;
using StreamTier.API.Dtos;

namespace StreamTier.API.Models;

public class Invoice
{
    public Guid Id { get; set; }

    [Required] public string UserId { get; set; } = null!;

    [Required] public string SubscriptionId { get; set; } = null!;
    
    [Required] public string StripeInvoiceId { get; set; } = null!;

    [Required] public long AmountPaidInCents { get; set; }

    [Required] public string Currency { get; set; } = null!;

    
    public static Invoice FromDto(CreateInvoiceDto dto) => new()
    {
        UserId = dto.UserId,
        AmountPaidInCents = dto.AmountPaidInCents,
        Currency = dto.Currency,
        StripeInvoiceId = dto.StripeInvoiceId,
        SubscriptionId = dto.SubscriptionId
    };
}

