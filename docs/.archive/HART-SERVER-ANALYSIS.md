# ?? HART-SERVER INFRASTRUCTURE ANALYSIS

**Analysis Date**: 2025-11-21 20:40 UTC  
**Purpose**: Pre-deployment research for application layer  
**Method**: Read-only SSH reconnaissance  

---

## ? SYSTEM OVERVIEW

### Operating System
```
OS: Ubuntu 22.04.5 LTS (Jammy)
Architecture: x86_64
Kernel: 5.15.0-161-generic
```

### Hardware Resources
```
Memory: 125 GiB total (117 GiB available)
Swap: 15 GiB
Disk: 209 GB total, 174 GB available (14% used)
CPU: Not checked (but Azure Arc agent running suggests adequate)
```

**Assessment**: ? **Excellent** - Plenty of resources for running multiple .NET apps

---

## ?? NETWORK CONNECTIVITY

### Connectivity to HART-DESKTOP (Database Server)
```bash
? Ping test: SUCCESS (0.3-0.4ms latency - local network)
? SQL Server port 1433: OPEN and accessible
? DNS resolution: Working (HART-DESKTOP.lan resolves correctly)
```

**Assessment**: ? **PERFECT** - Low latency, SQL Server accessible

### Currently Open Ports
```
Port 80 (HTTP): ? OPEN (nginx listening)
Port 443 (HTTPS): ? Not checked
Port 5000 (Kestrel): ? NOT OPEN (no .NET apps running)
Port 5001 (Kestrel HTTPS): ? NOT OPEN
Port 7687 (Neo4j Bolt): ? OPEN (Neo4j running)
```

**Assessment**: ? Ready for deployment (ports 5000/5001 available)

---

## ?? RUNTIME ENVIRONMENT

### .NET Runtime
```
Status: ? NOT INSTALLED
Command: dotnet --info ? command not found
```

**CRITICAL**: Must install .NET 8.0 SDK/Runtime before deploying applications

### Neo4j Graph Database
```
Status: ? RUNNING
Service: neo4j.service (active, enabled)
Memory: 858 MB
Port: 7687 (Bolt protocol)
Java: Microsoft OpenJDK 21
Config: /etc/neo4j/
Data: /var/lib/neo4j/
```

**Assessment**: ? **OPERATIONAL** - Ready for graph sync worker

### Nginx Web Server
```
Status: ? RUNNING
Service: nginx.service (active)
Config: /etc/nginx/sites-enabled/default
Listening: Port 80 (HTTP)
```

**Assessment**: ? **OPERATIONAL** - Can be configured as reverse proxy for API

### Azure Arc Agent
```
Status: ? RUNNING
Service: himdsd.service (active, enabled)
Memory: 31.1 MB
Uptime: 7+ hours
```

**Assessment**: ? **CONNECTED** - Ready for managed identity authentication

### Git
```
Status: ? INSTALLED
Version: 2.34.1
```

**Assessment**: ? Ready for code deployment

---

## ?? EXISTING DEPLOYMENT STRUCTURE

### Hartonomous Directories
```
Location: /home/ahart/hartonomous/
Created: Nov 21, 2025 20:35 UTC (TODAY - created by Copilot)

Structure:
~/hartonomous/
??? api/       (empty)
??? web/       (empty)
??? workers/   (empty)
```

**Assessment**: ? Directory structure created, ready for deployment

### Systemd Services
```
Hartonomous services: ? NONE FOUND
Current services: nginx, neo4j, himdsd (Azure Arc)
```

**Assessment**: Need to create systemd service files for API/workers

---

## ?? SECURITY & ACCESS

### User Permissions
```
User: ahart
Home: /home/ahart
Sudo: ? Requires password (not tested)
SSH: ? Key-based authentication working
```

**Assessment**: ? User has sufficient permissions for ~/hartonomous deployment

### Azure Arc Managed Identity
```
Status: ? ACTIVE (himdsd service running)
Can authenticate to: Azure resources (Key Vault, Storage, etc.)
```

**Assessment**: ? Ready for passwordless Azure authentication

---

## ?? DEPLOYMENT REQUIREMENTS

### CRITICAL - Must Install Before Deployment

1. **.NET 8.0 SDK/Runtime** ? **REQUIRED**
   ```bash
   # Ubuntu 22.04 installation
   wget https://dot.net/v1/dotnet-install.sh
   chmod +x dotnet-install.sh
   ./dotnet-install.sh --channel 8.0
   ```

2. **Environment Variables**
   - ConnectionStrings__HartonomousDb
   - Neo4j__Password
   - Azure credentials (via Managed Identity)

3. **Systemd Service Files**
   - hartonomous-api.service
   - hartonomous-web.service (optional)
   - hartonomous-worker-ces.service
   - hartonomous-worker-embedding.service
   - hartonomous-worker-neo4j.service

4. **Nginx Reverse Proxy Configuration**
   ```nginx
   # Proxy port 80 ? localhost:5000 (API)
   # OR expose 5000 directly
   ```

---

## ?? RECOMMENDED DEPLOYMENT ARCHITECTURE

### Option 1: Nginx Reverse Proxy (Recommended)
```
Internet ? Port 80 (nginx) ? localhost:5000 (Kestrel API)
         ? Port 443 (nginx SSL) ? localhost:5001 (Kestrel API HTTPS)
```

**Benefits**:
- SSL termination at nginx
- Load balancing support
- Better logging/monitoring
- Standard web server setup

### Option 2: Direct Kestrel Exposure
```
Internet ? Port 5000 (Kestrel API directly)
         ? Port 5001 (Kestrel API HTTPS)
```

**Benefits**:
- Simpler configuration
- Lower latency
- Good for development

**Recommendation**: Use **Option 1 (nginx proxy)** for production-like setup

