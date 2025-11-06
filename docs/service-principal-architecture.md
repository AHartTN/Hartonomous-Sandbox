# Hartonomous Service Principal Architecture

## Overview
This document defines the complete service principal strategy for Hartonomous, including Entra ID (primary tenant) and External ID (CIAM) configurations.

## Primary Tenant (Entra ID)
**Tenant ID:** `6c9c44c4-f04b-4b5f-bea0-f1069179799c`  
**Domain:** `aharttngmail710.onmicrosoft.com`

## External ID (CIAM) Tenant
**Tenant Name:** `hartonomous.onmicrosoft.com`  
**Country Code:** US  
**Display Name:** Hartonomous  
**Resource:** `/subscriptions/ed614e1a-7d8b-4608-90c8-66e86c37080b/resourceGroups/rg-hartonomous/providers/Microsoft.AzureActiveDirectory/ciamDirectories/hartonomous.onmicrosoft.com`

---

## Service Principals

### 1. Hartonomous (Main Application SP)
**App ID:** `c25ed11d-c712-4574-8897-6a3a0c8dbb7f`  
**Object ID:** `98972f62-17f0-4c5e-bf4f-cc4ae85b6bd9`  
**Purpose:** Primary service principal for Hartonomous services, Azure DevOps deployment agents, and cross-resource authentication

#### Current Roles:
- ✅ **App Configuration Data Reader** - Scope: `appconfig-hartonomous`
- ✅ **Azure Connected Machine Onboarding** - Scope: `rg-hartonomous`

#### Required Additional Roles:
- ⚠️ **Key Vault Secrets User** - Scope: `kv-hartonomous` (for service runtime secret access)
- ⚠️ **Storage Blob Data Contributor** - Scope: `hartonomousstorage` (for FILESTREAM/blob operations)
- ⚠️ **Monitoring Metrics Publisher** - Scope: `hartonomous-insights` (for custom telemetry)
- ⚠️ **Azure DevOps Deployment Group Administrator** - Manual grant in DevOps portal for "Primary Local" group

#### Secrets:
- Current secret stored in Key Vault as `HART-SERVER-Management-Secret` (used for DevOps agent auth)

---

### 2. Arc Machine Managed Identities

#### HART-DESKTOP (System Assigned)
**Principal ID:** `505c61a6-bcd6-4f22-aee5-5c6c0094ae0d`  
**Resource:** `Microsoft.HybridCompute/machines/HART-DESKTOP`  
**Purpose:** Managed identity for HART-DESKTOP Arc machine

##### Required Roles:
- ⚠️ **Key Vault Secrets User** - Scope: `kv-hartonomous` (for local SQL Server to access connection strings)
- ⚠️ **App Configuration Data Reader** - Scope: `appconfig-hartonomous` (for configuration access)

#### HART-SERVER (System Assigned)
**Principal ID:** `50c98169-43ea-4ee7-9daa-d752ed328994`  
**Resource:** `Microsoft.HybridCompute/machines/hart-server`  
**Purpose:** Managed identity for HART-SERVER Arc machine (runs .NET services)

##### Required Roles:
- ⚠️ **Key Vault Secrets User** - Scope: `kv-hartonomous` (for service access to secrets)
- ⚠️ **App Configuration Data Reader** - Scope: `appconfig-hartonomous` (for configuration access)
- ⚠️ **Storage Blob Data Contributor** - Scope: `hartonomousstorage` (for services to write blobs)
- ⚠️ **Monitoring Metrics Publisher** - Scope: `hartonomous-insights` (for Application Insights telemetry)

---

### 3. Database Service Principals (Arc SQL Server Identities)

#### hart-server-sql-identity
**App ID:** `2c203e55-909a-4b86-a04d-aa1c7de7a460`  
**Object ID:** `dade70d5-2a03-44fc-89f2-d8c5db28ab50`  
**Purpose:** Service principal for Arc-enabled SQL Server on HART-SERVER

##### Required Roles:
- ⚠️ **Key Vault Secrets User** - Scope: `kv-hartonomous` (for SQL Server to access credentials)

#### HART-SERVER-MSSQL
**App ID:** `e92b6cbf-8d5c-4f13-9bba-83520d0ef65c`  
**Object ID:** `60fa4990-985a-4ca4-bbe3-f3dfc1a23ea3`  
**Purpose:** Additional SQL Server identity for HART-SERVER

#### Arc SQL Server Extensions
- `HART-DESKTOP/WindowsAgent.SqlServer` - SQL Server extension on Windows
- `hart-server/LinuxAgent.SqlServer` - SQL Server extension on Linux

---

### 4. Infrastructure Service Principals

#### HART-SERVER-Management
**App ID:** `1a9c7caa-c5c9-4b39-8307-03b1134a70c3`  
**Object ID:** `f29aea3f-11fa-4988-9559-92a749855164`  
**Purpose:** Management identity for HART-SERVER

##### Current Roles:
- ✅ **Azure Connected Machine Onboarding** - Scope: `Development` resource group
- ✅ **User Access Administrator** - Scope: `Development` resource group

