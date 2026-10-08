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

        modelBuilder.Entity<Invoice>(entity => { entity.HasIndex(i => i.StripeInvoiceId).IsUnique(); });

        modelBuilder.Entity<Subscription>(entity => { entity.HasIndex(i => i.StripeSubscriptionId).IsUnique(); });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.ToTable("Plans");

            entity.HasKey(n => n.Id);

            entity.Property(n => n.Name).IsRequired();
            entity.Property(n => n.Currency).IsRequired();
            entity.Property(n => n.PriceInCents).IsRequired();
            entity.Property(n => n.BillingInterval).IsRequired();
            entity.Property(n => n.IsActive).IsRequired();
            entity.Property(n => n.MaxResolution).IsRequired();
            entity.Property(n => n.MaxScreens).IsRequired();
            entity.Property(n => n.StripePriceId);

            entity.HasData(
                new SubscriptionPlan
                {
                    Id = "FreePlan",
                    Name = "Free",
                    Currency = "EUR",
                    PriceInCents = 0,
                    BillingInterval = "Monthly",
                    IsActive = true,
                    MaxResolution = "SD",
                    MaxScreens = 1
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
                    MaxScreens = 2
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
                    MaxScreens = 4
                }
            );
        });
    }
}