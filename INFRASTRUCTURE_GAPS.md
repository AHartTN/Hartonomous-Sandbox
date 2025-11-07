# Infrastructure Configuration Gaps & Unknown State
**Reality Check:** 2025-11-07

## ❌ CRITICAL ISSUES I DON'T KNOW THE ANSWERS TO

### 1. Which Server Is The Deployment Target?
**Problem:** Pipelines hardcoded to `hart-server`, but you asked about `HART-DESKTOP`

**What I hardcoded:**
- `azure-pipelines.yml` line 110: `SQL_SERVER: 'hart-server'`
- `.github/workflows/ci-cd.yml`: Uses secrets for Arc server (not specified)

**What I don't know:**
- Is SQL Server on HART-DESKTOP or hart-server?
- Is Neo4j on HART-DESKTOP or hart-server?
- Are they on BOTH machines (local dev vs production)?
- Which machine should the CI/CD pipeline target?

**From your Arc audit, you have TWO Arc agents:**
- HART-DESKTOP (Windows desktop?)
- hart-server (Linux server?)

**NEEDS VERIFICATION:** Which server hosts which databases in which environment?

---

### 2. Entra ID Configuration
**Problem:** I added code that uses DefaultAzureCredential, but I don't know if Entra ID is configured

**What I assumed:**
- Azure Arc provides managed identity via HIMDS
- DefaultAzureCredential will authenticate
- Azure App Configuration trusts the managed identity

**What I don't know:**
- Is there an Entra ID app registration for Hartonomous?
- What's the Application (client) ID?
- Are the managed identities assigned to the Arc agents?
- Do the managed identities have the required RBAC roles?

**NEEDS CONFIGURATION:**
```
Azure Portal → Entra ID → App registrations
- Create app registration "Hartonomous-Production"
- Note Application (client) ID
- Note Directory (tenant) ID

Azure Portal → Arc Servers
- HART-DESKTOP → Identity → System assigned: ON
- hart-server → Identity → System assigned: ON

Azure Portal → App Configuration → Access control (IAM)
- Add role assignment: "App Configuration Data Reader"
- Assign to: HART-DESKTOP (managed identity)
- Assign to: hart-server (managed identity)

Azure Portal → Key Vault → Access control (IAM)
- Add role assignment: "Key Vault Secrets User"
- Assign to: HART-DESKTOP (managed identity)
- Assign to: hart-server (managed identity)
```

---

### 3. External ID (What Is This?)
**Problem:** You mentioned "External ID" - I don't know what this refers to

**Possibilities:**
- Azure AD B2C External Identities?
- Verified ID?
- Something else entirely?

**NEEDS CLARIFICATION:** What is "External ID" in your architecture?

---

### 4. Service Accounts
**Problem:** I changed systemd services to run as `hartonomous` user, but I don't know if this user exists

**What I changed:**
```ini
# Before
User=ahart

# After
User=hartonomous
```

**What I don't know:**
- Does the `hartonomous` user exist on hart-server?
- Does it exist on HART-DESKTOP?
- What groups is it in?
- What permissions does it have?

**NEEDS VERIFICATION:**
```bash
# On target server
id hartonomous
groups hartonomous

# Should see:
uid=1001(hartonomous) gid=1001(hartonomous) groups=1001(hartonomous)

# If doesn't exist:
sudo useradd -m -s /bin/bash hartonomous
sudo usermod -aG docker hartonomous  # if using Docker
```

---

### 5. RBAC Role Assignments
**Problem:** Code uses managed identity, but I don't know if RBAC roles are assigned

**Required Azure roles for managed identity:**

| Resource | Role | Scope |
|----------|------|-------|
| App Configuration | App Configuration Data Reader | appconfig-hartonomous |
| Key Vault | Key Vault Secrets User | kv-hartonomous |
| SQL Database | (not applicable - uses SQL auth) | N/A |
| Storage Account | Storage Blob Data Contributor | (if using Azure Storage) |
| Event Hubs | Azure Event Hubs Data Receiver | (if using Event Hubs) |

**NEEDS VERIFICATION:**
```bash
# Check role assignments for Arc agent managed identity
az role assignment list --assignee <managed-identity-object-id> --all

# Expected output should show:
# - App Configuration Data Reader on appconfig-hartonomous
# - Key Vault Secrets User on kv-hartonomous
```

