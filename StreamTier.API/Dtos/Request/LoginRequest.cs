using System.ComponentModel.DataAnnotations;

namespace StreamTier.API.Dtos;

public class LoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}