using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace StreamTier.API.Models;

public class User : IdentityUser
{
   public string? StripeCustomerId { get; set; }
}