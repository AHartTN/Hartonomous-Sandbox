# DEPLOYMENT ARCHITECTURE PLAN

**Generated**: November 11, 2025  
**Purpose**: Define deployment topology for hybrid Azure Arc SQL Server architecture  
**Status**: Planning Phase

---

## Executive Summary

**Current Infrastructure**:
- ✅ **2 Azure Arc-enabled servers** (HART-DESKTOP Windows, HART-SERVER Linux)
- ✅ **2 SQL Server 2025 Dev instances** (both Arc-connected with managed identity)
- ✅ **2 Neo4j deployments** (Desktop on Windows, Community on Linux)
- ✅ **Azure resources**: App Configuration (Standard SKU), Key Vault, Application Insights, External ID tenant
- ✅ **Build Status**: 0 errors (all projects compile successfully)
- ⚠️ **Test Status**: 24 integration test failures (require Azure/Neo4j/SQL connectivity)

**Proposed Architecture**:
- **HART-DESKTOP**: SQL Server FILESTREAM storage, Neo4j Desktop, development workstation
- **HART-SERVER**: Application hosting (.NET 10 APIs, Workers, Admin portal), Neo4j Community, linked to HART-DESKTOP SQL
- **Azure**: App Configuration, Key Vault, Application Insights, External ID (CIAM), Arc management
- **Hybrid Data Layer**: SQL Server linked servers + Service Broker messaging across both Arc servers

**Key Decision**: On-premise hybrid architecture with Azure Arc monitoring/management instead of migrating to Azure SQL Database/Managed Instance (avoids UNSAFE CLR restriction, maintains FILESTREAM support).

---

## 1. Current Azure Resource Inventory

### Azure Arc-Enabled SQL Server Instances

**Instance 1: HART-DESKTOP**
- **Machine Name**: HART-DESKTOP
- **OS**: Windows 11 Pro (10.0.26200.6899)
- **SQL Server**: MSSQLSERVER (default instance) + MSAS17\\MSSQLSERVER (Analysis Services)
- **Databases**: Hartonomous, AI-Sandbox, DWConfiguration, DWDiagnostics, DWQueue, master, model, msdb, tempdb
- **Arc Status**: Connected, SQL Server discovered (MssqlDiscovered: true)
- **Location**: East US
- **Extensions**: WindowsAgent.SqlServer
- **Role**: FILESTREAM storage, CLR assembly execution, development database

**Instance 2: hart-server**
- **Machine Name**: hart-server
- **FQDN**: hart-server
- **OS**: Ubuntu 22.04.5 LTS (5.15.0-161-generic)
- **SQL Server**: (default instance)
- **Arc Status**: Connected, SQL Server discovered (MssqlDiscovered: true)
- **Location**: East US
- **Extensions**: LinuxAgent.SqlServer, AADSSHLogin
- **Role**: Production-style linked server, Service Broker messaging, data replication target

### Azure Resources (rg-hartonomous)

**App Configuration**:
- **Name**: appconfig-hartonomous
- **SKU**: **Standard** (⚠️ ~$1.20/day = $36/month base + transaction costs)
- **Created**: August 31, 2025
- **Endpoint**: `https://appconfig-hartonomous.azconfig.io`
- **Purpose**: Centralized configuration for API, Workers, Admin portal
- **Cost Optimization Opportunity**: Consider downgrading to Free tier (1,000 requests/day limit) if usage is low

**Key Vault**:
- **Name**: kv-hartonomous
- **SKU**: (Standard assumed)
- **Purpose**: Secrets storage (connection strings, API keys), certificate management
- **Azure Arc Integration**: Managed identity authentication from Arc servers

**Application Insights**:
- **Name**: hartonomous-insights
- **Purpose**: Distributed tracing, performance monitoring, telemetry aggregation
- **Integration**: OpenTelemetry from .NET 10 apps, SQL Server telemetry via Arc

**Additional Insights**:
- **Name**: Development (in Development resource group)
- **Purpose**: (Legacy/testing - may be consolidatable)

**External ID (CIAM) Tenant**:
- **Name**: hartonomous.onmicrosoft.com
- **Type**: Microsoft.AzureActiveDirectory/ciamDirectories
- **Location**: United States
- **Purpose**: Customer identity and access management for external users
- **Integration**: Azure AD authentication for Hartonomous.Api, tenant isolation, role-based access

### Excluded Resources (Not in Use)

❌ **Azure SQL Database/Managed Instance**: Not used due to UNSAFE CLR restriction  
❌ **Azure Storage**: Not used (FILESTREAM on HART-DESKTOP instead)  
❌ **Azure Event Hub**: Not used (SQL Server Service Broker instead)  
❌ **ahdev-\* resources**: Development/testing resources, excluded from production architecture

---

## 2. Proposed Hybrid SQL Server Architecture

### Design Rationale

**Why Hybrid On-Premise + Azure Arc?**

