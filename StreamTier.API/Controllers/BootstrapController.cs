using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamTier.API.Services.SubscriptionPlanService;
using Stripe;

namespace StreamTier.API.Controllers;

[ApiController]
[Route("/admin/bootstrap")]
[Authorize(Roles = "Admin")]
public class BootstrapController : ControllerBase
{
    private readonly ISubscriptionPlanService _planService;

    private record PlanSpec(string Id, string Name, long AmountCents, string Currency, string LookupKey);

    public BootstrapController(ISubscriptionPlanService planService)
    {
        _planService = planService;
    }

    private static readonly PlanSpec[] Plans =
    [
        new("StandardPlan", "Standard", 999, "eur", "standard_monthly"),
        new("PremiumPlan", "Premium", 1999, "eur", "premium_monthly"),
    ];

    [HttpPost]
    public async Task<IActionResult> Bootstrap()
    {
        var productService = new ProductService();
        var priceService = new PriceService();

        var results = new List<object>();
        foreach (var plan in Plans)
        {
            var product = await FindOrCreateProduct(productService, plan);
            var price = await FindOrCreatePrice(priceService, product, plan);

            await _planService.SetStripePriceId(plan.Id, price.Id);

            results.Add(new { plan = plan.Id, product = product.Id, price = price.Id });
        }

        return Ok(results);
    }

    static async Task<Product> FindOrCreateProduct(ProductService svc, PlanSpec plan)
    {
        var all = await svc.ListAsync(new ProductListOptions { Limit = 100 });
        var found = all.Data.FirstOrDefault(p =>
            p.Metadata.TryGetValue("Id", out var key) && key == plan.Id);

        if (found is not null) return found;

        return await svc.CreateAsync(new ProductCreateOptions
        {
            Name = plan.Name,
            Metadata = new Dictionary<string, string> { ["Id"] = plan.Id }
        });
    }

    static async Task<Price> FindOrCreatePrice(PriceService svc, Product product, PlanSpec plan)
    {
        var existing = await svc.ListAsync(new PriceListOptions
        {
            LookupKeys = new List<string> { plan.LookupKey },
            Active = true
        });

        var found = existing.Data.FirstOrDefault();
        if (found is not null) return found;

        return await svc.CreateAsync(new PriceCreateOptions
        {
            Product = product.Id,
            UnitAmount = plan.AmountCents,
            Currency = plan.Currency,
            Recurring = new PriceRecurringOptions { Interval = "month" },
            LookupKey = plan.LookupKey
        });
    }
}