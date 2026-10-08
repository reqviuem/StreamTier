using Hangfire;
using StreamTier.API.Dtos;
using StreamTier.API.Models;
using StreamTier.API.Services.EmailService;
using StreamTier.API.Services.InvoiceService;
using StreamTier.API.Services.SubscriptionService;
using StreamTier.API.Services.UserService;
using Stripe;
using Stripe.Checkout;
using Invoice = Stripe.Invoice;
using ModelSubscription = StreamTier.API.Models.Subscription;
using Subscription = Stripe.Subscription;
using StripeSubscriptionService = Stripe.SubscriptionService;

namespace StreamTier.API.Services.WebHookService;

public class WebHookService : IWebHookService
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IInvoiceService _invoiceService;
    private readonly IUserService _userService;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public WebHookService(ISubscriptionService subscriptionService, IInvoiceService invoiceService,
        IUserService userService, IBackgroundJobClient backgroundJobClient)
    {
        _subscriptionService = subscriptionService;
        _invoiceService = invoiceService;
        _userService = userService;
        _backgroundJobClient = backgroundJobClient;
    }


    public async Task OnSessionCompleteSubscription(Event stripeEvent)
    {
        var stripeSubscriptionService = new StripeSubscriptionService();

        var session = stripeEvent.Data.Object as Session
                      ?? throw new InvalidOperationException("Expected Session object in Stripe event data.");


        // In case of one-time payment
        if (session.SubscriptionId == null)
        {
            return;
        }

        var existing = await _subscriptionService.GetByStripeSubscriptionId(session.SubscriptionId);
        if (existing != null)
            return;

        var stripeSubscription = await stripeSubscriptionService.GetAsync($"{session.SubscriptionId}");

        if (!session.Metadata.TryGetValue("userId", out var userId))
            throw new InvalidOperationException("Stripe session metadata missing 'userId'.");

        if (!session.Metadata.TryGetValue("planId", out var planId))
            throw new InvalidOperationException("Stripe session metadata missing 'planId'.");

        var subscription = new CreateSubscriptionDto()
        {
            UserId = userId,
            PlanId = planId,
            Status = Status.Active,
            StripeCustomerId = session.CustomerId,
            StripeSubscriptionId = session.SubscriptionId,
            CurrentPeriodStart = stripeSubscription.Items.Data[0].CurrentPeriodStart,
            CurrentPeriodEnd = stripeSubscription.Items.Data[0].CurrentPeriodEnd,
            CreatedAt = session.Created
        };

        var subscriptionToSave = ModelSubscription.FromDto(subscription);

        await _subscriptionService.SaveAsync(subscriptionToSave);
    }


    public async Task OnInvoicePaid(Event stripeEvent)
    {
        var stripeInvoice = stripeEvent.Data.Object as Invoice ??
                            throw new InvalidOperationException("Expected Invoice object in Stripe event data.");

        // If invoice generated not by the subscription, pass
        var subscriptionDetails = stripeInvoice.Parent?.SubscriptionDetails;
        if (subscriptionDetails?.SubscriptionId is null)
        {
            return;
        }

        if (subscriptionDetails.Metadata is null || !subscriptionDetails.Metadata.TryGetValue("userId", out var userId))
            throw new InvalidOperationException("Stripe subscription metadata missing 'userId'.");

        var stripeSubscriptionId = subscriptionDetails.SubscriptionId;

        await _subscriptionService.OnPaymentSucceeded(stripeSubscriptionId);

        var stripeInvoiceId = stripeInvoice.Id;

        var existing = await _invoiceService.GetByStripeInvoiceId(stripeInvoiceId);
        if (existing != null)
            return;

        var invoice = new CreateInvoiceDto()
        {
            UserId = userId,
            AmountPaidInCents = stripeInvoice.AmountPaid,
            Currency = stripeInvoice.Currency,
            StripeInvoiceId = stripeInvoiceId,
            SubscriptionId = stripeSubscriptionId
        };

        await _invoiceService.SaveAsync(invoice);

        var user = await _userService.GetUserByIdAsync(userId);

        _backgroundJobClient.Enqueue<IEmailService>(x =>
            x.SendAsync(user.Email, "Payment succeeded!", "Thank you for choosing StreamTier :)"));
    }

    public async Task OnSubscriptionDelete(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Subscription
                                 ?? throw new InvalidOperationException(
                                     "Expected Subscription object in Stripe event data.");


        await _subscriptionService.DowngradeSubscriptionAsync(stripeSubscription.Id);
    }

    public async Task OnUpdatePaymentFailed(Event stripeEvent)
    {
        var stripeInvoice = stripeEvent.Data.Object as Invoice ??
                            throw new InvalidOperationException("Expected Invoice object in Stripe event data.");

        var subscriptionDetails = stripeInvoice.Parent?.SubscriptionDetails;

        if (subscriptionDetails?.SubscriptionId is null)
        {
            return;
        }
        
        
        if (subscriptionDetails.Metadata is null || !subscriptionDetails.Metadata.TryGetValue("userId", out var userId))
            throw new InvalidOperationException("Stripe subscription metadata missing 'userId'.");
        
        
        var user = await _userService.GetUserByIdAsync(userId);
        
        if (user?.Email is not null)
        {
            _backgroundJobClient.Enqueue<IEmailService>(x => x.SendAsync(user.Email, "Payment failed - action needed",
                $"Hi,\n\nWe couldn't process your most recent payment for StreamTier. " +
                $"Please update your payment method within 7 days to avoid losing access.\n\n" +
                $"— StreamTier"));
        }


        var subscriptionId = subscriptionDetails.SubscriptionId;

        await _subscriptionService.OnPaymentFailed(subscriptionId);
    }

    public async Task OnPaymentFailed(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent
                            ?? throw new InvalidOperationException("Expected PaymentIntent.");

        if (string.IsNullOrEmpty(paymentIntent.CustomerId))
            return;

        var customer = await new CustomerService().GetAsync(paymentIntent.CustomerId);

        if (string.IsNullOrEmpty(customer.Email))
            return;


        _backgroundJobClient.Enqueue<IEmailService>(x => x.SendAsync(customer.Email, "Checkout payment failed",
            "We could not charge your card. Please try another payment method."));
    }

    public async Task OnCustomerDelete(Event stripeEvent)
    {
        var customer = stripeEvent.Data.Object as Customer
                       ?? throw new InvalidOperationException("Expected Customer object in Stripe event data.");

        await _userService.OnUserDelete(customer.Id);
    }
}