1. **UNSAFE CLR Support**: FILESTREAM, file system operations, OS command execution require UNSAFE CLR assemblies (not supported in Azure SQL Database)
2. **FILESTREAM Limitations**: FILESTREAM is Windows-only, not supported on Linux SQL Server or Azure SQL
3. **Service Broker**: Full Service Broker support on-premise (limited in Azure SQL Managed Instance)
4. **Cost**: On-premise SQL Server 2025 Dev licenses (free for development) vs Azure SQL Managed Instance (~$600+/month)
5. **Azure Benefits**: Arc provides managed identity, monitoring, Best Practices Assessment, Defender for Cloud, backup management without migrating data to Azure

**Architecture Components**:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         AZURE CLOUD                                     │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  App Configuration (Standard)                                    │  │
│  │  - Feature flags, connection strings, tenant configs             │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  Key Vault                                                       │  │
│  │  - Secrets, certificates, managed identity auth                  │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  Application Insights                                            │  │
│  │  - OpenTelemetry traces, SQL telemetry, performance metrics      │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  External ID (hartonomous.onmicrosoft.com)                       │  │
│  │  - Customer authentication, tenant isolation, RBAC               │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  Azure Arc Control Plane                                         │  │
│  │  - Managed identity, monitoring, Best Practices, Defender        │  │
│  └──────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
                                  │
                                  │ Arc Agent, Managed Identity
                                  │
        ┌─────────────────────────┴─────────────────────────┐
        │                                                   │
        │                                                   │
┌───────▼─────────────────────────────┐    ┌──────────────▼──────────────────────────┐
│  HART-DESKTOP (Windows 11)          │    │  HART-SERVER (Ubuntu 22.04)             │
│  Arc-Enabled Server                 │    │  Arc-Enabled Server                     │
│                                     │    │                                         │
│  ┌──────────────────────────────┐   │    │  ┌─────────────────────────────────┐    │
│  │  SQL Server 2025 Dev         │◄──┼────┼─►│  SQL Server 2025 Dev            │    │
│  │  - FILESTREAM enabled        │   │    │  │  - Linked Server to HART-DESKTOP│    │
│  │  - UNSAFE CLR assemblies     │   │    │  │  - Service Broker messaging     │    │
│  │  - Hartonomous database      │   │    │  │  - Read replicas, CDC ingestion │    │
│  │  - VECTOR, JSON, Spatial     │   │    │  │  - Temporal table queries       │    │
│  │  - Service Broker queues     │   │    │  └─────────────────────────────────┘    │
│  └──────────────────────────────┘   │    │                                         │
│                                     │    │  ┌─────────────────────────────────┐    │
│  ┌──────────────────────────────┐   │    │  │  .NET 10 Applications           │    │
│  │  Neo4j Desktop               │   │    │  │  - Hartonomous.Api (REST)       │    │
│  │  - Graph projections         │   │    │  │  - Hartonomous.Admin (Blazor)   │    │
│  │  - Development queries       │   │    │  │  - Workers (CesConsumer, Neo4j) │    │
│  └──────────────────────────────┘   │    │  │  - Systemd services             │    │
│                                     │    │  └─────────────────────────────────┘    │
│  ┌──────────────────────────────┐   │    │                                         │
│  │  Development Tools           │   │    │  ┌─────────────────────────────────┐    │
│  │  - VS Code, SSMS, Git        │   │    │  │  Neo4j Community                │    │
│  │  - Build/test environment    │   │    │  │  - Production graph storage     │    │
│  └──────────────────────────────┘   │    │  │  - Atom relationships           │    │
│                                     │    │  └─────────────────────────────────┘    │
└─────────────────────────────────────┘    └─────────────────────────────────────────┘

         (Development + FILESTREAM)              (Application Hosting + Graph)
