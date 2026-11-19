using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StreamTier.API.Data;
using StreamTier.API.Exceptions;
using StreamTier.API.Models;
using Xunit;
using SubscriptionService = StreamTier.API.Services.SubscriptionService.SubscriptionService;

namespace StreamTierUnitTests;

public class SubscriptionServiceTest : IDisposable
{
    private const string UserId = TestDatabase.UserId;

    private static readonly DateTime PeriodStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PeriodEnd = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();
    private readonly AppDbContext _db;
    private readonly SubscriptionService _service;

    public SubscriptionServiceTest()
    {
        _db = _database.CreateContext();
        _service = new SubscriptionService(_db, NullLogger<SubscriptionService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _database.Dispose();
    }

    [Fact]
    public async Task CreateFreeSubscription_AddsActiveFreePlan()
    {
        await _service.CreateFreeSubscriptionAsync(UserId);

        var saved = Assert.Single(await _database.GetSubscriptionsAsync());
        Assert.Equal("FreePlan", saved.PlanId);
        Assert.Equal(Status.Active, saved.Status);
        Assert.Null(saved.StripeSubscriptionId);
    }

    [Fact]
    public async Task ActivatePaidSubscription_UserOnFreePlan_CancelsFreeAndAddsPaid()
    {
        await _database.AddSubscriptionAsync("FreePlan", Status.Active);

        await _service.ActivatePaidSubscriptionAsync(TestDatabase.NewSubscription("PremiumPlan", stripeId: "sub_1"));

        var all = await _database.GetSubscriptionsAsync();
        Assert.Equal(2, all.Count);
        Assert.Equal(Status.Canceled, all.Single(s => s.PlanId == "FreePlan").Status);
        Assert.Equal(Status.Active, all.Single(s => s.PlanId == "PremiumPlan").Status);
    }

    [Theory]
    [InlineData(Status.Active)]
    [InlineData(Status.PastDue)]
    public async Task ActivatePaidSubscription_UserAlreadyPaying_ThrowsAndChangesNothing(Status currentStatus)
    {
        await _database.AddSubscriptionAsync("StandardPlan", currentStatus, "sub_old");

        var ex = await Assert.ThrowsAsync<PermanentWebhookException>(() =>
            _service.ActivatePaidSubscriptionAsync(TestDatabase.NewSubscription("PremiumPlan", stripeId: "sub_new")));
        Assert.Contains("already has an active paid subscription", ex.Message);

        var saved = Assert.Single(await _database.GetSubscriptionsAsync());
        Assert.Equal("sub_old", saved.StripeSubscriptionId);
        Assert.Equal(currentStatus, saved.Status);
    }
    
    [Fact]
    public async Task ActivatePaidSubscription_SameSubscriptionDeliveredTwice_IsNoOp()
    {
        await _database.AddSubscriptionAsync("PremiumPlan", Status.Active, "sub_1");

        await _service.ActivatePaidSubscriptionAsync(TestDatabase.NewSubscription("PremiumPlan", stripeId: "sub_1"));

        var saved = Assert.Single(await _database.GetSubscriptionsAsync());
        Assert.Equal("sub_1", saved.StripeSubscriptionId);
    }

    [Fact]
    public async Task ActivatePaidSubscription_OldPaidIsCanceled_DoesNotBlockNewPayment()
    {
        await _database.AddSubscriptionAsync("StandardPlan", Status.Canceled, "sub_old");
        await _database.AddSubscriptionAsync("FreePlan", Status.Active);

        await _service.ActivatePaidSubscriptionAsync(TestDatabase.NewSubscription("PremiumPlan", stripeId: "sub_new"));

        var current = (await _database.GetSubscriptionsAsync()).Where(s => s.Status == Status.Active);
        Assert.Equal("PremiumPlan", Assert.Single(current).PlanId);
    }

    [Fact]
    public async Task ActivatePaidSubscription_InsertFails_RollsBackFreePlanCancellation()
    {
        await _database.AddSubscriptionAsync("FreePlan", Status.Active);
        // fail after the free plan was canceled.
        await _database.AddSubscriptionAsync("StandardPlan", Status.Canceled, "sub_taken");

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            _service.ActivatePaidSubscriptionAsync(TestDatabase.NewSubscription("PremiumPlan", stripeId: "sub_taken")));

        var all = await _database.GetSubscriptionsAsync();
        Assert.Equal(2, all.Count);
        Assert.Equal(Status.Active, all.Single(s => s.PlanId == "FreePlan").Status);
    }

