using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using StreamTier.API.Dtos;
using StreamTier.API.Exceptions;
using StreamTier.API.Models;
using StreamTier.API.Services.InvoiceService;
using StreamTier.API.Services.SubscriptionService;
using StreamTier.API.Services.UserService;
using StreamTier.API.Services.WebHookService;
using Stripe;
using Stripe.Checkout;
using Xunit;
using Invoice = Stripe.Invoice;
using ModelSubscription = StreamTier.API.Models.Subscription;
using StripeSubscription = Stripe.Subscription;
using StripeSubscriptionService = Stripe.SubscriptionService;

namespace StreamTierUnitTests;

public class WebHookServiceTest
{
    private const string UserId = "user_1";

    private readonly Mock<ISubscriptionService> _subscriptions = new();
    private readonly Mock<IInvoiceService> _invoices = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<IBackgroundJobClient> _jobs = new();
    private readonly Mock<StripeSubscriptionService> _stripeSubscriptions = new();
    private readonly Mock<CustomerService> _stripeCustomers = new();
    private readonly WebHookService _service;

    public WebHookServiceTest()
    {
        _service = new WebHookService(_subscriptions.Object, _invoices.Object, _users.Object, _jobs.Object,
            _stripeSubscriptions.Object, _stripeCustomers.Object);

        _users.Setup(u => u.GetUserByIdAsync(UserId)).ReturnsAsync(new User { Id = UserId, Email = "user@test.com" });
    }

    private static Event EventOf(IHasObject stripeObject) => new() { Data = new EventData { Object = stripeObject } };

    private static Session PaidSession() => new()
    {
        Id = "cs_1",
        CustomerId = "cus_1",
        SubscriptionId = "sub_1",
        Created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Metadata = new() { ["userId"] = UserId, ["planId"] = "PremiumPlan" }
    };

    private static Invoice SubscriptionInvoice() => new()
    {
        Id = "in_1",
        AmountPaid = 1999,
        Currency = "eur",
        Parent = new InvoiceParent
        {
            SubscriptionDetails = new InvoiceParentSubscriptionDetails
            {
                SubscriptionId = "sub_1",
                Metadata = new() { ["userId"] = UserId }
            }
        }
    };

