# Hartonomous Stripe Integration Setup Guide

**Enterprise-Grade Billing with Full Stripe Integration**

---

## ?? **Overview**

The Hartonomous billing system provides:
- ? Usage tracking and metering
- ? Automated invoice generation
- ? Stripe payment processing
- ? Subscription management
- ? Webhook event handling
- ? Multi-tenant billing isolation
- ? Volume discounts and credits
- ? Usage analytics and forecasting

---

## ?? **Prerequisites**

### 1. Stripe Account Setup

1. **Create Stripe Account**
   - Go to https://stripe.com
   - Sign up for a new account
   - Complete business verification (for live mode)

2. **Get API Keys**
   - Navigate to: Dashboard ? Developers ? API keys
   - Copy your **Test** keys:
     - Publishable key: `pk_test_...`
     - Secret key: `sk_test_...`
   - Copy your **Live** keys (when ready for production):
     - Publishable key: `pk_live_...`
     - Secret key: `sk_live_...`

3. **Create Webhook Endpoint**
   - Navigate to: Dashboard ? Developers ? Webhooks
   - Click "Add endpoint"
   - Enter URL: `https://your-domain.com/api/webhooks/stripe`
   - Select events to listen for:
     - ? `invoice.created`
     - ? `invoice.paid`
     - ? `invoice.payment_failed`
     - ? `invoice.finalized`
     - ? `payment_intent.succeeded`
     - ? `payment_intent.payment_failed`
     - ? `customer.subscription.created`
     - ? `customer.subscription.updated`
     - ? `customer.subscription.deleted`
     - ? `customer.subscription.trial_will_end`
     - ? `charge.refunded`
   - Copy the **Signing secret**: `whsec_...`

4. **Create Pricing Plans** (Optional - for subscriptions)
   - Navigate to: Dashboard ? Products
   - Click "Add product"
   - Create pricing tiers:
     - **Starter**: $29/month
     - **Professional**: $99/month
     - **Enterprise**: $299/month
   - Copy the Price IDs: `price_...`

---

## ?? **Configuration**

### 1. User Secrets (Development)

**For local development**, use User Secrets to avoid committing keys:

```bash
# Navigate to API project
cd src/Hartonomous.Api

# Initialize user secrets
dotnet user-secrets init

# Set Stripe configuration
dotnet user-secrets set "Stripe:Enabled" "true"
dotnet user-secrets set "Stripe:Mode" "test"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_SECRET_KEY_HERE"
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_PUBLISHABLE_KEY_HERE"
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_YOUR_WEBHOOK_SECRET_HERE"
```

### 2. appsettings.json (Base Configuration)

```json
{
  "Stripe": {
    "Enabled": false,
    "Mode": "test",
    "SecretKey": "",
    "PublishableKey": "",
    "WebhookSecret": "",
    "DefaultCurrency": "usd",
    "AutoSyncInvoices": true,
    "DefaultPaymentTermsDays": 30,
    "EnableSmartRetries": true
  }
}
```

### 3. Azure Key Vault (Production)

**For production**, store secrets in Azure Key Vault:

```bash
# Create Key Vault
az keyvault create \
  --name hartonomous-keyvault \
  --resource-group hartonomous-rg \
  --location eastus

# Set Stripe secrets
az keyvault secret set \
  --vault-name hartonomous-keyvault \
  --name "Stripe--SecretKey" \
  --value "sk_live_YOUR_SECRET_KEY"

az keyvault secret set \
  --vault-name hartonomous-keyvault \
  --name "Stripe--PublishableKey" \
  --value "pk_live_YOUR_PUBLISHABLE_KEY"

az keyvault secret set \
  --vault-name hartonomous-keyvault \
  --name "Stripe--WebhookSecret" \
  --value "whsec_YOUR_WEBHOOK_SECRET"
```

**Update Program.cs** to load from Key Vault:

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://hartonomous-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

### 4. Environment Variables

Alternatively, use environment variables:

```bash
# Windows (PowerShell)
$env:Stripe__Enabled = "true"
$env:Stripe__Mode = "test"
$env:Stripe__SecretKey = "sk_test_..."
$env:Stripe__PublishableKey = "pk_test_..."
$env:Stripe__WebhookSecret = "whsec_..."

# Linux/Mac (Bash)
export Stripe__Enabled=true
export Stripe__Mode=test
export Stripe__SecretKey=sk_test_...
export Stripe__PublishableKey=pk_test_...
export Stripe__WebhookSecret=whsec_...
```

---

## ??? **Database Setup**

### 1. Run Migration Script

