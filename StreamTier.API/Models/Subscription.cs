using System.ComponentModel.DataAnnotations;

namespace StreamTier.API.Models;

public class Subscription
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    [Required]
    public string PlanId { get; set; } = null!;
    
    [Required]
    public string Status { get; set; } = null!;
    
    public string? StripeCustomerId { get; set; }
    
    public string? StripeSubscriptionId { get; set; }
    
    public DateTime? CurrentPeriodStart { get; set; }
    
    public DateTime? CurrentPeriodEnd { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}