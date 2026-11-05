using Microsoft.AspNetCore.Identity;
using Moq;
using StreamTier.API.Data;
using StreamTier.API.Models;
using StreamTier.API.Services.UserService;
using Stripe;
using Stripe.Checkout;
using Xunit;

namespace StreamTierUnitTests;

public class UserTests : IDisposable
{
    private const string UserId = TestDatabase.UserId;

    private readonly TestDatabase _database = new();
    private readonly AppDbContext _db;
    private readonly Mock<UserManager<User>> _userManager = new(
        Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
    private readonly UserService _service;

    public UserTests()
    {
        _db = _database.CreateContext();
        _service = new UserService(_db, _userManager.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
        _database.Dispose();
    }

    private static Event SessionEvent(string sessionId, Dictionary<string, string> metadata) => new()
    {
        Data = new EventData
        {
            Object = new Session { Id = sessionId, CustomerId = "cus_1", Metadata = metadata }
        }
    };

    [Fact]
    public async Task UpdateCustomer_PaidPendingSession_StoresCustomerAndClearsPending()
    {
        var user = await _db.Users.FindAsync(UserId);
        user!.PendingCheckoutSessionId = "cs_1";
        await _db.SaveChangesAsync();

        await _service.UpdateCustomerByIdAsync(SessionEvent("cs_1", new() { ["userId"] = UserId }));

        var saved = await _database.GetUserAsync();
        Assert.Equal("cus_1", saved.StripeCustomerId);
        Assert.Null(saved.PendingCheckoutSessionId);
    }

    [Fact]
    public async Task OnUserDelete_KnownCustomer_ClearsCustomerId()
    {
        var user = await _db.Users.FindAsync(UserId);
        user!.StripeCustomerId = "cus_1";
        await _db.SaveChangesAsync();

        await _service.OnUserDelete("cus_1");

        Assert.Null((await _database.GetUserAsync()).StripeCustomerId);
    }

    [Fact]
    public async Task Create_Succeeds_AssignsUserRole()
    {
        var user = new User();
        _userManager.Setup(m => m.CreateAsync(user, "pw")).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(user, "User")).ReturnsAsync(IdentityResult.Success);

        var result = await _service.CreateAsync(user, "pw");

        Assert.True(result.Succeeded);
        _userManager.Verify(m => m.AddToRoleAsync(user, "User"), Times.Once);
    }

    [Fact]
    public async Task Create_Fails_DoesNotAssignRole()
    {
        var user = new User();
        _userManager.Setup(m => m.CreateAsync(user, "pw")).ReturnsAsync(IdentityResult.Failed());

        var result = await _service.CreateAsync(user, "pw");

        Assert.False(result.Succeeded);
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SetPendingCheckoutSession_GoesThroughUserManager()
    {
        var user = new User();
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        await _service.SetPendingCheckoutSessionAsync(user, "cs_1");

        Assert.Equal("cs_1", user.PendingCheckoutSessionId);
        _userManager.Verify(m => m.UpdateAsync(user), Times.Once);
    }
}
