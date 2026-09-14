using StreamTier.API.Data;
using StreamTier.API.Dtos;
using StreamTier.API.Models;
using StreamTier.API.Services.InvoiceService;
using StreamTier.API.Services.SubscriptionService;
using Stripe;
using Stripe.Checkout;
using Invoice = Stripe.Invoice;
using Subscription = Stripe.Subscription;

namespace StreamTier.API.Services;

public class WebHookService : IWebHookService
{
    private readonly AppDbContext _appDbContext;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IInvoiceService _invoiceService;
    
    public WebHookService(AppDbContext appDbContext, ISubscriptionService subscriptionService, IInvoiceService invoiceService)
    {
        _appDbContext = appDbContext;
        _subscriptionService = subscriptionService;
        _invoiceService = invoiceService;
    }

    
    public void SaveSubscription(Event stripeEvent)
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

        _subscriptionService.Save(subscription);
    }


    public void SaveInvoice(Event stripeEvent)
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

        _invoiceService.Save(invoice);
    }

     public void OnSubscriptionDelete(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Subscription;
        
        _subscriptionService.DeleteAsync(stripeSubscription.Id);
    }
}