```

### SQL Server Linked Server Configuration

**Primary Server**: HART-DESKTOP (Windows)
- **Role**: Source of truth for FILESTREAM data, CLR assembly execution
- **FILESTREAM Path**: `D:\SQL_FILESTREAM` (or C:\SQL_FILESTREAM)
- **CLR Assemblies**: All UNSAFE CLR functions deployed here
- **Databases**: Hartonomous (primary), AI-Sandbox, Data Warehouse

**Secondary Server**: HART-SERVER (Linux)
- **Role**: Linked server for distributed queries, Service Broker messaging endpoint
- **Linked Server Name**: `HART_DESKTOP_LINK`
- **Authentication**: Managed identity (Azure Arc system-assigned) or SQL authentication
- **Configuration**:
  ```sql
  -- On HART-SERVER, create linked server to HART-DESKTOP
  EXEC sp_addlinkedserver 
      @server = 'HART_DESKTOP_LINK',
      @srvproduct = 'SQL Server',
      @provider = 'SQLNCLI',
      @datasrc = 'HART-DESKTOP'; -- or IP address if DNS unavailable
  
  -- Configure linked server security (use managed identity if possible)
  EXEC sp_addlinkedsrvlogin 
      @rmtsrvname = 'HART_DESKTOP_LINK',
      @useself = 'FALSE',
      @rmtuser = 'HartServer_LinkedLogin',
      @rmtpassword = '<stored_in_KeyVault>';
  
  -- Test linked server
  SELECT * FROM [HART_DESKTOP_LINK].[Hartonomous].[dbo].[Atoms] WHERE AtomId = 1;
  ```

**Data Distribution Strategy**:

| Data Type | Primary Storage (HART-DESKTOP) | Replicated to HART-SERVER? | Access Pattern |
|-----------|-------------------------------|---------------------------|----------------|
| **Atoms** (with FILESTREAM) | ✅ HART-DESKTOP | ❌ No (FILESTREAM not replicable) | Linked server queries |
| **AtomEmbeddings** (VARBINARY) | ✅ HART-DESKTOP | ✅ Yes (CDC replication) | Local queries on HART-SERVER |
| **TensorAtomCoefficients** | ✅ HART-DESKTOP | ✅ Yes (temporal table sync) | Distributed queries |
| **BillingUsageLedger** | ✅ HART-DESKTOP | ✅ Yes (real-time replication) | Local inserts on HART-SERVER |
| **InferenceCache** | ✅ HART-DESKTOP | ✅ Yes (cache warm-up) | Local queries on HART-SERVER |
| **Service Broker Queues** | ✅ HART-DESKTOP | ✅ HART-SERVER (separate queues) | Cross-server messaging |

**Service Broker Messaging Across Linked Servers**:

```sql
-- On HART-DESKTOP, route messages to HART-SERVER
CREATE ROUTE HartServerRoute
    WITH SERVICE_NAME = 'HartServerIngestionService',
    ADDRESS = 'TCP://hart-server:4022'; -- Service Broker endpoint

-- On HART-SERVER, route messages to HART-DESKTOP
CREATE ROUTE HartDesktopRoute
    WITH SERVICE_NAME = 'HartDesktopProcessingService',
    ADDRESS = 'TCP://HART-DESKTOP:4022'; -- Service Broker endpoint
```

**Limitations to Document**:

1. **FILESTREAM Access**: Applications on HART-SERVER cannot directly access FILESTREAM data (must query via linked server as VARBINARY(MAX))
2. **Distributed Transactions**: MS DTC required for cross-server transactions (enable on both servers)
3. **CLR Functions**: UNSAFE CLR functions only callable on HART-DESKTOP (not via linked server)
4. **Network Latency**: Linked server queries add ~5-20ms latency depending on network
5. **Service Broker Routing**: Requires static IP addresses or reliable DNS (consider HOSTS file entries)

---

## 3. Application Deployment Topology

### HART-SERVER: Application Hosting (Linux)

**Systemd Services** (from `deploy/` directory):

1. **hartonomous-api.service**
   - **Binary**: `Hartonomous.Api.dll`
   - **Port**: 5000 (HTTP), 5001 (HTTPS)
   - **Dependencies**: SQL Server (linked to HART-DESKTOP), Neo4j Community, App Configuration, Key Vault
   - **Environment**: Production
   - **Restart Policy**: always

2. **hartonomous-ces-consumer.service**
   - **Binary**: `Hartonomous.Workers.CesConsumer.dll`
   - **Purpose**: CDC (Change Data Capture) ingestion from SQL Server
   - **Dependencies**: SQL Server (HART-SERVER local + HART-DESKTOP linked)
   - **Restart Policy**: on-failure

3. **hartonomous-neo4j-sync.service**
   - **Binary**: `Hartonomous.Workers.Neo4jSync.dll`
   - **Purpose**: Service Broker message pump, graph synchronization
   - **Dependencies**: SQL Server Service Broker, Neo4j Community
   - **Restart Policy**: on-failure

4. **hartonomous-model-ingestion.service**
   - **Binary**: (TBD - not in current codebase)
   - **Purpose**: Model weight ingestion, ONNX processing
   - **Status**: Placeholder (to be implemented)

**Deployment Script**: `deploy/deploy-to-hart-server.ps1`
- Copies binaries via SSH/SCP
- Deploys systemd unit files
- Reloads systemd daemon
- Restarts services

**Configuration**:
- **App Configuration**: Feature flags, connection strings, tenant configs loaded from Azure
- **Key Vault**: Secrets (SQL passwords, Neo4j credentials, API keys) loaded via managed identity
- **Application Insights**: OpenTelemetry exporter configured for distributed tracing

### HART-DESKTOP: Development + Database (Windows)

**Services**:
- **SQL Server 2025**: Always running, FILESTREAM enabled
- **Neo4j Desktop**: Development graph database
- **Visual Studio/VS Code**: Development tools

**Not Hosted on HART-DESKTOP**:
- ❌ **Hartonomous.Api**: Too resource-intensive for dev workstation
- ❌ **Workers**: Background processing should not interfere with development
- ❌ **Admin Portal**: Hosted on HART-SERVER for centralized access

**Local Development Workflow**:
1. Developer works on HART-DESKTOP (VS Code, SQL Server, Neo4j Desktop)
2. `dotnet run` for local testing (connects to local SQL Server)
3. Git push to GitHub/Azure DevOps
4. Manual deployment to HART-SERVER via `deploy-to-hart-server.ps1`
5. (Future: Azure Pipelines CI/CD automation)

---

## 4. Authentication and Authorization Architecture

### Azure External ID (CIAM) Integration

**Tenant**: hartonomous.onmicrosoft.com (Microsoft.AzureActiveDirectory/ciamDirectories)

**User Types**:
1. **Admin Accounts**: Managed in External ID tenant, full access to admin portal + API
2. **Customer Accounts**: Self-service sign-up, tenant-isolated, role-based access (User, DataScientist, Admin)

**Authentication Flows**:

```
┌─────────────┐                  ┌────────────────────────────┐
│   Customer  │                  │  External ID Tenant        │
│   Browser   │                  │  hartonomous.onmicrosoft.com│
└──────┬──────┘                  └────────────┬───────────────┘
       │                                      │
       │  1. GET /api/infer (no token)       │
       ├────────────────────────────────────► │
       │                                      │
       │  2. 401 Unauthorized                 │
       │     WWW-Authenticate: Bearer         │
       │ ◄────────────────────────────────────┤
       │                                      │
       │  3. Redirect to External ID login    │
       ├────────────────────────────────────► │
       │                                      │
       │  4. User signs in (MFA, social, etc) │
       │ ◄───────────────────────────────────►│
       │                                      │
       │  5. JWT token (with tenant claim)    │
       │ ◄────────────────────────────────────┤
       │                                      │
       │  6. GET /api/infer                   │
       │     Authorization: Bearer <JWT>      │
       ├────────────────────────────────────► │
       │                                      │
       │  7. Token validation + tenant check  │
       │     (Hartonomous.Api validates)      │
       │                                      │
       │  8. 200 OK + inference results       │
       │ ◄────────────────────────────────────┤
       │                                      │
