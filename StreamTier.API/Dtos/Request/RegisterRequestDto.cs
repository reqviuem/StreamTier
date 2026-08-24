using System.ComponentModel.DataAnnotations;

namespace StreamTier.API.Dtos;

public record RegisterRequestDto
{

    public required string Email { get; set; } = null!;

    public required string Password { get; set; } = null!;
}