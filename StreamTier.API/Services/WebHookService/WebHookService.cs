using StreamTier.API.Models;
using StreamTier.API.Dtos;
using StreamTier.API.Services.InvoiceService;
using StreamTier.API.Services.SubscriptionService;
using Stripe;
using Stripe.Checkout;
using Invoice = Stripe.Invoice;
using ModelSubscription = StreamTier.API.Models.Subscription;
using Subscription = Stripe.Subscription;

namespace StreamTier.API.Services;

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
        var session = stripeEvent.Data.Object as Session;

        var stripeSubscriptionService = new Stripe.SubscriptionService();
        
        var stripeSubscription = stripeSubscriptionService.Get($"{session?.SubscriptionId}");
        
        var subscription = new CheckoutSubscriptionDto()
        {
            UserId = session.Metadata?["userId"],
            PlanId = session.Metadata?["planId"],
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


    public async Task OnInvoiceCreate(Event stripeEvent)
    {
        var stripeInvoice = stripeEvent.Data.Object as Invoice;
        
        var invoice = new CheckoutInvoiceDto()
        {
            UserId = stripeInvoice.Parent.SubscriptionDetails.Metadata?["userId"],
            AmountPaidInCents = stripeInvoice.AmountPaid,
            Currency = stripeInvoice.Currency,
            StripeInvoiceId = stripeInvoice.Id,
            SubscriptionId = stripeInvoice.Parent.SubscriptionDetails.SubscriptionId
        };

        await _invoiceService.Save(invoice);
    }

     public async Task OnSubscriptionDelete(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Subscription;
        
        await _subscriptionService.DeleteAsync(stripeSubscription.Id);
    }
}