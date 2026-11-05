# StreamTier

A subscription billing API for a streaming-style service, built with ASP.NET Core and Stripe. Users register, pick a tier, and pay through Stripe Checkout; the API keeps its own subscription and invoice records in sync with Stripe by consuming webhook events.

## Stack

.NET 10 · ASP.NET Core Web API · ASP.NET Core Identity · JWT bearer auth · PostgreSQL · Entity Framework Core · Stripe.net · Hangfire · MailKit · OpenAPI/Swagger

## How it works

The subscription lifecycle spans two systems, and **Stripe is the source of truth** for anything involving money. The API doesn't change a paid subscription on its own; it reacts to what Stripe reports, so cancellations, refunds and plan changes are made in Stripe and flow into the local database through webhooks. The one exception is a subscription the API has refused to activate, which it cancels in Stripe so the customer isn't charged for a plan they don't have.

1. A new user registers, receives the `User` role, and is immediately given the free plan, so every account has exactly one current subscription from the start. A Stripe customer is created for the user at the same time, so all of their checkouts belong to one customer. If Stripe is unreachable during registration, the account is still created and the customer is created at the user's first checkout instead.
2. To upgrade, the client calls `POST /checkout/session`. The API rejects the free plan and users who already have a paid subscription, then creates a Stripe Checkout session in `subscription` mode. The caller's `userId` and the `planId` are attached as session metadata, and `userId` is also attached to the subscription metadata so later invoice events can be traced back to an account.
3. The client sends the user to the returned URL, Stripe's hosted checkout page. No card data ever reaches this API. Afterwards Stripe redirects the browser to a small static page served by the API: `/checkout/success.html` or `/checkout/cancel.html`.
4. Stripe reports the outcome asynchronously to `POST /webhooks/stripe`. Every request is verified against the endpoint's signing secret before being parsed, so unsigned or tampered payloads are rejected.
5. Handled events keep local state in step with Stripe:

| Stripe event | What the API does |
| --- | --- |
| `checkout.session.completed` | Replaces the free plan with the new paid subscription, stores the Stripe customer ID on the user, and clears the user's pending checkout session. |
| `invoice.paid` | Records the invoice, restores a past-due subscription to active, and emails a receipt. |
| `customer.subscription.updated` | Mirrors Stripe's status, the plan (matched by Stripe price) and the current billing period into the local subscription. |
| `invoice.payment_failed` | Emails the user that a renewal payment failed. The status change itself arrives through `customer.subscription.updated`. |
| `payment_intent.payment_failed` | Emails customers whose payment failed during checkout (renewal failures are covered by `invoice.payment_failed`). |
| `customer.subscription.deleted` | Cancels the paid subscription and downgrades the user to the free plan. |
| `customer.deleted` | Clears the stored Stripe customer ID on the user. |

### Failed payments

When a renewal fails, Stripe retries it on the schedule configured in the Stripe Dashboard (Billing → Subscriptions and emails). The first failure moves the subscription to `past_due`, which the API mirrors locally while the user keeps access. If a retry succeeds, the subscription returns to `active`. If all retries fail and the Dashboard is set to cancel the subscription, Stripe sends `customer.subscription.deleted` and the user falls back to the free plan. The API enforces no deadline of its own; the retry window lives entirely in Stripe.

### Design notes

**Only one payable checkout link per user.** Stripe Checkout runs outside the API, so a user who opened two checkout sessions before paying could otherwise pay for both. The API remembers each user's open session; when a new one is requested, the previous one is expired in Stripe first, so its link can no longer be paid. If the previous session turns out to be already paid (its webhook just hasn't arrived yet), the request is answered with `409 Conflict` instead of creating a new session. Sessions also expire on their own after 30 minutes, and two checkout requests racing for the same user are caught by ASP.NET Core Identity's concurrency stamp: the loser expires its own session and returns `409`.

**One current subscription per user, enforced by the database.** A partial unique index allows each user at most one subscription in the `Active` or `PastDue` state, while any number of canceled ones can sit beside it. Swapping plans therefore cancels the old row before inserting the new one, inside a single transaction. Subscription statuses are stored as text, so the data stays readable and reordering the enum can't silently change their meaning.