```

**Tenant Isolation**:
- **JWT Claims**: `tenantId`, `role`, `userId`
- **Authorization Policy**: `Hartonomous.Api.Authorization.TenantIsolationHandler` validates tenant claim matches resource
- **Database RLS**: (Future) Row-Level Security filters on `TenantId` column

**Role Hierarchy** (from failing integration tests):
- **Admin**: Full access, bypasses tenant isolation
- **DataScientist**: Can create models, run inference, inherits User permissions
- **User**: Can run inference, view own data, tenant-isolated

**Rate Limiting** (from `Hartonomous.Infrastructure.RateLimiting`):
- **Free Tier**: 100 requests/hour
- **Premium Tier**: 1,000 requests/hour
- **Retry-After** header on 429 Too Many Requests

### Managed Identity Authentication (SQL Server Arc)

**SQL Server 2025 Managed Identity** (Preview):
- **System-assigned managed identity**: Created automatically when connecting to Azure Arc
- **Registry Configuration**: `HKLM\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQLServer\ExtendedSecurity`
- **Token Folder**: `C:\ProgramData\AzureConnectedMachineAgent\Tokens\` (requires SQL Server service account read access)

**Use Cases**:
1. **Inbound**: Customers authenticate to SQL Server via Azure AD (not implemented yet - future feature)
2. **Outbound**: SQL Server authenticates to Azure Key Vault, Azure Storage (for backup to URL)

**Configuration Steps** (from MS Docs):
1. Grant SQL Server service account `Read & execute` permissions on `C:\ProgramData\AzureConnectedMachineAgent\Tokens\`
2. Add SQL Server service account to `Hybrid agent extension applications` group
3. Update registry: `Use Primary Managed Identity = 1`
4. Restart SQL Server service
5. Test: `SELECT * FROM sys.dm_server_external_policy_principals;`

**Limitations**:
- ❌ **Windows only**: Managed identity for SQL Server 2025 only supported on Windows (HART-DESKTOP)
- ❌ **Preview**: Feature is in preview, not production-ready
- ❌ **Failover Clusters**: Not supported with FCIs

**HART-SERVER (Linux)**: Cannot use managed identity for SQL Server authentication (use connection strings from Key Vault instead)

---

## 5. Build and Deployment Status

### Build Validation

**Command**: `dotnet build Hartonomous.sln --no-restore`

**Result**: ✅ **Build succeeded** - 0 Warning(s), 0 Error(s), Time Elapsed 00:00:07.42

**Projects Built Successfully**:
1. Hartonomous.Database.Clr (.NET Framework 4.8.1) → CLR assemblies
2. Hartonomous.Shared.Contracts (.NET 10)
3. Hartonomous.Core (.NET 10)
4. Hartonomous.Database (SQL Server Database Project - DACPAC)
5. Hartonomous.Core.Performance (.NET 10) → ILGPU benchmarks
6. Hartonomous.Data (.NET 10) → EF Core
7. Hartonomous.Infrastructure (.NET 10)
8. Hartonomous.Workers.CesConsumer (.NET 10)
9. Hartonomous.Workers.Neo4jSync (.NET 10)
10. Hartonomous.Api (.NET 10)
11. Hartonomous.Admin (.NET 10) → Blazor Server
12. Hartonomous.UnitTests (.NET 10)
13. Hartonomous.IntegrationTests (.NET 10)
14. Hartonomous.DatabaseTests (.NET 10)
15. Hartonomous.EndToEndTests (.NET 10)

**SDK Notice**: `.NET 10.0 RC2` (preview version) - expected for early adoption of SQL Server 2025 features

### Test Results

**Unit Tests**: ✅ **Passed** - 110/110 tests, 0 failures  
**End-to-End Tests**: ✅ **Passed** - 2/2 tests, 0 failures  
**Integration Tests**: ⚠️ **Failed** - 24 failures, 3 passed, 1 skipped

**Integration Test Failure Analysis**:

**Category 1: Neo4j Connectivity** (3 failures)
- `Neo4j.GraphProjectionIntegrationTests.CreateInferenceNode_WithMetadata_NodeIsCreated`
- `Neo4j.GraphProjectionIntegrationTests.DeleteInference_WithRelationships_CascadesCorrectly`
- **Root Cause**: Tests expect Neo4j connection, but connection string/credentials likely missing in test environment
- **Fix**: Configure Neo4j connection in `appsettings.Testing.json` or use test containers

**Category 2: Inference/SQL Connectivity** (6 failures)
- `InferenceIntegrationTests.ComputeSpatialProjection_ProducesGeometry`
- `InferenceIntegrationTests.StudentModelComparison_ReturnsMetrics`
- `InferenceIntegrationTests.SemanticSearch_ReturnsOriginalEmbedding`
- `InferenceIntegrationTests.HybridSearch_ReturnsSpatialCandidates`
- `InferenceIntegrationTests.MultiResolutionSearch_UsesSpatialCoordinates`
- **Root Cause**: Tests expect SQL Server with Hartonomous database deployed + CLR functions + test data
- **Fix**: Run `deploy-database-unified.ps1` before tests, or mock SQL dependencies

**Category 3: Authentication/Authorization** (15 failures)
- `AuthenticationAuthorizationTests.*` (all auth tests failing)
- **Root Cause**: Tests expect Azure AD JWT tokens, but External ID tenant configuration missing in test environment
- **Fix**: Configure External ID test tenant, generate test tokens, or mock authentication

**Recommended Fixes**:
1. **Immediate**: Add `[Trait("Category", "RequiresInfrastructure")]` to failing tests, skip in CI pipeline
2. **Week 1**: Configure test environment with Neo4j, SQL Server, test data
3. **Week 2**: Set up External ID test tenant, generate test tokens for auth tests
4. **Alternative**: Use `Testcontainers` for Neo4j, `Respawn` for SQL Server test isolation

### Deployment Script Status

**Database Deployment**: `scripts/deploy-database-unified.ps1`
- **Status**: ✅ Complete (811 lines, well-structured)
- **Tested**: Not yet (requires SQL Server 2025 with FILESTREAM enabled)
- **Issues**: See DATABASE_AND_DEPLOYMENT_AUDIT.md (schema duplication, hardcoded paths)

**Application Deployment**: `deploy/deploy-to-hart-server.ps1`
- **Status**: ✅ Complete
- **Tested**: Not yet (requires HART-SERVER SSH access, systemd configuration)

**Local Development**: `deploy/deploy-local-dev.ps1`
- **Status**: ✅ Complete
- **Purpose**: Deploy to local Windows development environment

**Server Bootstrap**: `deploy/setup-hart-server.sh`
- **Status**: ✅ Complete (Bash script for initial server setup)
- **Purpose**: Install .NET 10, SQL Server, Neo4j, configure systemd on Ubuntu

**Azure Pipelines**: `azure-pipelines.yml`
- **Status**: Exists (not analyzed yet)
- **Action**: Review for CI/CD automation, add database deployment step

---

## 6. Cost Analysis and Optimization

### Current Monthly Azure Costs (Estimated)

| Resource | SKU/Tier | Monthly Cost | Optimization Opportunity |
|----------|----------|--------------|-------------------------|
| **App Configuration** | Standard | ~$36 | ⚠️ **Downgrade to Free** (1,000 req/day limit, $0/month) if usage < 1,000 req/day |
| **Key Vault** | Standard | ~$0.03/10k ops | ✅ Minimal cost |
| **Application Insights** | Pay-as-you-go | ~$2-10 | ✅ Depends on telemetry volume |
| **External ID** | MAU-based | $0 (first 50k MAU free) | ✅ Free tier sufficient for now |
| **Azure Arc** | Free | $0 | ✅ Free (monitoring/management only) |
| **SQL Server 2025 Dev** | On-premise | $0 | ✅ Free for development |
| **Neo4j Community** | On-premise | $0 | ✅ Free (open-source) |
| **TOTAL** | | **~$38-46/month** | **Target: <$15/month** |

**App Configuration Cost Breakdown**:
- **Standard SKU**: $1.20/day = $36/month base + $0.01/1,000 requests
- **Free SKU**: $0/month, 1,000 requests/day limit (30,000/month)
- **Current Usage**: (unknown - requires monitoring)
- **Recommendation**: Monitor App Configuration usage for 1 week, downgrade to Free if < 1,000 req/day

**Cost Optimization Actions**:

1. **Week 1**: Enable Application Insights transaction sampling (reduce telemetry volume by 50%)
   ```csharp
   // In Program.cs
   builder.Services.AddApplicationInsightsTelemetry(options =>
   {
       options.EnableAdaptiveSampling = true;
       options.SamplingPercentage = 50; // 50% sampling
   });
   ```

2. **Week 2**: Analyze App Configuration usage
   ```bash
   # Get App Configuration request metrics (last 7 days)
   az monitor metrics list \
       --resource /subscriptions/<sub>/resourceGroups/rg-hartonomous/providers/Microsoft.AppConfiguration/configurationStores/appconfig-hartonomous \
       --metric "HttpIncomingRequestCount" \
       --start-time 2025-11-04 \
       --end-time 2025-11-11 \
       --interval PT1H \
       --query "value[].timeseries[].data[].total"
   ```

3. **Week 3**: If usage < 1,000 req/day, downgrade to Free tier
   ```bash
   az appconfig update \
       --name appconfig-hartonomous \
       --resource-group rg-hartonomous \
       --sku Free
   ```

4. **Month 2**: Implement local App Configuration caching (reduce requests by 80%)
   ```csharp
   // In Hartonomous.Infrastructure
   builder.Services.AddAzureAppConfiguration(options =>
   {
       options.ConfigureRefresh(refresh =>
       {
           refresh.SetCacheExpiration(TimeSpan.FromMinutes(30)); // Reduce refresh frequency
       });
   });
   ```

**Alternative: Migrate to appsettings.json + Key Vault**
- **Savings**: $36/month (eliminate App Configuration entirely)
- **Trade-off**: Lose feature flags, centralized configuration, dynamic updates
- **Recommendation**: Only if App Configuration usage is very low and features not needed

---

## 7. Security Hardening Recommendations

### Network Isolation

**HART-DESKTOP (Windows)**:
- **Windows Firewall**: Allow inbound on SQL Server port 1433 only from HART-SERVER IP
- **SQL Server Configuration**:
  ```sql
  -- Disable remote DAC (Dedicated Admin Connection)
  sp_configure 'remote admin connections', 0;
  GO
  RECONFIGURE;
  GO
  
  -- Restrict sa account
  ALTER LOGIN sa DISABLE;
  GO
  ```

**HART-SERVER (Linux)**:
- **UFW (Uncomplicated Firewall)**:
  ```bash
  # Allow SSH, HTTP, HTTPS, SQL Server
  sudo ufw allow 22/tcp   # SSH
  sudo ufw allow 80/tcp   # HTTP (for Let's Encrypt)
  sudo ufw allow 443/tcp  # HTTPS (API)
  sudo ufw allow from <HART-DESKTOP-IP> to any port 1433 proto tcp  # SQL Server (restricted to HART-DESKTOP)
  sudo ufw enable
  ```

### UNSAFE CLR Security

**Terms of Service Acknowledgment**:
- Document that users accept UNSAFE CLR security implications
- Add to user agreement: "Hartonomous requires UNSAFE CLR assemblies for autonomous operations (file system access, OS command execution). Deployment to Azure SQL Database is not supported."

**TRUSTWORTHY Database Setting**:
- **HART-DESKTOP only**: `ALTER DATABASE Hartonomous SET TRUSTWORTHY ON;`
- **Recommendation**: Use assembly signing instead (more secure than TRUSTWORTHY)
  ```sql
  -- Alternative: Sign assemblies with certificate
  CREATE CERTIFICATE HartonomousCLRCert
      FROM FILE = 'D:\Certs\HartonomousCLR.cer'
      WITH PRIVATE KEY (
          FILE = 'D:\Certs\HartonomousCLR.pvk',
          DECRYPTION BY PASSWORD = '<password_from_KeyVault>'
      );
  GO
  
  ADD SIGNATURE TO ASSEMBLY::[SqlClrFunctions]
      BY CERTIFICATE HartonomousCLRCert
      WITH PASSWORD = '<password>';
  GO
  ```

### Secrets Management

**Current Risk**: Secrets in configuration files, environment variables

**Mitigation**:
1. **Migrate all secrets to Key Vault**:
   - SQL connection strings
   - Neo4j passwords
   - External ID client secrets
   - API keys

2. **Use Managed Identity for Key Vault access** (HART-SERVER via Arc):
   ```csharp
   // In Program.cs
   builder.Configuration.AddAzureKeyVault(
       new Uri("https://kv-hartonomous.vault.azure.net/"),
       new DefaultAzureCredential() // Uses managed identity
   );
   ```

3. **Never commit secrets to Git**:
   - Add `*.secrets.json`, `*.env`, `appsettings.Production.json` to `.gitignore`
   - Use Azure Pipelines secret variables for CI/CD

### Defender for Cloud

**Enable for SQL Server Arc**:
```bash
# Enable Defender for SQL Server on Arc machines
az security pricing create \
    --name SqlServerVirtualMachines \
    --tier Standard
