using System.ComponentModel.DataAnnotations;

namespace StreamTier.API.Models;

public class Subscription
{
    // TO DO
    // Subscriptions and Invoices currently store UserId as Guid,
    // but ASP.NET Identity’s default user id is string.
    // Since your migration creates AspNetUsers.Id as string,
    // you will eventually want your own subscription/invoice user ids to match that.
    
    public Guid Id { get; set; }

    public string UserId { get; set; } = null!;
    
    [Required]
    public string PlanId { get; set; } = null!;
    
    public Status Status { get; set; } 
    
    public string? StripeCustomerId { get; set; }
    
    public string? StripeSubscriptionId { get; set; }
    
    public DateTime? CurrentPeriodStart { get; set; }
    
    public DateTime? CurrentPeriodEnd { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}