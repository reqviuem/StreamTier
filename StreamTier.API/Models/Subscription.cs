using System.ComponentModel.DataAnnotations;
using StreamTier.API.Dtos;

namespace StreamTier.API.Models;

public class Subscription
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = null!;
    
    [Required]
    public string PlanId { get; set; } = null!;
    
    public Status Status { get; set; } 
    
    public string StripeCustomerId { get; set; }
    
    public string StripeSubscriptionId { get; set; }
    
    public DateTime CurrentPeriodStart { get; set; }
    
    public DateTime CurrentPeriodEnd { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public static Subscription FromDto(CreateSubscriptionDto dto) => new()
    {
        UserId = dto.UserId,
        PlanId = dto.PlanId,
        Status = Status.Active,
        StripeCustomerId = dto.StripeCustomerId,
        StripeSubscriptionId = dto.StripeSubscriptionId,
        CurrentPeriodStart = dto.CurrentPeriodStart,
        CurrentPeriodEnd = dto.CurrentPeriodEnd,
        CreatedAt = dto.CreatedAt
    };
}