```

**Benefits**:
- Vulnerability assessment (automatic scans)
- Threat detection (anomalous activity alerts)
- Cost: ~$15/server/month (consider for production only)

---

## 8. Deployment Checklist

### Phase 1: Infrastructure Setup (Week 1)

- [ ] **HART-DESKTOP SQL Server Configuration**
  - [ ] Enable FILESTREAM (run `sql/Setup_FILESTREAM.sql`)
  - [ ] Configure FILESTREAM path: `D:\SQL_FILESTREAM`
  - [ ] Enable CLR: `sp_configure 'clr enabled', 1;`
  - [ ] Set TRUSTWORTHY: `ALTER DATABASE Hartonomous SET TRUSTWORTHY ON;`
  - [ ] Deploy database: `./scripts/deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous"`
  - [ ] Verify CLR functions: Test `dbo.clr_VectorDotProduct`

- [ ] **HART-SERVER SQL Server Configuration**
  - [ ] Create linked server to HART-DESKTOP
  - [ ] Configure Service Broker endpoints (port 4022)
  - [ ] Enable CDC: Run `scripts/enable-cdc.sql`
  - [ ] Test linked server: `SELECT * FROM [HART_DESKTOP_LINK].[Hartonomous].[dbo].[Atoms];`

- [ ] **Neo4j Configuration**
  - [ ] HART-DESKTOP: Configure Neo4j Desktop (development)
  - [ ] HART-SERVER: Install Neo4j Community, configure authentication
  - [ ] Test connectivity from Hartonomous.Workers.Neo4jSync