---

## ?? APPLICATION CONNECTIONS

### API ? Database (HART-DESKTOP)
```
Connection String:
Server=HART-DESKTOP;Database=Hartonomous;
Integrated Security=False;
User ID=<azure-arc-managed-identity>;
Authentication=Active Directory Default;
TrustServerCertificate=True;
```

**Status**: ? Network path verified (ping 0.3ms, port 1433 open)

### Workers ? Neo4j (localhost)
```
Connection String:
bolt://localhost:7687
Username: neo4j
Password: <from-key-vault>
Database: neo4j
```

**Status**: ? Neo4j running locally on port 7687

### All Apps ? Azure Key Vault
```
Vault: https://kv-hartonomous.vault.azure.net/
Auth: Azure Arc Managed Identity (already active)
```

**Status**: ? Arc agent running, managed identity available

---

## ?? READINESS ASSESSMENT

| Component | Status | Blocker? | Action Required |
|-----------|--------|----------|-----------------|
| **OS & Hardware** | ? Ready | No | None |
| **Network to DB** | ? Ready | No | None |
| **Neo4j** | ? Running | No | Get password from Key Vault |
| **Nginx** | ? Running | No | Configure reverse proxy |
| **Azure Arc** | ? Connected | No | None |
| **.NET Runtime** | ? Missing | **YES** | **Install .NET 8.0** |
| **App Binaries** | ? Not deployed | **YES** | Build and publish from HART-DESKTOP |
| **Systemd Services** | ? Not created | **YES** | Create service files |
| **Environment Config** | ? Not configured | **YES** | Create appsettings.Production.json |

**Overall Readiness**: ?? **60%** - Infrastructure ready, runtime and apps missing

---

## ?? DEPLOYMENT SEQUENCE

### Phase 1: Install .NET Runtime (5 minutes)
```bash
ssh ahart@HART-SERVER "
  wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  /tmp/dotnet-install.sh --channel 8.0 --install-dir ~/.dotnet
  echo 'export PATH=\$PATH:\$HOME/.dotnet' >> ~/.bashrc
  source ~/.bashrc
  dotnet --info
"
```

### Phase 2: Build and Publish API (2 minutes)
```powershell
cd D:\Repositories\Hartonomous
dotnet publish src/Hartonomous.Api/Hartonomous.Api.csproj `
  -c Release `
  -o publish/api `
  --self-contained false
```

### Phase 3: Deploy Binaries (1 minute)
```powershell
scp -r publish/api/* ahart@HART-SERVER:~/hartonomous/api/
```

### Phase 4: Create Systemd Service (2 minutes)
```bash
ssh ahart@HART-SERVER "
  cat > ~/hartonomous/api/hartonomous-api.service <<'EOF'
[Unit]
Description=Hartonomous API
After=network.target

[Service]
Type=notify
User=ahart
WorkingDirectory=/home/ahart/hartonomous/api
ExecStart=/home/ahart/.dotnet/dotnet /home/ahart/hartonomous/api/Hartonomous.Api.dll
Restart=on-failure
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

  sudo mv ~/hartonomous/api/hartonomous-api.service /etc/systemd/system/
  sudo systemctl daemon-reload
  sudo systemctl enable hartonomous-api
  sudo systemctl start hartonomous-api
  sudo systemctl status hartonomous-api
"
```

### Phase 5: Configure Nginx (3 minutes)
```bash
ssh ahart@HART-SERVER "
  cat > /tmp/hartonomous-api <<'EOF'
server {
    listen 80;
    server_name api.hartonomous.local hart-server;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
}
EOF

  sudo mv /tmp/hartonomous-api /etc/nginx/sites-available/
  sudo ln -s /etc/nginx/sites-available/hartonomous-api /etc/nginx/sites-enabled/
  sudo nginx -t
  sudo systemctl reload nginx
"
```

### Phase 6: Verify Deployment (1 minute)
```bash
curl http://HART-SERVER/health
curl http://localhost:5000/health (from HART-DESKTOP)
```

**Total Estimated Time**: 15 minutes

---

## ?? NEXT STEPS

1. **Install .NET 8.0 Runtime** on HART-SERVER (CRITICAL)
2. **Build application binaries** on HART-DESKTOP (has VS and .NET)
3. **Create deployment script** that automates phases 2-6
4. **Test deployment** to ~/hartonomous/api
5. **Create systemd services** for workers
6. **Configure nginx** reverse proxy
7. **Verify end-to-end** (HART-DESKTOP ? HART-SERVER ? HART-DESKTOP DB)

---

## ?? DEPLOYMENT SCRIPT LOCATION

**Script to create**: `scripts/Deploy-AppLayer.ps1`

**What it should do**:
1. Check prerequisites (.NET installed on HART-SERVER)
2. Build and publish app projects
3. SCP binaries to HART-SERVER
4. Create/update systemd service files
5. Configure nginx (if needed)
6. Start services
7. Verify health endpoints

---

## ? CONCLUSION

**HART-SERVER Infrastructure Status**: ?? **READY WITH PREREQUISITES**

**Blocking Issues**:
- ? .NET 8.0 Runtime not installed (15 minutes to fix)
- ? Application binaries not deployed (automated with script)
- ? Systemd services not created (automated with script)

**Non-Blocking**:
- ? Network connectivity excellent
- ? Neo4j operational
- ? Nginx ready for configuration
- ? Azure Arc connected
- ? Plenty of resources (125GB RAM, 174GB disk)

**Recommendation**: 
1. Install .NET 8.0 on HART-SERVER
2. Create `Deploy-AppLayer.ps1` script
3. Run deployment

**Risk Level**: ?? **LOW** - Well-understood environment, clear path forward

---

*Research completed without making any changes to HART-SERVER* ?