---

### 6. Azure Arc Agent Configuration
**Problem:** I don't know if Arc agents are configured correctly for SQL Server

**What Arc agents need:**
- Azure Connected Machine agent installed
- SQL Server Best Practices Assessment enabled
- Managed identity enabled
- Proper network connectivity to Azure (outbound 443)

**NEEDS VERIFICATION:**
```powershell
# On HART-DESKTOP or hart-server
azcmagent show

# Expected output:
# - Agent Status: Connected
# - Resource Name: HART-DESKTOP or hart-server
# - Resource Group: <your-rg>
# - Subscription ID: <your-sub-id>
```

---

### 7. Azure App Configuration Values
**Problem:** Code references App Configuration, but I don't know if values are populated

**What needs to be in App Configuration:**

| Key | Value | Type |
|-----|-------|------|
| `ConnectionStrings:HartonomousDb` | SQL connection string | Key Vault reference |
| `Neo4j:Uri` | bolt://localhost:7687 or production URI | String |
| `Neo4j:Username` | neo4j | Key Vault reference |
| `Neo4j:Password` | (password) | Key Vault reference |
| `Azure:EventHubs:ConnectionString` | Event Hub connection | Key Vault reference |
| `ApplicationInsights:ConnectionString` | App Insights connection | Key Vault reference |

**Key Vault reference syntax:**
```json
{
  "@Microsoft.KeyVault(SecretUri=https://kv-hartonomous.vault.azure.net/secrets/Neo4j-Password/)"
}
```

**NEEDS VERIFICATION:**
```bash
# List all keys in App Configuration
az appconfig kv list --name appconfig-hartonomous

# Should see 10-15 configuration keys
```

---

### 8. Hardcoded Credentials Still Present
**Problem:** Found hardcoded Neo4j credentials

**File:** `src/Hartonomous.Infrastructure/appsettings.json` lines 71-74
```json
"Neo4j": {
  "Uri": "bolt://localhost:7687",
  "Username": "neo4j",
  "Password": "neo4jneo4j"  // ← HARDCODED
}
```

**Also in other appsettings.json files:**
- `src/Neo4jSync/appsettings.json`
- Other service projects

**NEEDS FIXING:** Replace with empty values and App Configuration references

---

### 9. Neo4j Deployment
**Problem:** I focused on SQL Server but didn't address Neo4j deployment at all

**Questions:**
- Where is Neo4j hosted? HART-DESKTOP? hart-server? Both?
- Is Neo4j containerized (Docker) or installed directly?
- What version of Neo4j?
- Is it configured for clustering?
- Where are the Neo4j backups?
- How do we deploy Neo4j schema changes?

**NEEDS INVESTIGATION:**
```bash
# Check if Neo4j is running
systemctl status neo4j  # Linux
# or
docker ps | grep neo4j  # Docker

# Check Neo4j version
cypher-shell --version
```

---

### 10. SQL Server Configuration on Arc Agents
**Problem:** Don't know the SQL Server configuration on each machine

**Questions:**
- SQL Server version on HART-DESKTOP? (2022? 2025?)
- SQL Server version on hart-server? (2022? 2025?)
- Is CLR integration enabled?
- Is FILESTREAM enabled?
- Is Service Broker enabled?
- Max memory configuration?
- Backup strategy?

**NEEDS VERIFICATION:**
```sql
-- Run on each SQL Server instance
SELECT @@VERSION;
SELECT name, value_in_use FROM sys.configurations WHERE name IN ('clr enabled', 'filestream access level');
SELECT name, is_broker_enabled FROM sys.databases WHERE name = 'Hartonomous';
```

---

### 11. Network Connectivity
**Problem:** Don't know if Arc agents can reach Azure services

**Required connectivity:**
- HTTPS (443) to Azure Arc endpoints
- HTTPS (443) to App Configuration endpoint
- HTTPS (443) to Key Vault endpoint
- HTTPS (443) to Azure Monitor (if using)

**NEEDS VERIFICATION:**
```powershell
# Test connectivity from Arc agent
Test-NetConnection appconfig-hartonomous.azconfig.io -Port 443
Test-NetConnection kv-hartonomous.vault.azure.net -Port 443
Test-NetConnection login.microsoftonline.com -Port 443
```