    [Fact]
    public async Task Database_RejectsSecondCurrentSubscriptionForSameUser()
    {
        await _database.AddSubscriptionAsync("FreePlan", Status.Active);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            _database.AddSubscriptionAsync("PremiumPlan", Status.PastDue));
    }

    [Fact]
    public async Task Downgrade_PaidSubscription_CancelsItAndAddsFreePlan()
    {
        await _database.AddSubscriptionAsync("PremiumPlan", Status.PastDue, "sub_1");

        await _service.DowngradeSubscriptionAsync("sub_1");

        var all = await _database.GetSubscriptionsAsync();
        Assert.Equal(2, all.Count);
        Assert.Equal(Status.Canceled, all.Single(s => s.PlanId == "PremiumPlan").Status);
        Assert.Equal(Status.Active, all.Single(s => s.PlanId == "FreePlan").Status);
    }

    [Fact]
    public async Task Downgrade_DeliveredTwice_AddsOnlyOneFreePlan()
    {
        await _database.AddSubscriptionAsync("PremiumPlan", Status.Active, "sub_1");

        await _service.DowngradeSubscriptionAsync("sub_1");
        await _service.DowngradeSubscriptionAsync("sub_1");

        Assert.Single(await _database.GetSubscriptionsAsync(), s => s.PlanId == "FreePlan");
    }

    [Fact]
    public async Task Downgrade_UnknownSubscription_ThrowsPermanent()
    {
        await Assert.ThrowsAsync<PermanentWebhookException>(() => _service.DowngradeSubscriptionAsync("sub_unknown"));
    }

    [Theory]
    [InlineData("active", Status.Active)]
    [InlineData("past_due", Status.PastDue)]
    public async Task Sync_MapsStripeStatus(string stripeStatus, Status expected)
    {
        var initial = expected == Status.Active ? Status.PastDue : Status.Active;
        await _database.AddSubscriptionAsync("PremiumPlan", initial, "sub_1");

        await _service.SyncFromStripeAsync("sub_1", stripeStatus, null, PeriodStart, PeriodEnd);

        Assert.Equal(expected, Assert.Single(await _database.GetSubscriptionsAsync()).Status);
    }

    [Fact]
    public async Task Sync_UpdatesBillingPeriod()
    {
        await _database.AddSubscriptionAsync("PremiumPlan", Status.Active, "sub_1");

        await _service.SyncFromStripeAsync("sub_1", "active", null, PeriodStart, PeriodEnd);

        var saved = Assert.Single(await _database.GetSubscriptionsAsync());
        Assert.Equal(PeriodStart, saved.CurrentPeriodStart);
        Assert.Equal(PeriodEnd, saved.CurrentPeriodEnd);
    }

    [Fact]
    public async Task Sync_EventFromEarlierPeriod_IsIgnored()
    {
        await _database.AddSubscriptionAsync("PremiumPlan", Status.Active, "sub_1", periodEnd: PeriodEnd);

        await _service.SyncFromStripeAsync("sub_1", "past_due", null, PeriodStart.AddMonths(-1), PeriodStart);

        var saved = Assert.Single(await _database.GetSubscriptionsAsync());
        Assert.Equal(Status.Active, saved.Status);
        Assert.Equal(PeriodEnd, saved.CurrentPeriodEnd);
    }

    [Fact]
    public async Task Sync_SamePeriod_IsApplied()
    {
        await _database.AddSubscriptionAsync("PremiumPlan", Status.Active, "sub_1", periodEnd: PeriodEnd);

        await _service.SyncFromStripeAsync("sub_1", "past_due", null, PeriodStart, PeriodEnd);

        Assert.Equal(Status.PastDue, Assert.Single(await _database.GetSubscriptionsAsync()).Status);
    }

    [Fact]
    public async Task Sync_CanceledSubscription_IsNotRevived()
    {
        await _database.AddSubscriptionAsync("PremiumPlan", Status.Canceled, "sub_1");

        await _service.SyncFromStripeAsync("sub_1", "active", null, PeriodStart, PeriodEnd);

        Assert.Equal(Status.Canceled, Assert.Single(await _database.GetSubscriptionsAsync()).Status);
    }

    [Fact]
    public async Task Sync_UnknownSubscription_IsIgnored()
    {
        await _service.SyncFromStripeAsync("sub_unknown", "active", null, PeriodStart, PeriodEnd);

        Assert.Empty(await _database.GetSubscriptionsAsync());
    }

    [Fact]
    public async Task Sync_NewPrice_MovesSubscriptionToThatPlan()
    {
        await _database.SetStripePriceAsync("PremiumPlan", "price_premium");
        await _database.AddSubscriptionAsync("StandardPlan", Status.Active, "sub_1");

        await _service.SyncFromStripeAsync("sub_1", "active", "price_premium", PeriodStart, PeriodEnd);

        Assert.Equal("PremiumPlan", Assert.Single(await _database.GetSubscriptionsAsync()).PlanId);
    }

    [Fact]
    public async Task OnPaymentSucceeded_PastDue_BecomesActive()
    {
        await _database.AddSubscriptionAsync("PremiumPlan", Status.PastDue, "sub_1");

        await _service.OnPaymentSucceeded("sub_1");

        Assert.Equal(Status.Active, Assert.Single(await _database.GetSubscriptionsAsync()).Status);
    }

    [Fact]
    public async Task OnPaymentSucceeded_Canceled_StaysCanceled()
    {
        await _database.AddSubscriptionAsync("PremiumPlan", Status.Canceled, "sub_1");

        await _service.OnPaymentSucceeded("sub_1");

        Assert.Equal(Status.Canceled, Assert.Single(await _database.GetSubscriptionsAsync()).Status);
    }

    [Fact]
    public async Task GetStripeSubscriptionId_PaidSubscription_ReturnsId()
    {
        await _database.AddSubscriptionAsync("PremiumPlan", Status.PastDue, "sub_1");

        Assert.Equal("sub_1", await _service.GetStripeSubscriptionId(UserId));
    }

    [Fact]
    public async Task GetStripeSubscriptionId_OnlyFreeAndCanceledPaid_ReturnsNull()
    {
        await _database.AddSubscriptionAsync("PremiumPlan", Status.Canceled, "sub_1");
        await _database.AddSubscriptionAsync("FreePlan", Status.Active);

        Assert.Null(await _service.GetStripeSubscriptionId(UserId));
    }

    [Fact]
    public async Task GetByStripeSubscriptionId_ReturnsMatchOrNull()
    {
        var subscription = await _database.AddSubscriptionAsync("PremiumPlan", Status.Active, "sub_1");

        Assert.Equal(subscription.Id, (await _service.GetByStripeSubscriptionId("sub_1"))?.Id);
        Assert.Null(await _service.GetByStripeSubscriptionId("sub_unknown"));
    }
}
