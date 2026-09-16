namespace StreamTier.API.Dtos;

public record InvoiceExistsDto
{
    public required Guid Id { get; init; }
}