#### HART-DESKTOP-Management
**App ID:** `b7e68645-c895-4889-a29f-e153f9b0ae2d`  
**Object ID:** `f4044c9d-3771-4283-8e83-f5f35fdba01a`  
**Purpose:** Management identity for HART-DESKTOP

#### HART-MCP
**App ID:** `60ef8898-133b-4be9-bf8d-a8aa6c4a57ee`  
**Object ID:** `785eee91-9445-4800-95a3-6d5dc3aa09b4`  
**Purpose:** Model Context Protocol server identity

##### Current Roles:
- ✅ **Contributor** - Scope: `HART-MCP` resource group

---

### 5. Specialized Service Principals

#### HART-DNS
**App ID:** `2e014e3e-3812-4468-898a-5814739b6256`  
**Object ID:** `ba9fedb6-c6ce-4e61-8b74-bb555ee8c180`  
**Purpose:** DNS zone management for ACME/Let's Encrypt certificates

##### Current Roles:
- ✅ **DNS Zone Contributor** - Scope: `Hartonomous.com` DNS zone
- ✅ **DNS Zone Contributor** - Scope: `gachanft.com` DNS zone
- ✅ **DNS Zone Contributor** - Scope: `jsontonft.com` DNS zone

#### hartonomous-router-ddns
**App ID:** `964265e5-df90-482e-9f92-6172f1e65227`  
**Object ID:** `c12e1b9d-29e7-4bbf-b7cc-21f0f200863d`  
**Purpose:** Dynamic DNS updates from OpenWRT router

##### Current Roles:
- ✅ **DNS Zone Contributor** - Scope: `development` resource group

#### HART-SERVER-NEO4J
**App ID:** `265becec-7da5-4a57-84cb-004494d37e5b`  
**Object ID:** `a402df31-14d5-43b0-8237-8f397eae8b7e`  
**Purpose:** Neo4j database service identity

##### Required Roles:
- ⚠️ **Key Vault Secrets User** - Scope: `kv-hartonomous` (for Neo4j credentials)

#### HART-SERVER-MILVUS
**App ID:** `f98704f0-a0fe-4f64-bf90-b5b390ba8fd4`  
**Object ID:** `2c02c4ea-1155-4af3-b98e-cef3da3f6cae`  
**Purpose:** Milvus vector database service identity

##### Required Roles:
- ⚠️ **Key Vault Secrets User** - Scope: `kv-hartonomous` (for Milvus credentials)
- ⚠️ **Storage Blob Data Contributor** - Scope: `hartonomousstorage` (for vector storage)

---

## External ID (CIAM) Service Principals

### Required for Customer Authentication
The CIAM tenant (`hartonomous.onmicrosoft.com`) will need:

1. **Hartonomous API Application**
   - Purpose: Customer-facing API authentication
   - Scopes: `api://hartonomous/access_as_user`
   - Required API Permissions:
     - Microsoft Graph: `User.Read`, `openid`, `profile`, `email`
   - Redirect URIs: Production API endpoints
   - Status: ⚠️ **NOT YET CREATED**

2. **Hartonomous Web Application** (if SPA/web client)
   - Purpose: Web application authentication
   - Auth Flow: Authorization Code Flow with PKCE
   - Status: ⚠️ **NOT YET CREATED**

---

## Azure Resources Requiring Access

### Key Vault: `kv-hartonomous`
**Resource ID:** `/subscriptions/ed614e1a-7d8b-4608-90c8-66e86c37080b/resourceGroups/rg-hartonomous/providers/Microsoft.KeyVault/vaults/kv-hartonomous`

#### Required Access Policies:
- ⚠️ Hartonomous SP: Secrets Get, List
- ⚠️ HART-DESKTOP MI: Secrets Get, List
- ⚠️ HART-SERVER MI: Secrets Get, List
- ⚠️ HART-SERVER-MSSQL SP: Secrets Get, List
- ⚠️ HART-SERVER-NEO4J SP: Secrets Get, List
- ⚠️ HART-SERVER-MILVUS SP: Secrets Get, List

### App Configuration: `appconfig-hartonomous`
**Endpoint:** `https://appconfig-hartonomous.azconfig.io`

#### Required Role Assignments:
- ✅ Hartonomous SP: App Configuration Data Reader
- ⚠️ HART-DESKTOP MI: App Configuration Data Reader
- ⚠️ HART-SERVER MI: App Configuration Data Reader

### Storage Account: `hartonomousstorage`
**Resource ID:** `/subscriptions/ed614e1a-7d8b-4608-90c8-66e86c37080b/resourceGroups/rg-hartonomous/providers/Microsoft.Storage/storageAccounts/hartonomousstorage`

#### Required Role Assignments:
- ⚠️ Hartonomous SP: Storage Blob Data Contributor
- ⚠️ HART-SERVER MI: Storage Blob Data Contributor
- ⚠️ HART-SERVER-MILVUS SP: Storage Blob Data Contributor

