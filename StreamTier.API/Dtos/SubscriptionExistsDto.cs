namespace StreamTier.API.Dtos;

public record SubscriptionExistsDto
{
    public required Guid Id { get; init; }
}