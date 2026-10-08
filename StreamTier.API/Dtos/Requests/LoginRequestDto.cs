namespace StreamTier.API.Dtos.Requests;

public record LoginRequestDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}