### Application Insights: `hartonomous-insights`
**Resource ID:** `/subscriptions/ed614e1a-7d8b-4608-90c8-66e86c37080b/resourceGroups/rg-hartonomous/providers/Microsoft.Insights/components/hartonomous-insights`

#### Required Role Assignments:
- ⚠️ Hartonomous SP: Monitoring Metrics Publisher
- ⚠️ HART-SERVER MI: Monitoring Metrics Publisher

---

## Azure DevOps Permissions

### Deployment Group: "Primary Local"
**Organization:** `https://dev.azure.com/aharttn/`  
**Project:** `Hartonomous`

#### Required Permissions:
- ⚠️ **Hartonomous SP** (`c25ed11d-c712-4574-8897-6a3a0c8dbb7f`): Administrator role
  - **Status:** BLOCKING - Must be granted manually in DevOps portal
  - **Impact:** Prevents agent registration on HART-DESKTOP and HART-SERVER
  - **Action:** Project Settings → Deployment groups → Primary Local → Security → Add SP with Administrator role

---

## Implementation Checklist

### Immediate (Blocking Deployment)
- [ ] Grant Hartonomous SP Administrator access to "Primary Local" deployment group in Azure DevOps
- [ ] Assign Key Vault Secrets User to Hartonomous SP on `kv-hartonomous`
- [ ] Assign Key Vault Secrets User to HART-SERVER MI on `kv-hartonomous`
- [ ] Assign App Configuration Data Reader to HART-SERVER MI on `appconfig-hartonomous`

### High Priority (Required for Service Runtime)
- [ ] Assign Storage Blob Data Contributor to HART-SERVER MI on `hartonomousstorage`
- [ ] Assign Monitoring Metrics Publisher to HART-SERVER MI on `hartonomous-insights`
- [ ] Assign Key Vault Secrets User to HART-DESKTOP MI on `kv-hartonomous`
- [ ] Assign App Configuration Data Reader to HART-DESKTOP MI on `appconfig-hartonomous`

### Database Services
- [ ] Assign Key Vault Secrets User to HART-SERVER-NEO4J SP on `kv-hartonomous`
- [ ] Assign Key Vault Secrets User to HART-SERVER-MILVUS SP on `kv-hartonomous`
- [ ] Assign Storage Blob Data Contributor to HART-SERVER-MILVUS SP on `hartonomousstorage`
- [ ] Assign Key Vault Secrets User to hart-server-sql-identity SP on `kv-hartonomous`

### External ID (CIAM) Setup
- [ ] Create app registration in CIAM tenant for customer authentication
- [ ] Configure API permissions and scopes
- [ ] Set up redirect URIs for production
- [ ] Configure token lifetimes and policies
- [ ] Set up user flows for sign-up/sign-in
- [ ] Configure branding and customization

### Monitoring & Validation
- [ ] Verify all role assignments propagate (can take 5-10 minutes)
- [ ] Test Hartonomous SP can access Key Vault secrets
- [ ] Test HART-SERVER MI can access App Configuration
- [ ] Test service authentication using DefaultAzureCredential
- [ ] Validate Application Insights telemetry flowing from services
- [ ] Test blob storage operations from services

---

## Authentication Flow

### Service Runtime Authentication
Services running on HART-SERVER will use **DefaultAzureCredential** which tries:
1. **Environment variables** (not used in our setup)
2. **Managed Identity** (HART-SERVER System Assigned MI: `50c98169-43ea-4ee7-9daa-d752ed328994`)
3. **Visual Studio** (not available on server)
4. **Azure CLI** (fallback, but MI should work)

### Azure DevOps Agent Authentication
- Agents use **Service Principal** (`c25ed11d-c712-4574-8897-6a3a0c8dbb7f`) for initial registration
- Credentials stored in agent configuration
- Used for ongoing DevOps communication

### Customer Authentication (CIAM)
- Customers authenticate against External ID tenant (`hartonomous.onmicrosoft.com`)
- API validates tokens from CIAM tenant
- Services use their own identities (MI or SP) for backend Azure resource access

---

## Security Best Practices

1. **Principle of Least Privilege**: Each identity only gets minimum required permissions
2. **Managed Identities First**: Use Arc machine managed identities for services instead of SP where possible
3. **Secret Rotation**: Hartonomous SP secret should be rotated every 90 days
4. **Scope Minimization**: Assignments scoped to specific resources, not subscription-wide
5. **Audit Logging**: All Key Vault access is logged and monitored
6. **RBAC over Access Policies**: Key Vault uses RBAC for modern permission management
7. **Separate Tenants**: Customer auth (CIAM) isolated from infrastructure auth (Entra ID)

---

## Notes

- Arc machine managed identities are **preferred** over service principals for services running on the machines
- The "Hartonomous" SP is primarily for Azure DevOps deployment agents and cross-cutting concerns
- CIAM tenant is separate and requires its own app registrations
- All database service identities (Neo4j, Milvus, SQL) should access Key Vault for their credentials
- DNS service principals (HART-DNS, hartonomous-router-ddns) are correctly configured for ACME challenges