```bash
# Using sqlcmd
sqlcmd -S localhost -d Hartonomous \
  -i src/Hartonomous.Database/Migrations/002_Add_Billing_Stripe_Schema.sql

# Or using SQL Server Management Studio
# Open and execute: src/Hartonomous.Database/Migrations/002_Add_Billing_Stripe_Schema.sql
```

### 2. Verify Tables Created

```sql
-- Check that billing tables exist
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN (
    'TenantSubscription',
    'BillingPayment',
    'BillingCredit',
    'StripeWebhookEvent'
);

-- Verify Stripe columns added
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Tenant' 
  AND COLUMN_NAME = 'StripeCustomerId';
```

---

## ?? **Testing**

### 1. Test Configuration

```csharp
// In Startup.cs or Program.cs
var stripeOptions = builder.Configuration.GetSection(StripeOptions.SectionName).Get<StripeOptions>();
if (stripeOptions?.Enabled == true)
{
    Console.WriteLine($"? Stripe enabled: Mode={stripeOptions.Mode}");
    Console.WriteLine($"? Secret key: {stripeOptions.SecretKey.Substring(0, 10)}...");
}
else
{
    Console.WriteLine("?? Stripe disabled - billing will operate in local mode");
}
```

### 2. Test API Endpoints

```bash
# Calculate bill (local mode - no Stripe required)
curl -X POST https://localhost:5001/api/billing/calculate \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": 1,
    "periodStart": "2025-01-01T00:00:00Z",
    "periodEnd": "2025-02-01T00:00:00Z",
    "generateInvoice": false
  }'

# Generate usage report
curl https://localhost:5001/api/billing/reports/1?reportType=Summary&timeRange=Month

# Get analytics
curl "https://localhost:5001/api/billing/analytics/1?startDate=2025-01-01&endDate=2025-01-31"
```

### 3. Test Stripe Integration

**Requires Stripe enabled**

```bash
# Create subscription
curl -X POST https://localhost:5001/api/billing/subscriptions \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": 1,
    "planId": "price_1ABC123xyz",
    "paymentMethodId": "pm_card_visa"
  }'

# Process payment
curl -X POST https://localhost:5001/api/billing/payments \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": 1,
    "amount": 99.00,
    "paymentMethodId": "pm_card_visa",
    "description": "One-time payment"
  }'
```

### 4. Test Stripe Test Cards

Stripe provides test card numbers for testing:

```
Success:
  4242 4242 4242 4242 (Visa)
  5555 5555 5555 4444 (Mastercard)
  
Decline:
  4000 0000 0000 0002 (Generic decline)
  
Insufficient funds:
  4000 0000 0000 9995
  
Requires authentication (3D Secure):
  4000 0025 0000 3155
```

Use:
- **Expiry:** Any future date (e.g., 12/34)
- **CVC:** Any 3 digits (e.g., 123)
- **ZIP:** Any 5 digits (e.g., 12345)

---

## ?? **Webhook Testing**

### 1. Use Stripe CLI (Recommended)

```bash
# Install Stripe CLI
# Windows: scoop install stripe
# Mac: brew install stripe/stripe-cli/stripe
# Linux: Download from https://stripe.com/docs/stripe-cli

# Login
stripe login

# Forward webhooks to localhost
stripe listen --forward-to https://localhost:5001/api/webhooks/stripe

# Trigger test events
stripe trigger payment_intent.succeeded
stripe trigger invoice.paid
stripe trigger customer.subscription.created
```

### 2. Use ngrok (Alternative)

```bash
# Install ngrok: https://ngrok.com/download

# Expose localhost
ngrok http https://localhost:5001

# Use ngrok URL in Stripe webhook configuration
# Example: https://abc123.ngrok.io/api/webhooks/stripe
```

### 3. Verify Webhook Events

```sql
-- Check webhook events received
SELECT TOP 10 
    StripeEventId,
    EventType,
    ProcessedSuccessfully,
    ReceivedAt
FROM dbo.StripeWebhookEvent
ORDER BY ReceivedAt DESC;
```

---

## ?? **Monitoring**

### 1. Stripe Dashboard

Monitor in Stripe Dashboard:
- **Payments:** Dashboard ? Payments
- **Subscriptions:** Dashboard ? Subscriptions
- **Invoices:** Dashboard ? Invoices
- **Customers:** Dashboard ? Customers
- **Webhooks:** Developers ? Webhooks ? Events

### 2. Application Logs

```bash
# View billing logs
dotnet run --project src/Hartonomous.Api | grep "Billing"

# View Stripe logs
dotnet run --project src/Hartonomous.Api | grep "Stripe"
```

### 3. Database Queries

