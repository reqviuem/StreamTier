using Microsoft.EntityFrameworkCore;
using StreamTier.API.Models;

namespace StreamTier.API.Data;

public class AppDbContext : DbContext
{
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

}