using System.ComponentModel.DataAnnotations;

namespace StreamTier.API.Dtos;

public record LoginRequestDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}