using StreamTier.API.Dtos;
using StreamTier.API.Models;
using StreamTier.API.Services.InvoiceService;
using StreamTier.API.Services.SubscriptionService;
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

    public WebHookService(ISubscriptionService subscriptionService, IInvoiceService invoiceService)
    {
        _subscriptionService = subscriptionService;
        _invoiceService = invoiceService;
    }


    public async Task OnSessionCompleteSubscription(Event stripeEvent)
    {
        var stripeSubscriptionService = new StripeSubscriptionService();

        var session = stripeEvent.Data.Object as Session
                      ?? throw new InvalidOperationException("Expected Session object in Stripe event data.");


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

        await _subscriptionService.Save(subscriptionToSave);
    }


    public async Task OnInvoicePaid(Event stripeEvent)
    {
        var stripeInvoice = stripeEvent.Data.Object as Invoice ??
                            throw new InvalidOperationException("Expected Session object in Stripe event data.");

        if (!stripeInvoice.Parent.SubscriptionDetails.Metadata.TryGetValue("userId", out var userId))
            throw new InvalidOperationException("Stripe session metadata missing 'userId'.");

        var subscriptionId = stripeInvoice.Parent.SubscriptionDetails.SubscriptionId;

        var existing = await _subscriptionService.GetByStripeSubscriptionId(subscriptionId);
        if (existing != null)
            return;
        
        var invoice = new CreateInvoiceDto()
        {
            UserId = userId,
            AmountPaidInCents = stripeInvoice.AmountPaid,
            Currency = stripeInvoice.Currency,
            StripeInvoiceId = stripeInvoice.Id,
            SubscriptionId = subscriptionId
        };

        await _invoiceService.SaveAsync(invoice);
    }

    public async Task OnSubscriptionDelete(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Subscription
                                 ?? throw new InvalidOperationException(
                                     "Expected Session object in Stripe event data.");

        await _subscriptionService.DeleteAsync(stripeSubscription.Id);
    }
}