namespace StreamTier.API.Models;

public class UserToken
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required string Token { get; set; }
    public required DateTime ExpiresAt { get; set; }
}