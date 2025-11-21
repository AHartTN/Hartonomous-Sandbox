# Hartonomous Deployment Guide - TEMPLATE

**?? DO NOT COMMIT WITH REAL VALUES**  
**This is a template. Create a local copy with actual secrets:**
```bash
cp DEPLOYMENT-TEMPLATE.md DEPLOYMENT-GUIDE.local.md
# Edit DEPLOYMENT-GUIDE.local.md with real values
# .gitignore will prevent committing *.local.md files
```

---

## ?? **Security Checklist**

- [ ] All secrets stored in Azure Key Vault
- [ ] No passwords in source control
- [ ] No API keys in documentation
- [ ] No IP addresses or server names
- [ ] User secrets configured for local development
- [ ] App Configuration references Key Vault (not plain text)

---

## ??? **Infrastructure Overview**

### **Development Environment:**
- SQL Server with Hartonomous database
- API running locally
- Azure Arc enabled for monitoring
- Secrets managed via user-secrets

### **Production Environment:**
- Azure Arc-enabled SQL Server
- Samba AD Domain Controller
- Neo4j graph database  
- Docker + Kubernetes available
- CI/CD agents configured

---

## ?? **Secret Management**

### **Local Development (User Secrets):**
```bash
cd src/Hartonomous.Api
dotnet user-secrets set "Stripe:SecretKey" "YOUR_STRIPE_SECRET_KEY"
dotnet user-secrets set "Stripe:WebhookSecret" "YOUR_WEBHOOK_SECRET"
```

### **Azure Key Vault (Production):**
```bash
# Store secrets in Key Vault (never in docs)
az keyvault secret set --vault-name YOUR_VAULT_NAME \
  --name "Stripe-SecretKey" \
  --value "YOUR_SECRET"
```

### **Azure App Configuration:**
```yaml
# Reference Key Vault secrets (no plain text)
Stripe:
  SecretKey:
    uri: https://YOUR_VAULT.vault.azure.net/secrets/Stripe-SecretKey
  WebhookSecret:
    uri: https://YOUR_VAULT.vault.azure.net/secrets/Stripe-WebhookSecret
```

---

## ?? **Deployment Steps**

### **1. SQL Server Setup**

```bash
# Reset SA password (do not document the actual password)
ssh YOUR_SERVER
sudo systemctl stop mssql-server
sudo /opt/mssql/bin/mssql-conf set-sa-password
# Enter a strong password (store in Key Vault)
sudo systemctl start mssql-server
```

### **2. Database Deployment**

```bash
# Deploy via DACPAC
sqlpackage /Action:Publish \
  /SourceFile:Hartonomous.Database.dacpac \
  /TargetConnectionString:"Server=YOUR_SERVER;Database=Hartonomous;..."
```

### **3. Application Deployment**

```bash
# Deploy via CI/CD (GitHub Actions or Azure Pipelines)
# Connection strings use managed identity or Key Vault references
```

---

## ?? **Configuration**

### **Environment Variables (Example - No Real Values):**
```yaml
ConnectionStrings:
  HartonomousDb: "Server=YOUR_SERVER;Database=Hartonomous;..."
  
Stripe:
  Enabled: true
  Mode: "production"  # or "test"
  SecretKey: "REFERENCE_KEY_VAULT"
  PublishableKey: "pk_live_..." # Client-safe, can be in config
  
Azure:
  SubscriptionId: "YOUR_SUBSCRIPTION_ID"
  TenantId: "YOUR_TENANT_ID"
```

---

## ? **Verification**

### **Test Connections:**
```bash
# Test SQL Server (use Key Vault for password)
sqlcmd -S YOUR_SERVER -U sa -P "$(az keyvault secret show...)" -Q "SELECT @@VERSION"

# Test API health
curl https://YOUR_DOMAIN/health

# Test Stripe webhook
stripe listen --forward-to https://YOUR_DOMAIN/api/v1/billing/webhook
```

---

## ?? **Additional Resources**

- Azure Key Vault: https://docs.microsoft.com/azure/key-vault/
- User Secrets: https://docs.microsoft.com/aspnet/core/security/app-secrets
- Stripe Security: https://stripe.com/docs/security
- Azure App Configuration: https://docs.microsoft.com/azure/azure-app-configuration/

---

**Remember:** 
- ? Store this template in source control
- ? Never commit files with real secrets
- ? Use `*.local.md` for personal notes with actual values
- ? Always reference Key Vault in production configs
