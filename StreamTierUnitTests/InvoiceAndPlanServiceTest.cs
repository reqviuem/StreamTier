using Microsoft.EntityFrameworkCore;
using StreamTier.API.Data;
using StreamTier.API.Dtos;
using StreamTier.API.Services.InvoiceService;
using StreamTier.API.Services.SubscriptionPlanService;
using Xunit;

namespace StreamTierUnitTests;

public class InvoiceAndPlanServiceTest : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly AppDbContext _db;

    public InvoiceAndPlanServiceTest()
    {
        _db = _database.CreateContext();
    }

    public void Dispose()
    {
        _db.Dispose();
        _database.Dispose();
    }

    private static CreateInvoiceDto NewInvoice(string userId = TestDatabase.UserId) => new()
    {
        UserId = userId,
        AmountPaidInCents = 1999,
        Currency = "eur",
        StripeInvoiceId = "in_1",
        StripeSubscriptionId = "sub_1"
    };

    [Fact]
    public async Task SaveInvoice_CanBeFoundByStripeId()
    {
        var service = new InvoiceService(_db);

        await service.SaveAsync(NewInvoice());

        Assert.NotNull(await service.GetByStripeInvoiceId("in_1"));
        Assert.Null(await service.GetByStripeInvoiceId("in_unknown"));
    }

    [Fact]
    public async Task SaveInvoice_SameStripeIdTwice_IsRejectedByDatabase()
    {
        await new InvoiceService(_db).SaveAsync(NewInvoice());

        await using var other = _database.CreateContext();

        await Assert.ThrowsAsync<DbUpdateException>(() => new InvoiceService(other).SaveAsync(NewInvoice()));
    }

    [Fact]
    public async Task GetAvailablePlans_ExcludesFreeAndInactive()
    {
        var standard = await _db.SubscriptionPlans.SingleAsync(p => p.Id == "StandardPlan");
        standard.IsActive = false;
        await _db.SaveChangesAsync();

        var plans = await new SubscriptionPlanService(_db).GetAvailablePlans();

        Assert.Equal("PremiumPlan", Assert.Single(plans).Id);
    }

    [Fact]
    public async Task GetActivePlanById_InactiveOrUnknown_ReturnsNull()
    {
        var standard = await _db.SubscriptionPlans.SingleAsync(p => p.Id == "StandardPlan");
        standard.IsActive = false;
        await _db.SaveChangesAsync();

        var service = new SubscriptionPlanService(_db);

        Assert.NotNull(await service.GetActivePlanByIdAsync("PremiumPlan"));
        Assert.Null(await service.GetActivePlanByIdAsync("StandardPlan"));
        Assert.Null(await service.GetActivePlanByIdAsync("UnknownPlan"));
    }

    [Fact]
    public async Task SetStripePriceId_StoresPrice()
    {
        await new SubscriptionPlanService(_db).SetStripePriceId("PremiumPlan", "price_1");

        Assert.Equal("price_1", (await _db.SubscriptionPlans.SingleAsync(p => p.Id == "PremiumPlan")).StripePriceId);
    }
}