- [ ] **Azure Arc Validation**
  - [ ] Verify both servers show "Connected" status
  - [ ] Enable managed identity on HART-DESKTOP (Windows only)
  - [ ] Configure SQL Server service account permissions for token folder
  - [ ] Test managed identity: `SELECT * FROM sys.dm_server_external_policy_principals;`

### Phase 2: Application Deployment (Week 2)

- [ ] **HART-SERVER Application Deployment**
  - [ ] Run `deploy/setup-hart-server.sh` (install .NET 10, configure systemd)
  - [ ] Deploy applications: `deploy/deploy-to-hart-server.ps1`
  - [ ] Configure App Configuration connection in systemd environment files
  - [ ] Configure Key Vault managed identity authentication
  - [ ] Start services: `sudo systemctl start hartonomous-api hartonomous-ces-consumer hartonomous-neo4j-sync`
  - [ ] Verify logs: `journalctl -u hartonomous-api -f`

- [ ] **External ID Configuration**
  - [ ] Create app registration in hartonomous.onmicrosoft.com
  - [ ] Configure redirect URIs for API
  - [ ] Add app roles (Admin, DataScientist, User)
  - [ ] Create test users for each role
  - [ ] Test authentication flow: `curl -H "Authorization: Bearer <token>" http://hart-server:5000/api/health`

