using StreamTier.API.Models;

namespace StreamTier.API.Dtos;

public record CheckoutSubscriptionDto
{
    public Guid Id { get; set; }

    public required string UserId { get; set; } = null!;
    
    public required string PlanId { get; set; } = null!;
    
    public required Status Status { get; set; } 
    
    public required string? StripeCustomerId { get; set; }
    
    public required string? StripeSubscriptionId { get; set; }
    
    public required DateTime? CurrentPeriodStart { get; set; }
    
    public required DateTime? CurrentPeriodEnd { get; set; }
    
    public required DateTime CreatedAt { get; set; }
    
    public  DateTime UpdatedAt { get; set; }
}
