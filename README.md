# StreamTier

A subscription billing API for a streaming-style service, built with ASP.NET Core and Stripe. Users register, pick a tier, and pay through Stripe Checkout; the API keeps its own subscription and invoice records in sync with Stripe by consuming webhook events.

> **Status: in active development** against Stripe test mode. Registration, authentication, plan listing, Checkout session creation, and signature-verified webhook intake are in place

## Stack

.NET 10 · ASP.NET Core Web API · ASP.NET Core Identity · JWT bearer auth · PostgreSQL · Entity Framework Core · Stripe.net · OpenAPI/Swagger

## How it works

The subscription lifecycle spans two systems, and Stripe is the source of truth for anything involving money:

1. A new user registers and is immediately given the free tier, so every account has exactly one active subscription from the start.
2. To upgrade, the client calls `POST /checkout/session`. The API looks up the requested plan, confirms it's active, and creates a Stripe Checkout session in `subscription` mode. The caller's `userId` and the `planId` are attached as session metadata, and `userId` is also attached to the subscription metadata so later invoice events can be traced back to an account.
3. The client is redirected to Stripe's hosted checkout page. No card data ever reaches this API.
4. Stripe reports the outcome asynchronously to `POST /webhooks/stripe`. Every request is verified against the endpoint's signing secret before being parsed, so unsigned or tampered payloads are rejected.
5. Handled events update local state: `checkout.session.completed` records the new paid subscription and stores the Stripe customer ID on the user, `invoice.paid` records a renewal invoice, and `customer.subscription.deleted` removes the cancelled subscription.

### Design notes

**Webhook handlers are idempotent.** Stripe does not guarantee exactly-once delivery — it retries with backoff until it gets a `2xx`, which means a handler can legitimately see the same event twice. Before inserting, each handler checks whether a record for that Stripe object already exists and returns early if so, so redelivery is a no-op rather than a duplicate subscription or a double-counted invoice.

**Users are correlated through Stripe metadata, not email.** Emails change and aren't reliably unique across Stripe objects, so the internal user ID is stamped onto the Checkout session and the subscription at creation time. Handlers treat missing metadata as a hard error rather than guessing.

**Plans are seeded, and pricing lives in Stripe.** The three tiers are seeded through EF Core so a fresh database is immediately usable, and each row stores its Stripe price ID. Amounts are held as integer minor units (cents) to avoid floating-point rounding on money.

| Plan | Price | Max screens | Max resolution |
| --- | --- | --- | --- |
| Basic | Free | 1 | QD |
| Standard | €9.99 / month | 2 | HD |
| Premium | €19.99 / month | 4 | 4K |

## Endpoints

| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| `POST` | `/auth/register` | — | Creates an account and provisions the free tier. |
| `POST` | `/auth/login` | — | Returns a JWT access token. |
| `GET` | `/plans` | JWT | Returns the available subscription tiers. |
| `POST` | `/checkout/session` | JWT | Creates a Stripe Checkout session and returns its URL. |
| `POST` | `/webhooks/stripe` | Stripe signature | Consumes subscription and invoice events. |

Swagger UI is served at `/swagger` in the Development environment.

## Running locally

You'll need the [.NET 10 SDK](https://dotnet.microsoft.com/download), a PostgreSQL instance, and a [Stripe](https://stripe.com) account in test mode.

Configuration is read from `appsettings.Development.json` for non-sensitive values; secrets are expected in [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) and are intentionally left blank in the committed files.

```bash
cd StreamTier.API

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=streamtier;Username=postgres;Password=postgres"
dotnet user-secrets set "Jwt:SecretKey" "<at least 32 characters>"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."

dotnet run
```

Migrations are applied automatically at startup, so the schema and the seeded plans are created on first run.

The seeded plans reference Stripe price IDs from the author's test account. To run checkout against your own account, create three recurring prices in the Stripe dashboard and update the `StripePriceId` values in `AppDbContext.OnModelCreating`.

### Receiving webhooks

Use the [Stripe CLI](https://stripe.com/docs/stripe-cli) to forward events to your local instance, and pass the signing secret it prints to `Stripe:WebhookSecret`:

```bash
stripe listen --forward-to http://localhost:5148/webhooks/stripe
```

The default `http` launch profile listens on port 5148. If you run the `https` profile instead, forward to `https://localhost:7123/webhooks/stripe` and add `--skip-verify` so the CLI accepts the ASP.NET development certificate.

## Project layout

```
StreamTier.API/
├── Controllers/     Authentication, tiers, Stripe checkout, webhook intake
├── Services/        Business logic behind interfaces, one folder per concern
├── Models/          EF Core entities (User, Subscription, SubscriptionPlan, Invoice)
├── Dtos/            Request, response, and internal transfer objects
├── Data/            AppDbContext, entity configuration, plan seed data
└── Migrations/      EF Core migrations
```

Controllers stay thin and handle HTTP concerns only; each service is registered against an interface so the payment and subscription logic can be tested without a live Stripe connection.