**Webhook handlers are idempotent.** Stripe does not guarantee exactly-once delivery, so a handler can legitimately see the same event twice. Before inserting, each handler checks whether a record for that Stripe object already exists and returns early if so, so redelivery is a no-op rather than a duplicate subscription or a double-counted invoice. Unique indexes on the Stripe subscription and invoice IDs back this up at the database level.

**Permanent versus retryable failures.** Handlers throw `PermanentWebhookException` for events that can never succeed, such as missing metadata or an unknown subscription. The controller logs these and still returns `200`, so Stripe does not retry a hopeless event for days. Any other exception, such as a database outage, becomes a `500` and Stripe retries with backoff. A completed Checkout session clears the user's pending session even if activation failed permanently, so one bad event can't block that user's future checkouts.

**Refused subscriptions are canceled, not refunded.** If a paid session can't be activated because the user already has a paid subscription, the new subscription is canceled in Stripe so it stops charging. The first payment is not refunded automatically: the log entry names the subscription and the session so a refund can be decided in Stripe. Sessions with missing metadata are only logged, because this API always sets it, so they didn't come from here.

**Events can arrive out of order.** A subscription update for a row that doesn't exist yet, one that is already canceled, or one describing an earlier billing period is logged and ignored rather than failing or overwriting newer state. Activation uses the subscription's current status from Stripe, so a subscription that ended before its `checkout.session.completed` arrived is not activated.

**Users are correlated through Stripe metadata, not email.** Emails change and aren't reliably unique across Stripe objects, so the internal user ID is stamped onto the Checkout session and the subscription at creation time. Handlers treat missing metadata as a hard error rather than guessing.

**Roles keep admin and customer access separate.** Registration assigns the `User` role, and customer endpoints require it. The seeded admin account has only the `Admin` role, so it can manage the system but cannot act as a customer. Roles are written into the JWT at login, so a role change takes effect after the user's next login.

**Plans are seeded, and pricing lives in Stripe.** The three tiers are seeded through EF Core so a fresh database is immediately usable. Amounts are held as integer minor units (cents) to avoid floating-point rounding on money. An admin-only bootstrap endpoint creates the matching Stripe products and prices and stores their IDs on the plan rows.

| Plan | Price | Max screens | Max resolution |
| --- | --- | --- | --- |
| Free | Free | 1 | SD |
| Standard | €9.99 / month | 2 | HD |
| Premium | €19.99 / month | 4 | 4K |

**Emails are sent in the background.** Payment receipts and failure notices are queued with Hangfire (stored in PostgreSQL) and delivered over SMTP with MailKit, so a slow mail server never delays a webhook response.

## Endpoints

| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| `POST` | `/auth/register` | — | Creates an account with the `User` role, provisions the free plan, and creates the Stripe customer. |
| `POST` | `/auth/login` | — | Returns a JWT access token carrying the user's roles. |
| `GET` | `/plans` | JWT, `User` role | Returns the purchasable subscription tiers (active, excluding the free plan). |
| `POST` | `/checkout/session` | JWT, `User` role | Creates a Stripe Checkout session and returns its URL. Expires the user's previous unpaid session. |
| `POST` | `/admin/bootstrap` | JWT, `Admin` role | Creates the Stripe products and prices and stores their IDs on the plans. |
| `POST` | `/webhooks/stripe` | Stripe signature | Consumes subscription, invoice and payment events. |

Also served:

- **Swagger UI** at `/swagger` in the Development environment. Use the **Authorize** button to paste the token from `/auth/login` once; it is then sent with every request and kept across page reloads.
- **Checkout result pages** at `/checkout/success.html` and `/checkout/cancel.html`, where Stripe sends the browser after checkout.
- **Hangfire dashboard** at `/hangfire`, for the background email jobs. It uses Hangfire's default access rule, so it only opens from the machine the API runs on.

## Running locally

