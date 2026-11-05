using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StreamTier.API.Data;
using StreamTier.API.Models;
using Subscription = StreamTier.API.Models.Subscription;

namespace StreamTierUnitTests;

public sealed class TestDatabase : IDisposable
{
    public const string UserId = "f601c435-61a5-42b5-85e0-30e3590d2595";

    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var db = CreateContext();
        db.Database.EnsureCreated();
        db.Users.Add(new User { Id = UserId, UserName = "test@test.com", Email = "test@test.com" });
        db.SaveChanges();
    }

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public async Task<Subscription> AddSubscriptionAsync(string planId, Status status, string? stripeId = null,
        DateTime? periodEnd = null)
    {
        await using var db = CreateContext();

        var subscription = NewSubscription(planId, status, stripeId, periodEnd);
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        return subscription;
    }

    public async Task SetStripePriceAsync(string planId, string priceId)
    {
        await using var db = CreateContext();

        var plan = await db.SubscriptionPlans.SingleAsync(p => p.Id == planId);
        plan.StripePriceId = priceId;
        await db.SaveChangesAsync();
    }

   //Reads through a new context
    public async Task<List<Subscription>> GetSubscriptionsAsync()
    {
        await using var db = CreateContext();

        return await db.Subscriptions.ToListAsync();
    }

    public async Task<User> GetUserAsync()
    {
        await using var db = CreateContext();

        return await db.Users.SingleAsync(u => u.Id == UserId);
    }

    public static Subscription NewSubscription(string planId, Status status = Status.Active, string? stripeId = null,
        DateTime? periodEnd = null) => new()
    {
        UserId = UserId,
        PlanId = planId,
        Status = status,
        StripeSubscriptionId = stripeId,
        CurrentPeriodStart = DateTime.UtcNow,
        CurrentPeriodEnd = periodEnd,
        CreatedAt = DateTime.UtcNow
    };

    public void Dispose() => _connection.Dispose();
}
