using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;
using StreamTier.API.Controllers;
using StreamTier.API.Dtos.Requests;
using StreamTier.API.Models;
using StreamTier.API.Services.SubscriptionService;
using StreamTier.API.Services.UserService;
using Stripe;
using Xunit;

namespace StreamTierUnitTests;

public class AuthenticationControllerTest
{
    private const string Email = "user@test.com";
    private const string Password = "Password1!";

    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ISubscriptionService> _subscriptions = new();
    private readonly Mock<CustomerService> _customers = new();
    private readonly AuthenticationController _controller;

    public AuthenticationControllerTest()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "a-test-signing-key-of-at-least-32-characters",
                ["Jwt:Issuer"] = "http://test/",
                ["Jwt:Audience"] = "http://test/",
                ["Jwt:ExpirationInMinutes"] = "30"
            })
            .Build();

        _controller = new AuthenticationController(_users.Object, config, _subscriptions.Object, _customers.Object,
            NullLogger<AuthenticationController>.Instance);

        _users.Setup(u => u.CreateAsync(It.IsAny<User>(), Password))
            .Callback<User, string>((user, _) => user.Id = "user_1")
            .ReturnsAsync(IdentityResult.Success);
    }

    private void StripeCustomerCreationReturns(string customerId) =>
        _customers.Setup(c => c.CreateAsync(It.IsAny<CustomerCreateOptions>(), It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = customerId });

    private Task<IActionResult> Register(string email = Email) =>
        _controller.Register(new RegisterRequestDto { Email = email, Password = Password });

    private Task<IActionResult> Login() =>
        _controller.Login(new LoginRequestDto { Email = Email, Password = Password });

    [Fact]
    public async Task Register_GivesFreePlanAndStripeCustomer()
    {
        StripeCustomerCreationReturns("cus_1");

        Assert.IsType<CreatedResult>(await Register());

        _subscriptions.Verify(s => s.CreateFreeSubscriptionAsync("user_1"), Times.Once);
        _users.Verify(u => u.SetStripeCustomerId(It.Is<User>(x => x.Id == "user_1"), "cus_1"), Times.Once);
    }

    [Fact]
    public async Task Register_StripeUnreachable_StillCreatesAccountWithFreePlan()
    {
        _customers.Setup(c => c.CreateAsync(It.IsAny<CustomerCreateOptions>(), It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StripeException("down"));

        Assert.IsType<CreatedResult>(await Register());

        _subscriptions.Verify(s => s.CreateFreeSubscriptionAsync("user_1"), Times.Once);
        _users.Verify(u => u.SetStripeCustomerId(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Register_IdentityRejectsUser_GivesNoPlan()
    {
        _users.Setup(u => u.CreateAsync(It.IsAny<User>(), Password)).ReturnsAsync(IdentityResult.Failed());

        Assert.IsType<BadRequestObjectResult>(await Register());

        _subscriptions.Verify(s => s.CreateFreeSubscriptionAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("user@localhost")]
    [InlineData("user@test.c")]
    [InlineData("user@test.123")]
    [InlineData("Name <user@test.com>")]
    public async Task Register_MalformedEmail_IsRejected(string email)
    {
        Assert.IsType<BadRequestObjectResult>(await Register(email));

        _users.Verify(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        Assert.IsType<UnauthorizedResult>(await Login());
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var user = new User { Id = "user_1", Email = Email };
        _users.Setup(u => u.FindByEmailAsync(Email)).ReturnsAsync(user);
        _users.Setup(u => u.CheckPasswordAsync(user, Password)).ReturnsAsync(false);

        Assert.IsType<UnauthorizedResult>(await Login());
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenWithIdentityAndRoles()
    {
        var user = new User { Id = "user_1", Email = Email };
        _users.Setup(u => u.FindByEmailAsync(Email)).ReturnsAsync(user);
        _users.Setup(u => u.CheckPasswordAsync(user, Password)).ReturnsAsync(true);
        _users.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(["User"]);

        var ok = Assert.IsType<OkObjectResult>(await Login());

        var accessToken = (string)ok.Value!.GetType().GetProperty("AccessToken")!.GetValue(ok.Value)!;
        var token = new JsonWebToken(accessToken);
        Assert.Equal("user_1", token.Subject);
        Assert.Equal("http://test/", token.Issuer);
        Assert.Contains(token.Claims, c => c.Value == "User");
        Assert.True(token.ValidTo > DateTime.UtcNow);
    }
}
