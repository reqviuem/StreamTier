using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StreamTier.API.Models;

namespace StreamTier.API.Data;

public class AppDbContext : IdentityDbContext<User>
{
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    public DbSet<Invoice> Invoices => Set<Invoice>();
    

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.ToTable("Plans");

            entity.HasKey(n => n.Id);

            entity.Property(n => n.Name).IsRequired();
            entity.Property(n => n.Currency).IsRequired();
            entity.Property(n => n.PriceInCents);
            entity.Property(n => n.BillingInterval);
            entity.Property(n => n.IsActive);
            entity.Property(n => n.MaxResolution).IsRequired();
            entity.Property(n => n.MaxScreens);
            entity.Property(n => n.StripePriceId).IsRequired();

            entity.HasData(
                new SubscriptionPlan
                {
                    Id = "FreePlan",
                    Name = "Basic",
                    Currency = "EUR",
                    PriceInCents = 0,
                    BillingInterval = "Monthly",
                    IsActive = true,
                    MaxResolution = "QD",
                    MaxScreens = 1,
                    StripePriceId = "price_1U57gt5B4xOxmiDEcyhXD7Nw"
                },
                new SubscriptionPlan
                {
                    Id = "StandardPlan",
                    Name = "Standard",
                    Currency = "EUR",
                    PriceInCents = 999,
                    BillingInterval = "Monthly",
                    IsActive = true,
                    MaxResolution = "HD",
                    MaxScreens = 2,
                    StripePriceId = "price_1U57hw5B4xOxmiDEqD1X3j4r"
                },
                new SubscriptionPlan
                {
                    Id = "PremiumPlan",
                    Name = "Premium",
                    Currency = "EUR",
                    PriceInCents = 1999,
                    BillingInterval = "Monthly",
                    IsActive = true,
                    MaxResolution = "4K",
                    MaxScreens = 4,
                    StripePriceId = "price_1U57ic5B4xOxmiDElOLXK9fh"
                }
            );
        });
    }
}