You'll need the [.NET 10 SDK](https://dotnet.microsoft.com/download), a PostgreSQL instance, a [Stripe](https://stripe.com) account in test mode, and an SMTP inbox for testing emails (for example [Mailtrap](https://mailtrap.io)).

Configuration is read from `appsettings.Development.json` for non-sensitive values (JWT issuer, Checkout redirect URLs, SMTP host, admin email); secrets are expected in [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) and are intentionally left blank in the committed files.

```bash
cd StreamTier.API

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=streamtier;Username=postgres;Password=postgres"
dotnet user-secrets set "Jwt:SecretKey" "<at least 32 characters>"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
dotnet user-secrets set "AdminSeed:Password" "<admin password>"
dotnet user-secrets set "Email:Username" "<smtp username>"
dotnet user-secrets set "Email:Password" "<smtp password>"

dotnet run
```

Migrations are applied automatically at startup, so the schema, the seeded plans, the `Admin` and `User` roles, and the admin account (`AdminSeed:Email`) are created on first run.

### With Docker

The only requirement is Docker. Compose starts PostgreSQL and the API on port 5148, the same port as the `http` launch profile, so the Stripe commands below work unchanged. Secrets come from a `.env` file, which git ignores.

```bash
cp .env.example .env
docker compose up --build
```

### Admin account

| Credential | `dotnet run` | Docker |
| --- | --- | --- |
| Email | `AdminSeed:Email` in `appsettings.Development.json` | Same setting, baked into the image at build |
| Password | `AdminSeed:Password` in user secrets | `ADMIN_PASSWORD` in `.env` |

The account is created once, when no user with that email exists. Changing the password setting later doesn't update it, and changing the email creates a second admin. The password must meet ASP.NET Core Identity's default rules (at least 6 characters, with upper case, lower case, digit and symbol); otherwise the failure is logged and no admin is created.

### Running the tests

```bash
dotnet test --project StreamTierUnitTests
```

Or without the .NET SDK:

```bash
docker compose run --rm --build tests
```

The tests need no running services. Stripe and other external services are mocked, and database code runs against in-memory SQLite, so they don't cover PostgreSQL-specific behavior or real Stripe calls.

### Connecting Stripe

The plans start without Stripe price IDs. Log in as the admin account and call `POST /admin/bootstrap` once. It finds or creates a Stripe product and a recurring monthly price for each paid plan and stores the price IDs locally. Checkout for a paid plan only works after this step.

### Receiving webhooks

Use the [Stripe CLI](https://stripe.com/docs/stripe-cli) to forward the handled events to your local instance, and pass the signing secret it prints to `Stripe:WebhookSecret`:

```bash
stripe listen --events checkout.session.completed,invoice.paid,invoice.payment_failed,payment_intent.payment_failed,customer.subscription.updated,customer.subscription.deleted,customer.deleted --forward-to http://localhost:5148/webhooks/stripe
```

Recent versions of the CLI require the `--events` list. The default `http` launch profile listens on port 5148. If you run the `https` profile instead, forward to `https://localhost:7123/webhooks/stripe` and add `--skip-verify` so the CLI accepts the ASP.NET development certificate.

In the Stripe Dashboard, make sure the webhook endpoint is subscribed to the events listed above, and set the subscription retry behavior to cancel the subscription once retries are exhausted.

## Project layout

```
StreamTier.API/
├── Controllers/     Authentication, plans, Stripe checkout, admin bootstrap, webhook intake
├── Services/        Business logic behind interfaces, one folder per concern
├── Models/          EF Core entities (User, Subscription, SubscriptionPlan, Invoice)
├── Dtos/            Request, response, and internal transfer objects
├── Exceptions/      PermanentWebhookException for events that must not be retried
├── Data/            AppDbContext, entity configuration, plan and identity seed data
├── Migrations/      EF Core migrations
├── OpenApi/         Adds the JWT "Authorize" button to Swagger UI
└── wwwroot/         Static checkout success and cancel pages
StreamTierUnitTests/ Unit tests for the services and controllers
```

Controllers stay thin and handle HTTP concerns only; the business logic lives in services registered against interfaces, one per concern.
