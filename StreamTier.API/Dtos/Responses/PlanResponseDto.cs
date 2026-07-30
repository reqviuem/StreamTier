namespace StreamTier.API.Dtos.Responses;

public class PlanResponseDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    public required string Currency { get; set; }

    public required int PriceInCents { get; set; }
    public required string BillingInterval { get; set; }
    
    public required int MaxScreens { get; set; }
    
    public required string MaxResolution { get; set; }
    
}