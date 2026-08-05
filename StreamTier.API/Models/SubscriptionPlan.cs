using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace StreamTier.API.Models;

public class SubscriptionPlan
{
    [Required] public string Id { get; set; } = null!;

    [Required] public string Name { get; set; } = null!;
    
    [Range(0, int.MaxValue)]
    public int PriceInCents { get; set; }

    [Required] public string Currency { get; set; } = null!;

    [Required] public string BillingInterval { get; set; } = null!;
    
    
    [Range(1,5)]
    public int MaxScreens { get; set; }

    [Required] public string MaxResolution { get; set; } = null!;

    [Required] public string StripePriceId { get; set; } = null!;
    
    public bool IsActive { get; set; }
}