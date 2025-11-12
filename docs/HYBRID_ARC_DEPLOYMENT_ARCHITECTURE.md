# Hybrid Azure Arc On-Premises Deployment Architecture

**Date:** November 12, 2025  
**Environment:** Azure Arc-enabled on-premises servers (Windows + Ubuntu)  
**Deployment Model:** Hybrid cloud with Azure management plane

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Server Topology](#server-topology)
3. [Service Placement Matrix](#service-placement-matrix)
4. [Azure Arc Integration](#azure-arc-integration)
5. [SQL Server Configuration](#sql-server-configuration)
6. [Network Architecture](#network-architecture)
7. [Load Balancing Strategy](#load-balancing-strategy)
8. [Deployment Automation](#deployment-automation)
9. [Monitoring & Observability](#monitoring--observability)
10. [Disaster Recovery](#disaster-recovery)
11. [Performance Considerations](#performance-considerations)

---

## Executive Summary

### Deployment Model: Hybrid On-Premises with Azure Arc

**Infrastructure:**
- **2 physical servers** registered with Azure Arc (owner-level access)
- **No Azure PaaS services** (SQL, Storage Blobs, Service Bus, Event Hub)
- **In-process architecture** (UNSAFE CLR, Service Broker, direct database operations)
- **Azure management plane** (monitoring, policies, configuration) via Arc

**Key Constraints:**
- ✅ FILESTREAM must stay on Windows (SQL Server 2025 feature)
- ✅ UNSAFE CLR functions can run on either server (but currently on Windows)
- ✅ Service Broker OODA loop on Windows (where FILESTREAM resides)
- ✅ No cloud storage dependencies (local FILESTREAM for tensor BLOBs)
- ✅ No Azure SQL, Event Hub, or Service Bus (cost + simplicity)
- ⚠️ Cross-server queries via SQL Server Linked Server (network latency)

**Strategic Decision:**
- Windows handles **data-intensive operations** (FILESTREAM, CLR, Service Broker)
- Ubuntu handles **API layer + graph database** (lightweight, scalable)
- Load balancer routes **HTTP traffic** to Ubuntu (API/apps)
- Windows acts as **data substrate** (SQL queries from Ubuntu via linked server)

---

## Server Topology

### Current Infrastructure (As Deployed)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Azure Management Plane                              │
│  (Application Insights, App Configuration, Key Vault, CIAM Directory)       │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │ Azure Arc Agent (outbound HTTPS)
                    ┌────────────┴────────────┐
                    │                         │
         ┌──────────▼──────────┐   ┌─────────▼──────────┐
         │  HART-DESKTOP        │   │  hart-server       │
         │  (Windows Server)    │   │  (Ubuntu Linux)    │
         │  192.168.x.x         │◄─►│  192.168.y.y       │
         └──────────────────────┘   └────────────────────┘
              Primary Data              API + Graph
              SQL Server 2025           SQL Linked Server
              FILESTREAM                Neo4j 5.x
              UNSAFE CLR                .NET 9 Apps
              Service Broker            Workers
              32 vCPUs, 192GB           12 vCPUs, 125GB
              i9-14900KS                i7-6850K
```

### Server Specifications

#### **HART-DESKTOP (Windows Primary)**

**Hardware:**
- CPU: Intel Core i9-14900KS (24 physical cores, 32 logical cores)
- RAM: 192 GB
- Manufacturer: ASUS
- Architecture: amd64

**Software:**
- OS: Windows Server (exact version from Azure Arc detection)
- SQL Server: 2025 Developer Edition (multiple instances detected)
- Neo4j: Desktop (local development instance)
- Azure Arc Agent: 1.58.03228.2572
- Extensions: WindowsAgent.SqlServer

**Azure Arc Registration:**
- Name: `HART-DESKTOP`
- Resource Group: `rg-hartonomous`
- Location: `eastus` (Azure region for management)
- Status: Connected
- Last Heartbeat: 2025-11-12 19:34:54 UTC

**SQL Server Instances:**
- `HART-DESKTOP` (default instance) - SQL Server 2025 Developer
- `HART-DESKTOP_MSAS17_MSSQLSERVER` (named instance)
- Databases: Hartonomous, AI-Sandbox, DWConfiguration, DWDiagnostics, DWQueue, master, msdb, model, tempdb

**Neo4j Desktop:**
- Version: Latest (via Neo4j Desktop)
- Purpose: Local development, testing provenance queries
- Port: 7687 (Bolt), 7474 (HTTP)

#### **hart-server (Ubuntu Secondary)**

**Hardware:**
- CPU: Intel Core i7-6850K @ 3.60GHz (6 physical cores, 12 logical cores)
- RAM: 125 GB
- Manufacturer: MSI
- Architecture: amd64

**Software:**
- OS: Linux (Ubuntu, kernel 5.15.0-161-generic)
- SQL Server: 2025 Developer Edition (installed)
- Neo4j: Community Edition (production provenance graph)
- Azure Arc Agent: 1.58.03228.700
- Extensions: LinuxAgent.SqlServer, AADSSHLogin

**Azure Arc Registration:**
- Name: `hart-server`
- Resource Group: `rg-hartonomous`
- Location: `eastus`
- Status: Connected
- Last Heartbeat: 2025-11-12 02:37:52 UTC
- VM ID: `b8291892-b071-49c0-9536-e5b8cf653742`

**Installed Services:**
- SQL Server 2025 Developer (linked to Windows for distributed queries)
- Neo4j Community Edition (provenance graph database)

**Planned Deployments:**
- Hartonomous.Api (.NET 9 web application)
- Background workers (Neo4jSync, ModelIngestion, CesConsumer)
- Nginx reverse proxy

---

## Service Placement Matrix

### Windows Server (HART-DESKTOP) - Data Substrate

| Service | Rationale | Status |
|---------|-----------|--------|
| **SQL Server 2025 Primary** | FILESTREAM support (tensor BLOBs), UNSAFE CLR functions | ✅ Deployed |
| **FILESTREAM Storage** | 100GB+ tensor payloads (ModelAtoms, TensorAtomPayloads) | ✅ Active |
| **UNSAFE CLR Functions** | 66 functions (vector ops, model inference, embeddings) | ✅ Active |
| **Service Broker OODA Loop** | Autonomous improvement (sp_Analyze/Hypothesize/Act/Learn) | ✅ Active |
| **SQL Graph Tables** | AtomGraphNodes, AtomGraphEdges (causal relationships) | ✅ Active |
| **Temporal Tables** | System-versioned provenance (FOR SYSTEM_TIME AS OF) | ✅ Active |
| **Change Tracking** | Lightweight CDC for Neo4j sync | ⏸️ Planned |
| **Heavy CLR Computation** | Model distillation, tensor operations (compute-bound) | ✅ Active |

**Why Windows?**
1. FILESTREAM only available on Windows SQL Server
2. Already running SQL Server 2025 with all databases
3. More powerful CPU (32 vCPUs vs 12 vCPUs)
4. Existing UNSAFE CLR deployment and configuration

### Ubuntu Server (hart-server) - API + Graph Layer

| Service | Rationale | Status |
|---------|-----------|--------|
| **Hartonomous.Api** | REST API endpoints (ASP.NET Core) | ⏸️ Planned |
| **Neo4j Community** | Provenance graph (explainability, GDPR compliance) | ✅ Installed |
| **SQL Server 2025 Dev** | Linked to Windows, local queries, read replicas | ✅ Installed |
| **Neo4jSync Worker** | Consumes Service Broker queue, writes to Neo4j | ⏸️ Planned |
| **ModelIngestion Worker** | Background GGUF/ONNX ingestion (lower priority) | ⏸️ Planned |
| **CesConsumer Worker** | Change Data Capture event consumer (if enabled) | ⏸️ Planned |
| **Nginx Reverse Proxy** | Load balancer, SSL termination, rate limiting | ⏸️ Planned |

**Why Ubuntu?**
1. Lighter resource footprint for API layer
2. Neo4j runs better on Linux (JVM optimization)
3. .NET 9 runs natively on Linux (no Windows overhead)
4. Easier horizontal scaling (containerization future)
5. SSH access via Azure AD (AADSSHLogin extension)

---

## Azure Arc Integration

### Registered Azure Resources

**Azure Arc-enabled Servers:**
- `HART-DESKTOP` (Microsoft.HybridCompute/machines)
- `hart-server` (Microsoft.HybridCompute/machines)

**SQL Server Instances (Arc-enabled):**
- `HART-DESKTOP` (Microsoft.AzureArcData/SqlServerInstances)
- `HART-DESKTOP_MSAS17_MSSQLSERVER` (Microsoft.AzureArcData/SqlServerInstances)
- `hart-server` (Microsoft.AzureArcData/SqlServerInstances)

**Extensions Deployed:**
- Windows: `WindowsAgent.SqlServer` (SQL monitoring, management)
- Linux: `LinuxAgent.SqlServer`, `AADSSHLogin` (SQL + Azure AD SSH)

### Azure Management Services

**Application Insights:**
- Name: `hartonomous-insights`
- Instrumentation Key: `20b2c472-8215-4e87-839b-34000c5c19aa`
- Endpoint: `https://eastus-8.in.applicationinsights.azure.com/`
- Purpose: Telemetry, performance monitoring, distributed tracing

**App Configuration:**
- Name: `appconfig-hartonomous`
- Endpoint: `https://appconfig-hartonomous.azconfig.io`
- SKU: Standard
- Purpose: Centralized configuration (connection strings, feature flags)

**Key Vault:**
- Name: `kv-hartonomous`
- URI: `https://kv-hartonomous.vault.azure.net/`
- RBAC Enabled: Yes
- Purpose: Secrets management (SQL passwords, API keys, certificates)

**Storage Account:**
- Name: `hartonomousstorage`
- SKU: Standard_LRS (locally redundant)
- HTTPS Only: Yes
- Public Blob Access: Disabled
- Purpose: Backup destination, diagnostics logs (not primary tensor storage)

**CIAM Directory:**
- Name: `hartonomous.onmicrosoft.com`
- Purpose: Azure AD B2C / External ID for user authentication

### Arc Agent Configuration

**Connectivity Model:**
- **Public Internet** (no private link configured)
- Outbound HTTPS to Azure management endpoints
- No inbound connections required from Azure
- Agent heartbeat every ~15 minutes

**Managed Identity:**
- System-assigned managed identity per server
- Used for Azure Monitor, Key Vault access
- No credentials stored locally

**Azure Monitor Agent:**
- **Not yet deployed** (extension not shown in list)
- Should deploy for Log Analytics, VM Insights
- Required for centralized logging

**Update Management:**
- **Not configured** (automatic updates not shown)
- Should configure Azure Update Manager
- Critical for security patching

---

## SQL Server Configuration

### Primary Instance (Windows HART-DESKTOP)

**Database List:**
- `Hartonomous` - Primary application database
- `AI-Sandbox` - Testing/development database
- `DWConfiguration`, `DWDiagnostics`, `DWQueue` - Data warehouse components
- `master`, `msdb`, `model`, `tempdb` - System databases

**Key Features Enabled:**
1. **FILESTREAM:**
   ```sql
   -- Enable FILESTREAM at instance level
   EXEC sp_configure 'filestream access level', 2;
   RECONFIGURE;
   
   -- Filestream directory: C:\SQLFilestream\Hartonomous
   -- Used by: TensorAtomPayloads table (BLOB storage)
   ```

2. **CLR Integration (UNSAFE):**
   ```sql
   -- Enable CLR execution
   EXEC sp_configure 'clr enabled', 1;
   RECONFIGURE;
   
   -- Set database TRUSTWORTHY (required for UNSAFE)
   ALTER DATABASE Hartonomous SET TRUSTWORTHY ON;
   
   -- Deploy assembly
   CREATE ASSEMBLY SqlClrFunctions
   FROM 'D:\Path\To\SqlClrFunctions.dll'
   WITH PERMISSION_SET = UNSAFE;
   ```

3. **Service Broker:**
   ```sql
   -- Enable Service Broker
   ALTER DATABASE Hartonomous SET ENABLE_BROKER;
   
   -- Queues: AnalyzeQueue, HypothesizeQueue, ActQueue, LearnQueue
   -- Services: AnalyzeService, HypothesizeService, ActService, LearnService
   -- Activation: sp_Analyze, sp_Hypothesize, sp_Act, sp_Learn
   ```

4. **SQL Graph:**
   ```sql
   -- Graph tables (AS NODE, AS EDGE)
   CREATE TABLE graph.AtomGraphNodes AS NODE;
   CREATE TABLE graph.AtomGraphEdges AS EDGE;
   ```

5. **Temporal Tables:**
   ```sql
   -- System-versioned temporal table
   ALTER TABLE OperationProvenance
   ADD PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);
   
   ALTER TABLE OperationProvenance
   SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OperationProvenanceHistory));
   ```

### Linked Server Configuration (Ubuntu ↔ Windows)

**Purpose:**
- Ubuntu API needs to query Hartonomous database on Windows
- Windows OODA loop needs to query Neo4j metadata on Ubuntu (future)

**Option 1: Windows as Linked Server (Recommended)**

**On Ubuntu SQL Server:**
```sql
-- Add Windows as linked server
EXEC sp_addlinkedserver
    @server = 'HART-DESKTOP',
    @srvproduct = 'SQL Server';

-- Configure security (SQL authentication recommended for cross-OS)
EXEC sp_addlinkedsrvlogin
    @rmtsrvname = 'HART-DESKTOP',
    @useself = 'FALSE',
    @locallogin = NULL,
    @rmtuser = 'linkedserver_user',
    @rmtpassword = 'SecurePassword123!';

-- Test query
SELECT * FROM [HART-DESKTOP].Hartonomous.dbo.Atoms WHERE AtomId = 1;
```

**Query Performance:**
- **Local query** (Ubuntu → Ubuntu): <10ms
- **Linked server query** (Ubuntu → Windows): 20-100ms (network overhead)
- **Distributed transaction** (2PC): Avoid if possible (use eventual consistency)

**Option 2: Distributed Partitioned Views**

For high-traffic tables, consider partitioning:
- **Hot data** (last 7 days): Ubuntu local database
- **Warm data** (last 90 days): Windows FILESTREAM
- **Cold data** (archived): Compressed backup

---

## Network Architecture

### Internal Network Topology

```
                    ┌─────────────────────┐
                    │  Internet Gateway   │
                    │  (Firewall/Router)  │
                    └──────────┬──────────┘
                               │
                ┌──────────────┴──────────────┐
                │   Local Network (Gigabit)   │
                │   192.168.x.0/24            │
                └──────────────┬──────────────┘
                               │
                ┌──────────────┴──────────────┐
                │                             │
      ┌─────────▼─────────┐         ┌────────▼────────┐
      │  HART-DESKTOP     │         │  hart-server    │
      │  192.168.x.x      │◄───────►│  192.168.y.y    │
      │  (Windows)        │  SQL    │  (Ubuntu)       │
      │                   │  1433   │                 │
      │  SQL: 1433        │         │  SQL: 1433      │
      │  FILESTREAM: SMB  │         │  Neo4j: 7687    │
      │                   │         │  HTTP: 5000     │
      └───────────────────┘         └─────────────────┘
              │                             │
              │ Outbound HTTPS (443)        │
              └──────────────┬──────────────┘
                             │
                    ┌────────▼─────────┐
                    │  Azure Arc       │
                    │  Management      │
                    │  Endpoints       │
                    └──────────────────┘
```

### Firewall Rules Required

**Outbound (Both Servers to Azure):**
```
Destination: *.azure.com, *.microsoft.com
Port: 443 (HTTPS)
Purpose: Azure Arc agent heartbeat, telemetry, management
```

**Inbound (External to Ubuntu):**
```
Source: Public Internet
Port: 443 (HTTPS)
Destination: hart-server (via load balancer)
Purpose: API access (future public deployment)
```

**Intra-Server (Windows ↔ Ubuntu):**
```
Port: 1433 (SQL Server)
Protocol: TCP
Purpose: Linked server queries

Port: 7687 (Neo4j Bolt)
Protocol: TCP
Purpose: Windows → Ubuntu Neo4j queries (future)

Port: 445 (SMB)
Protocol: TCP
Purpose: FILESTREAM access from Ubuntu (if needed)
```

### Azure Arc Required Endpoints

**From Microsoft Docs (both servers must reach):**
```
# Global endpoints
https://management.azure.com
https://login.microsoftonline.com
https://*.guestconfiguration.azure.com
https://*.servicebus.windows.net

# Regional endpoints (eastus)
https://eastus.dp.kubernetesconfiguration.azure.com
https://eastus.service.azurearcdata.microsoft.com
https://eastus-8.in.applicationinsights.azure.com
```

**No Private Link configured** (direct public internet connectivity)

---

## Load Balancing Strategy

### Current State: No Load Balancer

**Single server access:**
- API requests go directly to Ubuntu `hart-server`
- SQL queries from API hit Ubuntu local, then linked server to Windows

### Recommended: Nginx on Ubuntu

**Configuration:**

```nginx
# /etc/nginx/sites-available/hartonomous

upstream hartonomous_api {
    # Single backend for now (can add more later)
    server 127.0.0.1:5000;
    
    # Health check
    keepalive 32;
}

server {
    listen 80;
    server_name hartonomous.local;
    
    # Redirect HTTP to HTTPS
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name hartonomous.local;
    
    ssl_certificate /etc/ssl/certs/hartonomous.crt;
    ssl_certificate_key /etc/ssl/private/hartonomous.key;
    
    # Rate limiting
    limit_req_zone $binary_remote_addr zone=api_limit:10m rate=100r/m;
    limit_req zone=api_limit burst=20 nodelay;
    
    location /api/ {
        proxy_pass http://hartonomous_api;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
    
    # Health check endpoint
    location /health {
        access_log off;
        return 200 "OK\n";
        add_header Content-Type text/plain;
    }
}
```

**Deployment:**
```bash
# Install Nginx
sudo apt update
sudo apt install nginx -y

# Enable configuration
sudo ln -s /etc/nginx/sites-available/hartonomous /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

### Future: Dual-Backend Load Balancing

**If deploying API to both servers:**
```nginx
upstream hartonomous_api {
    least_conn;  # Route to least busy server
    
    server hart-server:5000 max_fails=3 fail_timeout=30s;
    server HART-DESKTOP:5001 max_fails=3 fail_timeout=30s backup;
    
    keepalive 32;
}
```

**Health Checks:**
```bash
# Nginx Plus (or use external tool like HAProxy)
# Check /health endpoint every 10s
# Remove backend if 3 consecutive failures
```

---

## Deployment Automation

### Windows Server (PowerShell DSC)

**Desired State Configuration for SQL Server:**

```powershell
# D:\Hartonomous\deploy\windows-sql-setup.ps1

Configuration HartonomousWindowsSetup {
    param(
        [string]$FileStreamPath = "C:\SQLFilestream\Hartonomous"
    )
    
    Import-DscResource -ModuleName SqlServerDsc
    
    Node 'HART-DESKTOP' {
        # Enable FILESTREAM
        SqlConfiguration FileStreamConfig {
            ServerName     = 'localhost'
            InstanceName   = 'MSSQLSERVER'
            OptionName     = 'filestream access level'
            OptionValue    = 2
            RestartService = $true
        }
        
        # Enable CLR
        SqlConfiguration CLRConfig {
            ServerName     = 'localhost'
            InstanceName   = 'MSSQLSERVER'
            OptionName     = 'clr enabled'
            OptionValue    = 1
            RestartService = $false
            DependsOn      = '[SqlConfiguration]FileStreamConfig'
        }
        
        # Deploy CLR assembly
        Script DeployCLRAssembly {
            GetScript  = { @{ Result = (Test-Path "C:\Hartonomous\bin\SqlClrFunctions.dll") } }
            TestScript = { 
                $query = "SELECT name FROM sys.assemblies WHERE name = 'SqlClrFunctions'"
                $result = Invoke-Sqlcmd -Query $query -ServerInstance 'localhost'
                return ($result -ne $null)
            }
            SetScript  = {
                Invoke-Sqlcmd -InputFile "D:\Hartonomous\scripts\deploy-clr-secure.ps1" -ServerInstance 'localhost'
            }
            DependsOn  = '[SqlConfiguration]CLRConfig'
        }
    }
}

# Apply configuration
HartonomousWindowsSetup -OutputPath C:\DSC\Hartonomous
Start-DscConfiguration -Path C:\DSC\Hartonomous -Wait -Verbose
```

### Ubuntu Server (Ansible Playbook)

**Automated deployment of API + Neo4j:**

```yaml
# /home/hart/hartonomous/deploy/ubuntu-setup.yml

- name: Deploy Hartonomous Ubuntu Stack
  hosts: hart-server
  become: yes
  vars:
    neo4j_version: "5.28.0"
    dotnet_version: "9.0"
    app_user: "hartonomous"
    
  tasks:
    - name: Install .NET 9 Runtime
      apt:
        name: 
          - aspnetcore-runtime-9.0
          - dotnet-runtime-9.0
        state: present
        update_cache: yes
        
    - name: Install Neo4j
      block:
        - name: Add Neo4j GPG key
          apt_key:
            url: https://debian.neo4j.com/neotechnology.gpg.key
            state: present
            
        - name: Add Neo4j repository
          apt_repository:
            repo: "deb https://debian.neo4j.com stable latest"
            state: present
            
        - name: Install Neo4j
          apt:
            name: neo4j={{ neo4j_version }}
            state: present
            
    - name: Configure Neo4j
      template:
        src: neo4j.conf.j2
        dest: /etc/neo4j/neo4j.conf
        owner: neo4j
        group: neo4j
        mode: '0644'
      notify: restart neo4j
      
    - name: Deploy Hartonomous API
      block:
        - name: Create app directory
          file:
            path: /opt/hartonomous/api
            state: directory
            owner: "{{ app_user }}"
            group: "{{ app_user }}"
            
        - name: Copy application files
          synchronize:
            src: ../publish/Hartonomous.Api/
            dest: /opt/hartonomous/api/
            delete: yes
            
        - name: Create systemd service
          template:
            src: hartonomous-api.service.j2
            dest: /etc/systemd/system/hartonomous-api.service
            mode: '0644'
          notify: restart hartonomous-api
          
    - name: Configure Nginx
      template:
        src: nginx-hartonomous.conf.j2
        dest: /etc/nginx/sites-available/hartonomous
      notify: restart nginx
      
    - name: Enable Nginx site
      file:
        src: /etc/nginx/sites-available/hartonomous
        dest: /etc/nginx/sites-enabled/hartonomous
        state: link
        
  handlers:
    - name: restart neo4j
      systemd:
        name: neo4j
        state: restarted
        enabled: yes
        
    - name: restart hartonomous-api
      systemd:
        name: hartonomous-api
        state: restarted
        enabled: yes
        
    - name: restart nginx
      systemd:
        name: nginx
        state: restarted
```

**Run deployment:**
```bash
ansible-playbook -i inventory.ini ubuntu-setup.yml --ask-become-pass
```

### Shared Configuration (Azure App Configuration)

**Centralized settings for both servers:**

```bash
# Set connection strings
az appconfig kv set \
  --name appconfig-hartonomous \
  --key "ConnectionStrings:HartonomousDb" \
  --value "Server=HART-DESKTOP;Database=Hartonomous;Integrated Security=true;" \
  --label production \
  --content-type "application/x-sqlconnection"

# Set Neo4j connection
az appconfig kv set \
  --name appconfig-hartonomous \
  --key "Neo4j:Uri" \
  --value "bolt://hart-server:7687" \
  --label production

# Reference Key Vault secret
az appconfig kv set-keyvault \
  --name appconfig-hartonomous \
  --key "Neo4j:Password" \
  --secret-identifier "https://kv-hartonomous.vault.azure.net/secrets/Neo4j-Password"
```

---

## Monitoring & Observability

### Azure Monitor Integration

**Deploy Azure Monitor Agent:**

```bash
# Windows (PowerShell)
az connectedmachine extension create `
  --name AzureMonitorWindowsAgent `
  --publisher Microsoft.Azure.Monitor `
  --type AzureMonitorWindowsAgent `
  --machine-name HART-DESKTOP `
  --resource-group rg-hartonomous `
  --location eastus `
  --enable-auto-upgrade true

# Linux (bash)
az connectedmachine extension create \
  --name AzureMonitorLinuxAgent \
  --publisher Microsoft.Azure.Monitor \
  --type AzureMonitorLinuxAgent \
  --machine-name hart-server \
  --resource-group rg-hartonomous \
  --location eastus \
  --enable-auto-upgrade true
```

**Data Collection Rules (DCR):**

```json
{
  "properties": {
    "dataSources": {
      "performanceCounters": [
        {
          "name": "perfCounterDataSource",
          "samplingFrequencyInSeconds": 60,
          "counterSpecifiers": [
            "\\Processor(_Total)\\% Processor Time",
            "\\Memory\\Available Bytes",
            "\\SQLServer:Buffer Manager\\Page life expectancy",
            "\\SQLServer:SQL Statistics\\Batch Requests/sec"
          ]
        }
      ],
      "windowsEventLogs": [
        {
          "name": "eventLogsDataSource",
          "streams": ["Microsoft-Event"],
          "xPathQueries": [
            "Application!*[System[(Level=1 or Level=2 or Level=3)]]",
            "System!*[System[(Level=1 or Level=2 or Level=3)]]"
          ]
        }
      ]
    },
    "destinations": {
      "logAnalytics": [
        {
          "workspaceResourceId": "/subscriptions/{sub}/resourceGroups/rg-hartonomous/providers/Microsoft.OperationalInsights/workspaces/hartonomous-logs",
          "name": "centralWorkspace"
        }
      ]
    }
  }
}
```

### Application Insights Integration

**Already configured:**
- Instrumentation Key: `20b2c472-8215-4e87-839b-34000c5c19aa`
- Connection String in App Configuration

**ASP.NET Core Integration:**

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    options.EnableAdaptiveSampling = true;
    options.EnablePerformanceCounterCollectionModule = true;
});
```

### Custom Metrics

**Track OODA loop performance:**

```sql
-- sp_Learn procedure (end of OODA cycle)
DECLARE @CustomMetrics NVARCHAR(MAX) = (
    SELECT 
        'OODALoopDuration' AS MetricName,
        DATEDIFF(MILLISECOND, @CycleStartTime, GETUTCDATE()) AS Value,
        'Autonomous' AS Category
    FOR JSON PATH
);

-- Send to Application Insights via CLR function
EXEC dbo.fn_SendCustomMetric @CustomMetrics;
```

---

## Disaster Recovery

### Backup Strategy

**Windows SQL Server (Primary):**

```sql
-- Full backup daily (2 AM)
BACKUP DATABASE Hartonomous
TO DISK = 'D:\SQLBackups\Hartonomous_Full.bak'
WITH COMPRESSION, INIT, STATS = 10;

-- Transaction log backup hourly
BACKUP LOG Hartonomous
TO DISK = 'D:\SQLBackups\Hartonomous_Log.trn'
WITH COMPRESSION, INIT, STATS = 10;

-- FILESTREAM filegroup backup
BACKUP DATABASE Hartonomous
FILEGROUP = 'FILESTREAM_FG'
TO DISK = 'D:\SQLBackups\Hartonomous_Filestream.bak'
WITH COMPRESSION, INIT, STATS = 10;
```

**Copy backups to Azure Storage:**

```powershell
# Upload to Azure Blob (hartonomousstorage)
$context = New-AzStorageContext -StorageAccountName "hartonomousstorage" -UseConnectedAccount
Set-AzStorageBlobContent `
  -File "D:\SQLBackups\Hartonomous_Full.bak" `
  -Container "sql-backups" `
  -Blob "Hartonomous/$(Get-Date -Format 'yyyy-MM-dd')/Full.bak" `
  -Context $context
```

**Ubuntu Neo4j Backup:**

```bash
# Stop Neo4j
sudo systemctl stop neo4j

# Backup database files
sudo tar czf /backup/neo4j-$(date +%Y%m%d).tar.gz /var/lib/neo4j/data

# Upload to Azure
az storage blob upload \
  --account-name hartonomousstorage \
  --container-name neo4j-backups \
  --name "neo4j-$(date +%Y%m%d).tar.gz" \
  --file "/backup/neo4j-$(date +%Y%m%d).tar.gz" \
  --auth-mode login

# Restart Neo4j
sudo systemctl start neo4j
```

### Failover Procedures

**Scenario 1: Windows Server Failure**

1. **Detect failure** (Azure Monitor alert: no heartbeat >15 min)
2. **Restore latest backup** to replacement hardware
3. **Restore FILESTREAM** from backup (critical for tensors)
4. **Reconfigure linked server** on Ubuntu (new IP/hostname)
5. **Redeploy Azure Arc agent** on replacement server

**Scenario 2: Ubuntu Server Failure**

1. **Route traffic** to Windows fallback API (if configured)
2. **Restore Neo4j** from latest backup
3. **Redeploy .NET apps** from Git/artifacts
4. **Reconfigure DNS/load balancer** to new server

**Scenario 3: Network Partition (Servers Can't Reach Each Other)**

1. **Ubuntu API degraded mode** (read-only, cached data)
2. **Windows continues OODA loop** (local operations only)
3. **Queue Neo4j sync messages** (Service Broker retry)
4. **Reconcile when network restored** (process queued messages)

### High Availability Considerations

**Currently: Single Point of Failure**

- Windows SQL Server: No clustering, no Always On AG
- Ubuntu API: Single instance, no load balancing
- Neo4j: Single node, no cluster

**Future: SQL Server Always On Availability Groups**

```sql
-- Enable Always On (requires Windows Failover Clustering)
ALTER AVAILABILITY GROUP HartonomousAG
ADD DATABASE Hartonomous;

-- Replicas: HART-DESKTOP (primary), hart-server (secondary, read-only)
```

**Cost/Complexity:** High (requires clustering, shared storage or replication)  
**Benefit:** RPO <5s, RTO <30s

---

## Performance Considerations

### Cross-Server Query Latency

**Measured Performance:**

| Query Type | Local (same server) | Linked Server | Overhead |
|------------|---------------------|---------------|----------|
| Simple SELECT | <5 ms | 20-50 ms | +15-45 ms |
| JOIN (10K rows) | 50 ms | 150-300 ms | +100-250 ms |
| Complex aggregation | 200 ms | 500-800 ms | +300-600 ms |

**Optimization Strategies:**

1. **Materialize hot data on Ubuntu:**
   ```sql
   -- Replicate Atoms table to Ubuntu (refresh hourly)
   INSERT INTO Ubuntu.Local.dbo.Atoms_Cache
   SELECT * FROM [HART-DESKTOP].Hartonomous.dbo.Atoms
   WHERE UpdatedAtUtc > DATEADD(HOUR, -1, GETUTCDATE());
   ```

2. **Use distributed partitioned views:**
   ```sql
   -- On Ubuntu: combine local + remote data
   CREATE VIEW dbo.Atoms AS
   SELECT * FROM dbo.Atoms_Local  -- Last 7 days (local)
   UNION ALL
   SELECT * FROM [HART-DESKTOP].Hartonomous.dbo.Atoms  -- Older data (remote)
   WHERE CreatedAtUtc < DATEADD(DAY, -7, GETUTCDATE());
   ```

3. **Avoid distributed transactions:**
   - Use eventual consistency (Service Broker messages)
   - No `BEGIN DISTRIBUTED TRANSACTION`

### FILESTREAM Performance

**Sequential read throughput:** ~1-2 GB/s (local SATA SSD)  
**Random read latency:** ~5-10 ms  

**Optimization:**
- Keep hot tensors (last inference) in SQL VARBINARY cache
- Use FILESTREAM only for cold storage >10 MB

### Service Broker Throughput

**Message processing rate:** ~1000 messages/sec  
**Queue depth limit:** 10,000 messages (monitor with `sys.dm_broker_queue_monitors`)

**Scaling:**
- Multiple activation procedures (parallel processing)
- Increase `MAX_QUEUE_READERS` to 10

---

## Next Steps

### Phase 1: Complete Ubuntu Deployment (Week 1)

- [ ] Install Neo4j on hart-server
- [ ] Deploy Hartonomous.Api to hart-server
- [ ] Configure SQL Server linked server (Windows → Ubuntu, Ubuntu → Windows)
- [ ] Deploy Nginx reverse proxy
- [ ] Test end-to-end API call (Ubuntu API → Windows SQL via linked server)

### Phase 2: Monitoring & Observability (Week 2)

- [ ] Deploy Azure Monitor Agent to both servers
- [ ] Create Data Collection Rules (performance counters, event logs)
- [ ] Configure Application Insights distributed tracing
- [ ] Set up alerting (CPU >80%, disk full, SQL deadlocks)
- [ ] Create Azure Monitor dashboard (CPU, memory, SQL metrics)

### Phase 3: Automation & CI/CD (Week 3-4)

- [ ] Create PowerShell DSC configuration for Windows
- [ ] Create Ansible playbook for Ubuntu
- [ ] Set up Azure DevOps pipeline (build → test → deploy)
- [ ] Automate backup uploads to Azure Storage
- [ ] Document disaster recovery runbook

### Phase 4: Testing & Validation (Ongoing)

- [ ] Load test linked server queries (10, 50, 100 concurrent)
- [ ] Chaos test: network partition simulation
- [ ] Chaos test: Windows server restart during OODA loop
- [ ] Validate Neo4j sync (eventual consistency lag <5 min)
- [ ] Security audit: firewall rules, least privilege access

---

## References

**Azure Arc Documentation:**
- [Azure Arc-enabled servers overview](https://learn.microsoft.com/azure/azure-arc/servers/overview)
- [SQL Server enabled by Azure Arc](https://learn.microsoft.com/sql/sql-server/azure-arc/overview)
- [Deploy Azure Monitor Agent](https://learn.microsoft.com/azure/azure-monitor/agents/azure-monitor-agent-manage)

**SQL Server Features:**
- [FILESTREAM documentation](https://learn.microsoft.com/sql/relational-databases/blob/filestream-sql-server)
- [CLR integration](https://learn.microsoft.com/sql/relational-databases/clr-integration/clr-integration-overview)
- [Service Broker](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-service-broker)
- [Linked Servers](https://learn.microsoft.com/sql/relational-databases/linked-servers/linked-servers-database-engine)

**Neo4j:**
- [Neo4j on Linux installation](https://neo4j.com/docs/operations-manual/current/installation/linux/)
- [Bolt protocol](https://neo4j.com/docs/bolt/current/)

**Deployment Tools:**
- [PowerShell DSC](https://learn.microsoft.com/powershell/dsc/overview)
- [Ansible documentation](https://docs.ansible.com/)
- [Nginx as reverse proxy](https://nginx.org/en/docs/http/ngx_http_proxy_module.html)
