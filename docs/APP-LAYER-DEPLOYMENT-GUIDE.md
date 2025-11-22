# ?? APP LAYER DEPLOYMENT - READY TO EXECUTE

**Status**: Research complete, deployment script ready  
**Target**: HART-SERVER (Linux Ubuntu 22.04)  
**Database**: HART-DESKTOP (already deployed ?)  

---

## ?? Current Status

### ? What's Ready
- Database deployed on HART-DESKTOP (localhost) ?
- EF entities scaffolded ?
- Application projects build successfully (0 errors, 0 warnings) ?
- SSH access to HART-SERVER configured ?
- Network connectivity verified (0.3ms latency) ?
- Neo4j running on HART-SERVER ?
- Nginx running on HART-SERVER ?
- Azure Arc agent active ?

### ? What's Missing
- .NET 8.0 Runtime on HART-SERVER ? (script will install)
- Application binaries not deployed ? (script handles this)
- Systemd services not created ? (script handles this)

---

## ?? Deploy Application Layer

### Option 1: Deploy API Only (Recommended First)

```powershell
cd D:\Repositories\Hartonomous
pwsh -File scripts/Deploy-AppLayer.ps1 -Component API
```

**What this does**:
1. ? Checks SSH connectivity
2. ? Checks .NET 8.0 on HART-SERVER (offers to install if missing)
3. ? Verifies database connectivity from HART-SERVER
4. ? Builds Hartonomous.Api (Release configuration)
5. ? Copies binaries to HART-SERVER:~/hartonomous/api/
6. ? Creates systemd service (hartonomous-api.service)
7. ? Starts the service
8. ? Verifies health endpoint (http://localhost:5000/health)

**Duration**: ~5 minutes (includes .NET installation if needed)

---

### Option 2: Deploy Everything

```powershell
pwsh -File scripts/Deploy-AppLayer.ps1 -Component All
```

**Deploys**:
- Hartonomous.Api
- Hartonomous.Web
- Hartonomous.Workers.CesConsumer
- Hartonomous.Workers.EmbeddingGenerator
- Hartonomous.Workers.Neo4jSync

---

### Option 3: Dry Run (See what would happen)

```powershell
pwsh -File scripts/Deploy-AppLayer.ps1 -Component API -DryRun
```

**Shows**: What would be deployed without actually deploying

---

## ?? What the Script Does Automatically

### 1. Prerequisite Checks
- SSH connectivity to HART-SERVER
- .NET 8.0 runtime installed (installs if missing)
- Database reachable from HART-SERVER

### 2. Build Applications
- Runs `dotnet publish` for selected components
- Release configuration
- Self-contained: false (uses shared runtime)
- Output: `D:\Repositories\Hartonomous\publish\`

### 3. Deploy Binaries
- Uses `scp` to copy binaries to HART-SERVER
- Target: `~/hartonomous/api/` (or web/, workers/)
- Preserves file permissions

### 4. Create Systemd Services
- Generates service files for each component
- Configures auto-restart on failure
- Sets environment variables (ASPNETCORE_ENVIRONMENT)
- Installs to `/etc/systemd/system/`

### 5. Start Services
- Reloads systemd daemon
- Enables services (auto-start on boot)
- Starts services
- Waits 3 seconds for startup

### 6. Verify Deployment
- Checks service status
- Tests health endpoint
- Shows logs if service fails

---

## ?? After Deployment

### Access the API

From HART-DESKTOP:
```powershell
curl http://HART-SERVER:5000/health
```

From anywhere:
```powershell
curl http://HART-SERVER/health  # If nginx configured
```

### Check Service Status

```bash
ssh ahart@HART-SERVER "sudo systemctl status hartonomous-api"
```

### View Logs

```bash
ssh ahart@HART-SERVER "sudo journalctl -u hartonomous-api -f"
```

### Restart Service

```bash
ssh ahart@HART-SERVER "sudo systemctl restart hartonomous-api"
```

---

## ?? Configuration

### API Configuration (appsettings.Production.json)

The API will use:
- **Database**: Server=HART-DESKTOP;Database=Hartonomous;...
- **Neo4j**: bolt://localhost:7687 (running on HART-SERVER)
- **Azure Arc**: Managed Identity for Key Vault access
- **Port**: 5000 (HTTP), 5001 (HTTPS if configured)

**Note**: Configuration is embedded in binaries during publish. To change config:
1. Edit `src/Hartonomous.Api/appsettings.Production.json`
2. Re-run deployment script

---

## ?? Security Notes

### Authentication
- Development: Authentication disabled (DisableAuth: true)
- Production: Entra ID authentication required

### Database Access
- Uses Azure Arc Managed Identity (no passwords)
- Connection string includes: Authentication=Active Directory Default

### Secrets
- Neo4j password: Retrieved from Azure Key Vault
- Azure services: Managed Identity authentication

---

## ?? Known Issues & Workarounds

### Issue 1: .NET Not Found
**Error**: `dotnet: command not found`  
**Fix**: Script will offer to install automatically (choose 'Y')

### Issue 2: Permission Denied (Systemd)
**Error**: `Failed to create service file`  
**Fix**: Ensure you have sudo access on HART-SERVER

### Issue 3: Port 5000 Already in Use
**Error**: `Address already in use`  
**Fix**: 
```bash
ssh ahart@HART-SERVER "sudo lsof -ti:5000 | xargs sudo kill -9"
```

### Issue 4: Database Unreachable
**Error**: `Cannot connect to database`  
**Fix**: 
- Verify HART-DESKTOP SQL Server allows remote connections
- Check Windows Firewall on HART-DESKTOP (port 1433)
- Verify SQL Server TCP/IP protocol enabled

---

## ?? Deployment Architecture

```
???????????????????????????????????????????????????????
?  HART-DESKTOP (Windows)                             ?
?  ?? SQL Server 2025                                 ?
?  ?  ?? Database: Hartonomous ? DEPLOYED            ?
?  ?? Development Tools (Visual Studio, .NET SDK)     ?
???????????????????????????????????????????????????????
                   ?
                   ? SQL Connection (Azure AD)
                   ? Port 1433
                   ? Latency: 0.3-0.4ms
                   ?
???????????????????????????????????????????????????????
?  HART-SERVER (Linux Ubuntu 22.04)                   ?
?  ?? Hartonomous.Api ?? READY TO DEPLOY              ?
?  ?  ?? Kestrel (port 5000)                          ?
?  ?  ?? Systemd service                              ?
?  ?? Hartonomous.Workers ?? READY TO DEPLOY          ?
?  ?  ?? CES Consumer (OODA loop)                     ?
?  ?  ?? Embedding Generator                          ?
?  ?  ?? Neo4j Sync                                   ?
?  ?? Neo4j ? RUNNING (port 7687)                    ?
?  ?? Nginx ? RUNNING (port 80)                      ?
?  ?? Azure Arc Agent ? CONNECTED                    ?
???????????????????????????????????????????????????????
```

---

## ?? Next Steps

1. **Run the deployment script**:
   ```powershell
   pwsh -File scripts/Deploy-AppLayer.ps1 -Component API
   ```

2. **Verify the API is running**:
   ```powershell
   curl http://HART-SERVER:5000/health
   ```

3. **Test an endpoint**:
   ```powershell
   curl http://HART-SERVER:5000/swagger
   ```

4. **Deploy workers** (after API is working):
   ```powershell
   pwsh -File scripts/Deploy-AppLayer.ps1 -Component Workers
   ```

---

## ? Success Criteria

After running the script, you should see:
- ? API service running (systemctl status shows "active (running)")
- ? Health endpoint returns 200 OK
- ? Swagger UI accessible at http://HART-SERVER:5000/swagger
- ? No errors in logs (journalctl -u hartonomous-api)
- ? Database queries work (check logs for SQL connections)

---

**Ready to deploy!** ??

Run: `pwsh -File scripts/Deploy-AppLayer.ps1 -Component API`
