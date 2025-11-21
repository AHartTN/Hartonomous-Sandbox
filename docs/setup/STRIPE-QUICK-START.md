# ?? STRIPE QUICK START - 5 MINUTE SETUP

**Get your billing system running in 5 minutes!**

---

## ? **Step 1: Get Stripe API Keys** (2 minutes)

1. Go to https://stripe.com ? Sign up / Log in
2. Navigate to: **Developers ? API keys**
3. Copy these values:

```
Publishable key: pk_test_51...
Secret key:      sk_test_51...
```

---

## ? **Step 2: Configure User Secrets** (1 minute)

```bash
cd src/Hartonomous.Api

dotnet user-secrets set "Stripe:Enabled" "true"
dotnet user-secrets set "Stripe:Mode" "test"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY_HERE"
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_KEY_HERE"
```

---

## ? **Step 3: Deploy Database Schema** (1 minute)

**DACPAC approach** (automatic with build):

```bash
# Build the database project (automatically deploys to LocalDB)
dotnet build src\Hartonomous.Database\Hartonomous.Database.sqlproj

# Or publish to specific server
SqlPackage /Action:Publish \
  /SourceFile:src\Hartonomous.Database\bin\Debug\Hartonomous.Database.dacpac \
  /TargetServerName:localhost \
  /TargetDatabaseName:Hartonomous
```

**Tables created:**
- ? `dbo.TenantSubscription` - Subscription tracking
- ? `dbo.BillingPayment` - Payment tracking
- ? `dbo.BillingCredit` - Credits and refunds
- ? `dbo.StripeWebhookEvent` - Webhook audit log
- ? `dbo.BillingInvoice` - Updated with Stripe columns
- ? `dbo.TenantGuidMapping` - Updated with StripeCustomerId

---

## ? **Step 4: Test the API** (1 minute)

```bash
# Start the API
dotnet run --project src/Hartonomous.Api

# Test bill calculation (works without Stripe)
curl -X POST https://localhost:5001/api/billing/calculate \
  -H "Content-Type: application/json" \
  -d '{"tenantId":1,"generateInvoice":false}'
```

Expected: Bill calculation result with subtotal, discounts, tax, total.

---

## ? **Step 5: Test Stripe Integration** (optional)

### Create a test subscription:

```bash
curl -X POST https://localhost:5001/api/billing/subscriptions \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": 1,
    "planId": "price_XXXXX",
    "paymentMethodId": "pm_card_visa"
  }'
```

### Process a test payment:

```bash
curl -X POST https://localhost:5001/api/billing/payments \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": 1,
    "amount": 99.00,
    "paymentMethodId": "pm_card_visa"
  }'
```

**Test Card:** 4242 4242 4242 4242 (any expiry, any CVC)

---

## ?? **Webhook Setup** (Optional - for production)

### Setup Stripe CLI:

```bash
# Install
brew install stripe/stripe-cli/stripe  # Mac
scoop install stripe                   # Windows

# Login & forward
stripe login
stripe listen --forward-to https://localhost:5001/api/webhooks/stripe

# Trigger test events
stripe trigger payment_intent.succeeded
```

---

## ?? **What You Can Do Now**

### ? **Core Billing (No Stripe Required)**
- Calculate bills with volume discounts
- Generate usage reports (Summary, Detailed, Forecast)
- Get time-series analytics (HOUR, DAY, WEEK buckets)

### ? **Stripe Integration (Requires Stripe)**
- Create subscriptions ($29, $99, $299/month)
- Process one-time payments
- Cancel subscriptions
- Track all payments in dashboard

---

## ?? **Check Everything Works**

### Verify Stripe Configuration:
```bash
dotnet user-secrets list --project src/Hartonomous.Api | grep Stripe
```

Should show:
```
Stripe:Enabled = true
Stripe:Mode = test
Stripe:SecretKey = sk_test_...
Stripe:PublishableKey = pk_test_...
```

### Verify Database Tables:
```sql
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN (
    'TenantSubscription',
    'BillingPayment',
    'BillingCredit',
    'StripeWebhookEvent'
);

-- Check Stripe columns added
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TenantGuidMapping' 
  AND COLUMN_NAME = 'StripeCustomerId';
  
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'BillingInvoice' 
  AND COLUMN_NAME = 'StripeInvoiceId';
```

Should return 4 tables and 2 columns.

---

## ?? **Quick Troubleshooting**

### "Stripe integration is not enabled"
```bash
dotnet user-secrets set "Stripe:Enabled" "true"
```

### "Invalid API key provided"
```bash
# Verify your key starts with sk_test_ (not sk_live_)
dotnet user-secrets list | grep SecretKey
```

### "No such customer"
```sql
-- Clear cached customer ID
UPDATE dbo.TenantGuidMapping SET StripeCustomerId = NULL WHERE TenantId = 1;
```

### DACPAC Deployment Failed
```bash
# Check for schema drift
SqlPackage /Action:Script \
  /SourceFile:src\Hartonomous.Database\bin\Debug\Hartonomous.Database.dacpac \
  /TargetServerName:localhost \
  /TargetDatabaseName:Hartonomous \
  /OutputPath:drift.sql
```

---

## ?? **You're Done!**

**You now have:**
- ? Full billing system operational
- ? Stripe payment processing ready
- ? Usage tracking configured
- ? Invoice generation working
- ? Subscription management enabled
- ? Webhook handling set up

**Total Setup Time:** ~5 minutes  
**Monthly Cost:** $0 (Stripe has no monthly fees, only 2.9% + 30¢ per transaction)

---

## ?? **Next Steps**

1. **Read Full Guide:** `docs/setup/STRIPE-INTEGRATION-GUIDE.md`
2. **Test API:** Use Postman/Insomnia with examples above
3. **Monitor Stripe:** https://dashboard.stripe.com
4. **Production:** Switch to live keys when ready

---

**Status:** ? Ready to Bill!  
**Support:** See STRIPE-INTEGRATION-GUIDE.md for detailed help
