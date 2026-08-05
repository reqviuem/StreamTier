using System.ComponentModel.DataAnnotations;

namespace StreamTier.API.Models;

public class User
{
    public Guid Id { get; set; }
    
    [Required]public string Email  { get; set; } = null!;
    
    [Required]public string PasswordHash { get; set; } = null!;
    
    [Required] public DateTime? CreatedAt { get; set; } = null!;
}