---

### 12. CI/CD Pipeline Credentials
**Problem:** Don't know if Azure DevOps service connections are configured

**Azure DevOps needs:**
- Service connection to Azure subscription
- SSH service connection to hart-server (or HART-DESKTOP?)
- SQL Server credentials stored as pipeline variables/secrets

**NEEDS VERIFICATION:**
Azure DevOps → Project Settings → Service connections
- Should see: "hart-server-ssh" (referenced in pipeline line 201)
- Should see: Azure Resource Manager connection

**Pipeline variables needed:**
- `SQL_USERNAME` (secret)
- `SQL_PASSWORD` (secret)
- `ARC_SERVER_HOST` (variable)

---

## 🔍 HONEST ASSESSMENT

### What I Actually Fixed
✅ Consolidated 6 EF migrations into 1 clean migration
✅ Fixed 2 bugs in azure-pipelines.yml (wrong path, redundant generation)
✅ Created local deployment script (deploy-local.ps1)
✅ Created seed data script (seed-data.sql)
✅ Created GitHub Actions workflow
✅ Removed hardcoded Application Insights connection string from ModelIngestion

### What I Assumed (Without Verification)
❌ That deployment target is "hart-server" (might be HART-DESKTOP)
❌ That Entra ID app registration exists
❌ That managed identities are assigned to Arc agents
❌ That RBAC roles are assigned
❌ That `hartonomous` user exists on target servers
❌ That Azure App Configuration is populated with values
❌ That Key Vault secrets exist
❌ That Arc agents are properly configured
❌ That Neo4j is deployed and configured
❌ That pipelines can actually connect to servers

### What I Haven't Tested
❌ End-to-end local deployment (deploy-local.ps1)
❌ End-to-end Azure pipeline deployment
❌ GitHub Actions workflow (can't test without GitHub repo)
❌ Service startup (systemd services)
❌ Application functionality
❌ Database queries with CLR functions
❌ Neo4j connectivity

---

## 📋 VERIFICATION CHECKLIST (MUST DO BEFORE CLAIMING "PRODUCTION READY")

### Infrastructure
- [ ] Verify which server hosts SQL Server (HART-DESKTOP vs hart-server)
- [ ] Verify which server hosts Neo4j
- [ ] Update pipeline target to correct server
- [ ] Create/verify `hartonomous` service account on target server
- [ ] Configure Entra ID app registration
- [ ] Enable managed identity on Arc agents
- [ ] Assign RBAC roles to managed identities
- [ ] Populate Azure App Configuration with all keys
- [ ] Create Key Vault secrets with actual values
- [ ] Remove all hardcoded credentials from appsettings.json files

### Testing
- [ ] Test deploy-local.ps1 on HART-DESKTOP
- [ ] Verify database deploys successfully
- [ ] Verify CLR functions work
- [ ] Verify stored procedures execute
- [ ] Test Azure DevOps pipeline end-to-end
- [ ] Test GitHub Actions workflow (if using GitHub)
- [ ] Verify systemd services start and run
- [ ] Test application functionality
- [ ] Test Neo4j connectivity

### Documentation
- [ ] Document actual server topology
- [ ] Document Entra ID configuration
- [ ] Document RBAC role assignments
- [ ] Document network requirements
- [ ] Update deployment docs with verified steps

---

## 🎯 NEXT STEPS (ACTUAL NEXT STEPS)

1. **Tell me the truth about your infrastructure:**
   - Where is SQL Server? (HART-DESKTOP, hart-server, or both?)
   - Where is Neo4j?
   - What's the "External ID" you mentioned?
   - Do you have Entra ID configured already?

2. **I'll update the pipelines to target the correct servers**

3. **I'll create a checklist for Azure configuration**

4. **You test deploy-local.ps1 and tell me what breaks**

5. **We fix whatever breaks**

6. **Then we can actually claim it's production ready**

---

## 💡 REALITY

**I cleaned up the deployment scripts and consolidated migrations, but I have NO IDEA if your actual Azure infrastructure is configured correctly.**

I can't verify:
- Azure configuration
- Arc agent setup
- Network connectivity
- Service accounts
- RBAC roles
- Entra ID

**I need you to tell me what's actually configured so I can fix what's wrong.**