- [ ] **Application Insights Validation**
  - [ ] Verify telemetry is flowing (check Azure Portal)
  - [ ] Create custom dashboard for API performance
  - [ ] Set up alerts for error rate > 5%

### Phase 3: Testing and Validation (Week 3)

- [ ] **Integration Test Fixes**
  - [ ] Configure Neo4j connection in test environment
  - [ ] Deploy test database: `./scripts/deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous_Test"`
  - [ ] Generate test JWT tokens from External ID
  - [ ] Re-run integration tests: `dotnet test Hartonomous.Tests.sln`
  - [ ] Target: 0 failures (28/28 passing)

- [ ] **End-to-End Validation**
  - [ ] User sign-up flow (External ID)
  - [ ] Atom creation with FILESTREAM storage (HART-DESKTOP)
  - [ ] Inference request (API → SQL → CLR functions)
  - [ ] Graph projection (SQL → Service Broker → Neo4j Worker)
  - [ ] Billing ledger update (CDC → CES Consumer)

- [ ] **Performance Testing**
  - [ ] Linked server query latency (HART-SERVER → HART-DESKTOP)
  - [ ] Service Broker message throughput
  - [ ] API response time under load (100 concurrent requests)

### Phase 4: Cost Optimization (Week 4)

- [ ] **App Configuration Analysis**
  - [ ] Monitor request count for 7 days
  - [ ] If < 1,000 req/day, downgrade to Free tier
  - [ ] Implement local caching (30-minute refresh interval)

