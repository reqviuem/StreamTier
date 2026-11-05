using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using StreamTier.API.Controllers;
using StreamTier.API.Dtos.Requests;
using StreamTier.API.Models;
using StreamTier.API.Services.SubscriptionPlanService;
using StreamTier.API.Services.SubscriptionService;
using StreamTier.API.Services.UserService;
using Stripe;
using Stripe.Checkout;
using Xunit;

namespace StreamTierUnitTests;

public class StripeControllerTest
{
    private const string UserId = "user_1";

    private readonly Mock<ISubscriptionPlanService> _plans = new();
    private readonly Mock<ISubscriptionService> _subscriptions = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<SessionService> _sessions = new();
    private readonly User _user = new() { Id = UserId, Email = "user@test.com", StripeCustomerId = "cus_1" };
    private readonly StripeController _controller;

    private readonly StripeCheckoutRequestDto _request = new() { PlanId = "PremiumPlan" };

    public StripeControllerTest()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:SuccessUrl"] = "http://test/success",
                ["Stripe:CancelUrl"] = "http://test/cancel"
            })
            .Build();

        _controller = new StripeController(_plans.Object, config, _subscriptions.Object, _users.Object,
            _sessions.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, UserId),
                        new Claim(ClaimTypes.Email, "user@test.com")
                    ]))
                }
            }
        };

        _users.Setup(u => u.GetUserByIdAsync(UserId)).ReturnsAsync(_user);
        _users.Setup(u => u.SetPendingCheckoutSessionAsync(_user, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        PlanIs(NewPlan("PremiumPlan", "price_premium"));

        _sessions.Setup(s => s.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session { Id = "cs_new", Url = "http://stripe/pay" });
    }

    private static SubscriptionPlan NewPlan(string id, string? priceId) => new()
    {
        Id = id,
        Name = id,
        PriceInCents = 1999,
        Currency = "EUR",
        BillingInterval = "Monthly",
        MaxScreens = 4,
        MaxResolution = "4K",
        StripePriceId = priceId,
        IsActive = true
    };

    private void PlanIs(SubscriptionPlan? plan) =>
        _plans.Setup(p => p.GetActivePlanByIdAsync(It.IsAny<string>())).ReturnsAsync(plan);

    private void PendingSessionIs(string status)
    {
        _user.PendingCheckoutSessionId = "cs_old";
        _sessions.Setup(s => s.GetAsync("cs_old", It.IsAny<SessionGetOptions>(), It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session { Id = "cs_old", Status = status });
    }

    private void VerifyExpired(string sessionId, Times times) =>
        _sessions.Verify(s => s.ExpireAsync(sessionId, It.IsAny<SessionExpireOptions>(), It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()), times);

    private void VerifyNoSessionCreated() =>
        _sessions.Verify(s => s.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public async Task Checkout_CreatesSessionTiedToUserAndPlan()
    {
        SessionCreateOptions? options = null;
        _sessions.Setup(s => s.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<SessionCreateOptions, RequestOptions, CancellationToken>((o, _, _) => options = o)
            .ReturnsAsync(new Session { Id = "cs_new", Url = "http://stripe/pay" });

        var result = await _controller.CheckoutSession(_request);

        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(options);
        Assert.Equal("subscription", options.Mode);
        Assert.Equal(UserId, options.Metadata["userId"]);
        Assert.Equal("PremiumPlan", options.Metadata["planId"]);
        Assert.Equal(UserId, options.SubscriptionData.Metadata["userId"]);
        Assert.Equal("cus_1", options.Customer);
        Assert.Null(options.CustomerEmail);
        Assert.Equal("price_premium", Assert.Single(options.LineItems).Price);
        _users.Verify(u => u.SetPendingCheckoutSessionAsync(_user, "cs_new"), Times.Once);
    }

    [Fact]
    public async Task Checkout_NoStripeCustomerYet_PassesEmailInstead()
    {
        _user.StripeCustomerId = null;
        SessionCreateOptions? options = null;
        _sessions.Setup(s => s.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<SessionCreateOptions, RequestOptions, CancellationToken>((o, _, _) => options = o)
            .ReturnsAsync(new Session { Id = "cs_new" });

        await _controller.CheckoutSession(_request);

        Assert.Null(options!.Customer);
        Assert.Equal("user@test.com", options.CustomerEmail);
    }

    [Fact]
    public async Task Checkout_UserAlreadyPaying_ReturnsBadRequest()
    {
        _subscriptions.Setup(s => s.GetStripeSubscriptionId(UserId)).ReturnsAsync("sub_1");

        Assert.IsType<BadRequestObjectResult>(await _controller.CheckoutSession(_request));
        VerifyNoSessionCreated();
    }

    [Fact]
    public async Task Checkout_PlanNotFound_ReturnsBadRequest()
    {
        PlanIs(null);

        Assert.IsType<BadRequestObjectResult>(await _controller.CheckoutSession(_request));
        VerifyNoSessionCreated();
    }

    [Fact]
    public async Task Checkout_FreePlan_ReturnsBadRequest()
    {
        PlanIs(NewPlan("FreePlan", null));

        Assert.IsType<BadRequestObjectResult>(await _controller.CheckoutSession(_request));
        VerifyNoSessionCreated();
    }

    [Fact]
    public async Task Checkout_PlanNotBootstrappedYet_Returns503()
    {
        PlanIs(NewPlan("PremiumPlan", null));

        var result = Assert.IsType<ObjectResult>(await _controller.CheckoutSession(_request));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        VerifyNoSessionCreated();
    }

    [Fact]
    public async Task Checkout_PreviousSessionOpen_ExpiresItFirst()
    {
        PendingSessionIs("open");

        Assert.IsType<OkObjectResult>(await _controller.CheckoutSession(_request));
        VerifyExpired("cs_old", Times.Once());
    }

    [Fact]
    public async Task Checkout_PreviousSessionExpired_CreatesNewOne()
    {
        PendingSessionIs("expired");

        Assert.IsType<OkObjectResult>(await _controller.CheckoutSession(_request));
        VerifyExpired("cs_old", Times.Never());
    }

    [Fact]
    public async Task Checkout_PreviousSessionAlreadyPaid_ReturnsConflict()
    {
        PendingSessionIs("complete");

        Assert.IsType<ConflictObjectResult>(await _controller.CheckoutSession(_request));
        VerifyNoSessionCreated();
    }

    [Fact]
    public async Task Checkout_LosesConcurrencyRace_ExpiresOwnSessionAndReturnsConflict()
    {
        _users.Setup(u => u.SetPendingCheckoutSessionAsync(_user, "cs_new")).ReturnsAsync(IdentityResult.Failed());

        Assert.IsType<ConflictObjectResult>(await _controller.CheckoutSession(_request));
        VerifyExpired("cs_new", Times.Once());
    }
}
