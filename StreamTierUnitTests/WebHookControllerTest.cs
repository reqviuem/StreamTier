using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StreamTier.API.Controllers;
using StreamTier.API.Exceptions;
using StreamTier.API.Services.UserService;
using StreamTier.API.Services.WebHookService;
using Stripe;
using Xunit;

namespace StreamTierUnitTests;

public class WebHookControllerTest
{
    private const string WebhookSecret = "whsec_test";

    private readonly Mock<IWebHookService> _service = new();
    private readonly Mock<IUserService> _users = new();

    private WebHookController ControllerFor(string payload, string signature)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Stripe:WebhookSecret"] = WebhookSecret })
            .Build();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        httpContext.Request.Headers["Stripe-Signature"] = signature;

        return new WebHookController(config, _service.Object, _users.Object, NullLogger<WebHookController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private WebHookController SignedControllerFor(string eventType)
    {
        var payload = $$"""
                        {
                          "id": "evt_1",
                          "object": "event",
                          "type": "{{eventType}}",
                          "data": { "object": { "id": "obj_1" } }
                        }
                        """;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(WebhookSecret),
            Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));

        return ControllerFor(payload, $"t={timestamp},v1={Convert.ToHexStringLower(hash)}");
    }

    [Fact]
    public async Task InvalidSignature_ReturnsBadRequest()
    {
        var result = await ControllerFor("{}", "t=1,v1=forged").Webhooks();

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task SessionCompleted_RunsBothHandlers()
    {
        var result = await SignedControllerFor("checkout.session.completed").Webhooks();

        Assert.IsType<OkResult>(result);
        _service.Verify(s => s.OnSessionCompleteSubscription(It.IsAny<Event>()), Times.Once);
        _users.Verify(u => u.UpdateCustomerByIdAsync(It.IsAny<Event>()), Times.Once);
    }
    
    [Fact]
    public async Task SessionCompleted_ActivationFailsPermanently_StillClearsPendingSession()
    {
        _service.Setup(s => s.OnSessionCompleteSubscription(It.IsAny<Event>()))
            .ThrowsAsync(new PermanentWebhookException("duplicate"));

        var result = await SignedControllerFor("checkout.session.completed").Webhooks();

        Assert.IsType<OkResult>(result);
        _users.Verify(u => u.UpdateCustomerByIdAsync(It.IsAny<Event>()), Times.Once);
    }
    
    [Fact]
    public async Task SessionCompleted_ActivationFailsTemporarily_KeepsPendingSessionAndRetries()
    {
        _service.Setup(s => s.OnSessionCompleteSubscription(It.IsAny<Event>()))
            .ThrowsAsync(new TimeoutException());

        await Assert.ThrowsAsync<TimeoutException>(() =>
            SignedControllerFor("checkout.session.completed").Webhooks());

        _users.Verify(u => u.UpdateCustomerByIdAsync(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task PermanentFailure_ReturnsOkSoStripeStopsRetrying()
    {
        _service.Setup(s => s.OnInvoicePaid(It.IsAny<Event>()))
            .ThrowsAsync(new PermanentWebhookException("hopeless"));

        var result = await SignedControllerFor("invoice.paid").Webhooks();

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task OtherFailure_PropagatesSoStripeRetries()
    {
        _service.Setup(s => s.OnInvoicePaid(It.IsAny<Event>())).ThrowsAsync(new TimeoutException());

        await Assert.ThrowsAsync<TimeoutException>(() => SignedControllerFor("invoice.paid").Webhooks());
    }

    [Fact]
    public async Task EventTypes_ReachTheirHandlers()
    {
        await SignedControllerFor("invoice.paid").Webhooks();
        await SignedControllerFor("customer.subscription.updated").Webhooks();
        await SignedControllerFor("customer.subscription.deleted").Webhooks();
        await SignedControllerFor("invoice.payment_failed").Webhooks();
        await SignedControllerFor("payment_intent.payment_failed").Webhooks();
        await SignedControllerFor("customer.deleted").Webhooks();

        _service.Verify(s => s.OnInvoicePaid(It.IsAny<Event>()), Times.Once);
        _service.Verify(s => s.OnSubscriptionUpdated(It.IsAny<Event>()), Times.Once);
        _service.Verify(s => s.OnSubscriptionDelete(It.IsAny<Event>()), Times.Once);
        _service.Verify(s => s.OnUpdatePaymentFailed(It.IsAny<Event>()), Times.Once);
        _service.Verify(s => s.OnPaymentFailed(It.IsAny<Event>()), Times.Once);
        _service.Verify(s => s.OnCustomerDelete(It.IsAny<Event>()), Times.Once);
    }
}
