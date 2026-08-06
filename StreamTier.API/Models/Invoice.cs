using System.ComponentModel.DataAnnotations;

namespace StreamTier.API.Models;

public class Invoice
{
    public Guid Id { get; set; }

    [Required]public string UserId { get; set; }
    
    [Required]public Guid SubscriptionId { get; set; }
    
    [Required] public string StripeInvoiceId { get; set; } = null!;

    [Required] public int AmountPaidInCents { get; set; }

    [Required] public string Currency { get; set; } = null!;


}