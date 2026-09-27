namespace StreamTier.API.Dtos.Request;

public record RegisterRequestDto
{

    public required string Email { get; set; } = null!;

    public required string Password { get; set; } = null!;
}