```sql
-- Recent invoices
SELECT TOP 10 *
FROM dbo.BillingInvoice
ORDER BY GeneratedUtc DESC;

-- Subscription status
SELECT 
    t.TenantId,
    t.Name,
    ts.Status,
    ts.PlanId,
    ts.CurrentPeriodEnd
FROM dbo.Tenant t
LEFT JOIN dbo.TenantSubscription ts ON t.TenantId = ts.TenantId
WHERE ts.Status IN ('active', 'trialing');

-- Payment history
SELECT 
    bp.BillingPaymentId,
    bp.Amount,
    bp.Status,
    bp.CreatedAt
FROM dbo.BillingPayment bp
WHERE bp.TenantId = 1
ORDER BY bp.CreatedAt DESC;
```

---

## ?? **Production Checklist**

Before going live:

- [ ] **Switch to Live Keys**
  - [ ] Update SecretKey to `sk_live_...`
  - [ ] Update PublishableKey to `pk_live_...`
  - [ ] Set Mode to "live"

- [ ] **Webhook Configuration**
  - [ ] Update webhook URL to production domain
  - [ ] Copy new webhook signing secret
  - [ ] Test webhook delivery

- [ ] **Security**
  - [ ] Store keys in Azure Key Vault (not User Secrets)
  - [ ] Enable HTTPS only
  - [ ] Implement rate limiting on billing endpoints
  - [ ] Add authorization checks

- [ ] **Business Setup**
  - [ ] Complete Stripe business verification
  - [ ] Configure payout schedule
  - [ ] Set up tax collection (if required)
  - [ ] Configure email receipts

- [ ] **Testing**
  - [ ] Test end-to-end payment flow
  - [ ] Test subscription lifecycle
  - [ ] Test webhook event handling
  - [ ] Test refund processing

- [ ] **Monitoring**
  - [ ] Set up Stripe alerts
  - [ ] Configure Application Insights
  - [ ] Set up billing error alerts
  - [ ] Monitor webhook delivery

---

## ?? **Troubleshooting**

### Issue: "Stripe integration is not enabled"

**Solution:**
```bash
# Check configuration
dotnet user-secrets list

# Ensure Stripe:Enabled = true
dotnet user-secrets set "Stripe:Enabled" "true"
```

### Issue: Webhook signature verification fails

**Solution:**
```bash
# Verify webhook secret is correct
dotnet user-secrets list | grep WebhookSecret

# Update if incorrect
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
```

### Issue: Payment fails with "Invalid API key"

**Solution:**
```bash
# Verify key starts with correct prefix
# Test: sk_test_...
# Live: sk_live_...

# Update secret key
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
```

### Issue: Customer already exists error

**Solution:**
```sql
-- Check if Stripe customer ID is already stored
SELECT TenantId, StripeCustomerId
FROM dbo.Tenant
WHERE TenantId = 1;

-- If duplicate, clear it
UPDATE dbo.Tenant
SET StripeCustomerId = NULL
WHERE TenantId = 1;
```

---

## ?? **Additional Resources**

- [Stripe API Documentation](https://stripe.com/docs/api)
- [Stripe .NET SDK](https://github.com/stripe/stripe-dotnet)
- [Stripe Testing Guide](https://stripe.com/docs/testing)
- [Stripe Webhooks Guide](https://stripe.com/docs/webhooks)
- [Stripe Security Best Practices](https://stripe.com/docs/security/guide)

---

## ? **What's Included**

**Files Created:**
```
src/
??? Hartonomous.Core/
?   ??? Configuration/
?   ?   ??? StripeOptions.cs                    ? Configuration class
?   ??? Interfaces/
?       ??? Billing/
?           ??? IBillingService.cs               ? Service interface
??? Hartonomous.Infrastructure/
?   ??? Services/
?   ?   ??? Billing/
?   ?       ??? SqlBillingService.cs             ? Full implementation
?   ??? Configurations/
?       ??? BusinessServiceRegistration.cs        ? DI registration
??? Hartonomous.Api/
?   ??? Controllers/
?       ??? BillingController.cs                 ? REST API
?       ??? StripeWebhookController.cs           ? Webhook handler
??? Hartonomous.Database/
    ??? Migrations/
        ??? 002_Add_Billing_Stripe_Schema.sql    ? Database schema
```

**Features:**
- ? Usage tracking and metering
- ? Invoice generation with Stripe sync
- ? Payment processing (one-time and recurring)
- ? Subscription management
- ? Webhook event handling
- ? Volume discounts
- ? Credits and refunds
- ? Usage analytics and forecasting
- ? Multi-tenant isolation
- ? Comprehensive error handling
- ? Full logging and telemetry

---

**Status:** ? Ready for Configuration  
**Next Step:** Set up User Secrets and test with Stripe CLI