    private void StripeSubscriptionIs(string status)
    {
        _stripeSubscriptions
            .Setup(s => s.GetAsync("sub_1", It.IsAny<SubscriptionGetOptions>(), It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeSubscription
            {
                Status = status,
                Items = new StripeList<SubscriptionItem>
                {
                    Data =
                    [
                        new SubscriptionItem
                        {
                            CurrentPeriodStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            CurrentPeriodEnd = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
                        }
                    ]
                }
            });
    }

    private void VerifyStripeSubscriptionNotFetched() =>
        _stripeSubscriptions.Verify(s => s.GetAsync(It.IsAny<string>(), It.IsAny<SubscriptionGetOptions>(),
            It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);

    private void VerifyNotActivated() =>
        _subscriptions.Verify(s => s.ActivatePaidSubscriptionAsync(It.IsAny<ModelSubscription>()), Times.Never);

    private void VerifyStripeCancellation(Times times) =>
        _stripeSubscriptions.Verify(s => s.CancelAsync("sub_1", It.IsAny<SubscriptionCancelOptions>(),
            It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()), times);

    private void VerifyEmailSentTo(string email) =>
        _jobs.Verify(j => j.Create(
            It.Is<Job>(job => job.Type == typeof(StreamTier.API.Services.EmailService.IEmailService)
                              && (string)job.Args[0] == email),
            It.IsAny<IState>()), Times.Once);

    private void VerifyNoEmail() =>
        _jobs.Verify(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);

    [Fact]
    public async Task SessionComplete_ActivatesWithStripeData()
    {
        StripeSubscriptionIs("active");
        ModelSubscription? activated = null;
        _subscriptions.Setup(s => s.ActivatePaidSubscriptionAsync(It.IsAny<ModelSubscription>()))
            .Callback<ModelSubscription>(s => activated = s);

        await _service.OnSessionCompleteSubscription(EventOf(PaidSession()));

        Assert.NotNull(activated);
        Assert.Equal(UserId, activated.UserId);
        Assert.Equal("PremiumPlan", activated.PlanId);
        Assert.Equal("sub_1", activated.StripeSubscriptionId);
        Assert.Equal("cus_1", activated.StripeCustomerId);
        Assert.Equal(Status.Active, activated.Status);
        Assert.Equal(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), activated.CurrentPeriodEnd);
        VerifyStripeCancellation(Times.Never());
    }

    [Fact]
    public async Task SessionComplete_SubscriptionAlreadyCanceledInStripe_IsNotActivated()
    {
        StripeSubscriptionIs("canceled");

        await Assert.ThrowsAsync<PermanentWebhookException>(() =>
            _service.OnSessionCompleteSubscription(EventOf(PaidSession())));

        VerifyNotActivated();
        VerifyStripeCancellation(Times.Never());
    }

    [Fact]
    public async Task SessionComplete_ActivationRefused_CancelsInStripeAndReportsIt()
    {
        StripeSubscriptionIs("active");
        _subscriptions.Setup(s => s.ActivatePaidSubscriptionAsync(It.IsAny<ModelSubscription>()))
            .ThrowsAsync(new PermanentWebhookException("User already has an active paid subscription."));

        var ex = await Assert.ThrowsAsync<PermanentWebhookException>(() =>
            _service.OnSessionCompleteSubscription(EventOf(PaidSession())));

        VerifyStripeCancellation(Times.Once());
        Assert.Contains("already has an active paid subscription", ex.Message);
        Assert.Contains("sub_1", ex.Message);
        Assert.Contains("cs_1", ex.Message);
    }

    [Fact]
    public async Task SessionComplete_CancelFails_PropagatesSoStripeRetries()
    {
        StripeSubscriptionIs("active");
        _subscriptions.Setup(s => s.ActivatePaidSubscriptionAsync(It.IsAny<ModelSubscription>()))
            .ThrowsAsync(new PermanentWebhookException("refused"));
        _stripeSubscriptions.Setup(s => s.CancelAsync("sub_1", It.IsAny<SubscriptionCancelOptions>(),
                It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StripeException("down"));

        await Assert.ThrowsAsync<StripeException>(() =>
            _service.OnSessionCompleteSubscription(EventOf(PaidSession())));
    }
    
    [Fact]
    public async Task SessionComplete_TemporaryActivationFailure_DoesNotCancel()
    {
        StripeSubscriptionIs("active");
        _subscriptions.Setup(s => s.ActivatePaidSubscriptionAsync(It.IsAny<ModelSubscription>()))
            .ThrowsAsync(new TimeoutException());

        await Assert.ThrowsAsync<TimeoutException>(() =>
            _service.OnSessionCompleteSubscription(EventOf(PaidSession())));

        VerifyStripeCancellation(Times.Never());
    }

    [Fact]
    public async Task SessionComplete_DeliveredTwice_IsNoOp()
    {
        _subscriptions.Setup(s => s.GetByStripeSubscriptionId("sub_1"))
            .ReturnsAsync(new SubscriptionExistsDto { Id = Guid.NewGuid() });

        await _service.OnSessionCompleteSubscription(EventOf(PaidSession()));

        VerifyStripeSubscriptionNotFetched();
        VerifyNotActivated();
    }

    [Fact]
    public async Task InvoicePaid_NewInvoice_RestoresSavesAndEmails()
    {
        await _service.OnInvoicePaid(EventOf(SubscriptionInvoice()));

        _subscriptions.Verify(s => s.OnPaymentSucceeded("sub_1"), Times.Once);
        _invoices.Verify(i => i.SaveAsync(It.Is<CreateInvoiceDto>(d =>
            d.UserId == UserId && d.AmountPaidInCents == 1999 && d.Currency == "eur"
            && d.StripeInvoiceId == "in_1" && d.StripeSubscriptionId == "sub_1")), Times.Once);
        VerifyEmailSentTo("user@test.com");
    }

    [Fact]
    public async Task InvoicePaid_DeliveredTwice_DoesNotSaveOrEmailAgain()
    {
        _invoices.Setup(i => i.GetByStripeInvoiceId("in_1")).ReturnsAsync(new InvoiceExistsDto { Id = Guid.NewGuid() });

        await _service.OnInvoicePaid(EventOf(SubscriptionInvoice()));

        _invoices.Verify(i => i.SaveAsync(It.IsAny<CreateInvoiceDto>()), Times.Never);
        VerifyNoEmail();
    }

    [Fact]
    public async Task SubscriptionDelete_DowngradesThatSubscription()
    {
        await _service.OnSubscriptionDelete(EventOf(new StripeSubscription { Id = "sub_1" }));

        _subscriptions.Verify(s => s.DowngradeSubscriptionAsync("sub_1"), Times.Once);
    }

    [Fact]
    public async Task SubscriptionUpdated_PassesStatusPriceAndPeriod()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        await _service.OnSubscriptionUpdated(EventOf(new StripeSubscription
        {
            Id = "sub_1",
            Status = "past_due",
            Items = new StripeList<SubscriptionItem>
            {
                Data =
                [
                    new SubscriptionItem
                    {
                        Price = new Price { Id = "price_1" },
                        CurrentPeriodStart = start,
                        CurrentPeriodEnd = end
                    }
                ]
            }
        }));

        _subscriptions.Verify(s => s.SyncFromStripeAsync("sub_1", "past_due", "price_1", start, end), Times.Once);
    }

    [Fact]
    public async Task RenewalFailed_EmailsUser()
    {
        await _service.OnUpdatePaymentFailed(EventOf(SubscriptionInvoice()));

        VerifyEmailSentTo("user@test.com");
    }

    [Fact]
    public async Task PaymentFailed_CustomerWithPaidSubscription_IsLeftToRenewalEmail()
    {
        _users.Setup(u => u.FindByStripeCustomerIdAsync("cus_1")).ReturnsAsync(new User { Id = UserId });
        _subscriptions.Setup(s => s.GetStripeSubscriptionId(UserId)).ReturnsAsync("sub_1");

        await _service.OnPaymentFailed(EventOf(new PaymentIntent { CustomerId = "cus_1" }));

        VerifyNoEmail();
    }

    [Fact]
    public async Task PaymentFailed_DuringCheckout_EmailsStripeCustomer()
    {
        _stripeCustomers.Setup(c => c.GetAsync("cus_1", It.IsAny<CustomerGetOptions>(), It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = "cus_1", Email = "customer@test.com" });

        await _service.OnPaymentFailed(EventOf(new PaymentIntent { CustomerId = "cus_1" }));

        VerifyEmailSentTo("customer@test.com");
    }

    [Fact]
    public async Task CustomerDelete_ClearsThatCustomer()
    {
        await _service.OnCustomerDelete(EventOf(new Customer { Id = "cus_1" }));

        _users.Verify(u => u.OnUserDelete("cus_1"), Times.Once);
    }
}