- [ ] **Application Insights Optimization**
  - [ ] Enable adaptive sampling (50%)
  - [ ] Review telemetry volume in Azure Portal
  - [ ] Disable debug-level logs in production

- [ ] **Review Other Costs**
  - [ ] Check Key Vault transaction volume
  - [ ] Validate External ID MAU count (should be < 50k for free tier)

---

## 9. Known Issues and Risks

### High Priority

1. **Integration Test Failures**
   - **Impact**: Cannot validate authentication, Neo4j, inference pipelines
   - **Mitigation**: Configure test environment, generate test tokens
   - **Timeline**: Week 3

2. **Linked Server Latency**
   - **Impact**: Cross-server queries may be slow (5-20ms overhead)
   - **Mitigation**: Replicate frequently-accessed data to HART-SERVER via CDC
   - **Timeline**: Ongoing monitoring

3. **FILESTREAM Backup Strategy**
   - **Impact**: Standard SQL backups don't include FILESTREAM files
   - **Mitigation**: Use `BACKUP DATABASE` with `FILESTREAM` option, separate file system backup
   - **Timeline**: Week 2

### Medium Priority

4. **Schema Duplication** (from DATABASE_AND_DEPLOYMENT_AUDIT.md)
   - **Impact**: 15 duplicate table definitions (EF Core vs manual SQL)
   - **Mitigation**: Execute Phase 1 cleanup (delete duplicate SQL files)
   - **Timeline**: Week 1-2

5. **Managed Identity Preview**
   - **Impact**: SQL Server 2025 managed identity is in preview (not production-ready)
   - **Mitigation**: Use SQL authentication for now, migrate to managed identity when GA
   - **Timeline**: Monitor for GA announcement (Q1 2026 expected)

6. **App Configuration Cost**
   - **Impact**: $36/month for Standard tier may be excessive
   - **Mitigation**: Monitor usage, downgrade to Free tier if possible
   - **Timeline**: Week 4

### Low Priority

7. **Azure Pipelines Configuration**
   - **Impact**: Manual deployments are error-prone
   - **Mitigation**: Configure CI/CD pipeline for automated deployments
   - **Timeline**: Month 2

8. **Defender for Cloud Cost**
   - **Impact**: $30/month for both servers ($15 each)
   - **Mitigation**: Enable only for production HART-SERVER, disable for HART-DESKTOP
   - **Timeline**: Month 2

---

## 10. Next Steps

### Immediate Actions (This Week)

1. ✅ **Document deployment architecture** (this document)
2. 🔄 **Deploy Hartonomous database to HART-DESKTOP**
   - Run `./scripts/deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous"`
   - Verify FILESTREAM enabled, CLR assemblies deployed
3. 🔄 **Configure linked server on HART-SERVER**
   - Create linked server to HART-DESKTOP
   - Test distributed queries
4. 🔄 **Fix integration tests**
   - Configure Neo4j connection
   - Generate External ID test tokens
   - Re-run tests: `dotnet test Hartonomous.Tests.sln`

### Week 2-4 Actions

- **Week 2**: Deploy applications to HART-SERVER, configure External ID, validate end-to-end flows
- **Week 3**: Performance testing, cost optimization analysis
- **Week 4**: Implement cost optimizations (App Configuration downgrade, sampling), document lessons learned

### Month 2+ Actions

- **Azure Pipelines CI/CD**: Automate build, test, deployment
- **Monitoring Dashboard**: Application Insights custom dashboard for SLOs
- **Backup Automation**: Scheduled SQL backups + FILESTREAM file system backups
- **Disaster Recovery**: Document failover procedures (HART-SERVER failure → restore on HART-DESKTOP)

---

## 11. Decision Record

**Decision**: Hybrid on-premise SQL Server architecture with Azure Arc management

**Context**:
- FILESTREAM required for large BLOB storage (atom payloads)
- UNSAFE CLR required for autonomous operations (file system, OS commands)
- Azure SQL Database/Managed Instance incompatible with UNSAFE CLR
- Budget constraints favor on-premise SQL Server Dev (free) over Azure SQL Managed Instance ($600+/month)

**Alternatives Considered**:
1. **Azure SQL Managed Instance**: ❌ No UNSAFE CLR support, high cost
2. **Azure Storage + Azure SQL Database**: ❌ Requires application rewrite, loss of CLR functions
3. **Single-server deployment**: ❌ No separation of concerns (dev vs app hosting)

**Consequences**:
- ✅ **Pros**: Full SQL Server 2025 feature set, low cost, managed identity via Arc, familiar deployment model
- ⚠️ **Cons**: Linked server latency, manual failover, on-premise infrastructure management, network dependency

**Approval**: Pending user review

**Reversibility**: High - can migrate to Azure SQL Managed Instance if UNSAFE CLR restriction is lifted or CLR functions are rewritten

---

**Document Version**: 1.0  
**Last Updated**: November 11, 2025  
**Next Review**: December 1, 2025 (after Phase 1-4 completion)
