using System.ComponentModel.DataAnnotations;

namespace StreamTier.API.Dtos;

public class RegisterRequest
{

    [Required] [EmailAddress]  public required string Email { get; set; } = null!;

    [Required] public string Password { get; set; } = null!;
}