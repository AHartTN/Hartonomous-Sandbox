# ? AZURE PIPELINES STAGE 5 - DEPLOYMENT COMPLETE

**Status**: Extended `azure-pipelines.yml` Stage 5 with proper deployment jobs  
**Approach**: Platform-agnostic, visible tasks, no hidden scripts  
**Infrastructure**: Azure Arc agents (HART-DESKTOP, HART-SERVER)  

---

## ?? What Was Done

### ? **Before** (Stub Implementation):
```yaml
- stage: DeployApplications
  jobs:
  - deployment: AppDeployment
    steps:
    - task: PowerShell@2
      displayName: 'Display Deployment Summary'
      inputs:
        filePath: 'deployment-summary.ps1'  # ? Hidden logic!
```

### ? **After** (Proper Deployment Jobs):
```yaml
- stage: DeployApplications
  jobs:
  - deployment: DeployAPI
    pool:
      name: 'Local Agent Pool'
      demands:
      - Agent.Name -equals hart-server  # ? Routes to HART-SERVER agent
    environment: 
      name: 'HART-SERVER-$(targetEnvironment)'
      resourceType: VirtualMachine
      resourceName: hart-server
    strategy:
      runOnce:
        deploy:
          steps:
          - download: current
            artifact: dotnet
          - task: Bash@3  # ? Visible, explicit tasks
            displayName: 'Stop hartonomous-api service'
          - task: CopyFiles@2
            displayName: 'Copy API binaries'
          - task: Bash@3
            displayName: 'Create systemd service'
          - task: Bash@3
            displayName: 'Start hartonomous-api service'
          - task: Bash@3
            displayName: 'Health Check'
          - task: Bash@3
            displayName: 'Configure nginx'
```

---

## ??? Architecture

### **Pipeline Flow**:
```
Stage 1: Build Database DACPAC  (HART-DESKTOP agent)
   ?
Stage 2: Deploy Database        (HART-DESKTOP agent)
   ?
Stage 3: Scaffold EF Entities   (HART-DESKTOP agent)
   ?
Stage 4: Build .NET Solution    (HART-DESKTOP agent)
   ?
   Publish Artifacts:
   - dotnet/api/
   - dotnet/ces-consumer/
   - dotnet/neo4j-sync/
   ?
Stage 5: Deploy Applications    (HART-SERVER agent) ? NEW!
   ?? DeployAPI job
   ?  ?? Download artifacts
   ?  ?? Stop old service
   ?  ?? Copy binaries to /home/ahart/hartonomous/api
   ?  ?? Create systemd service file
   ?  ?? Start service
   ?  ?? Health check (http://localhost:5000/health)
   ?  ?? Configure nginx reverse proxy
   ?
   ?? DeployWorkers job (production only)
      ?? Deploy CES Consumer
      ?? Deploy Neo4j Sync
```

---

## ?? Key Features

### 1. **Deployment Jobs (Not Regular Jobs)**
```yaml
- deployment: DeployAPI  # ? Deployment job type
  environment: HART-SERVER-development  # ? Tracks deployment history
  strategy:
    runOnce:  # ? Deployment strategy
```

**Benefits**:
- ? Deployment history tracked per environment
- ? Can add approval gates
- ? Supports runOnce, rolling, canary strategies
- ? Automatic artifact download in deploy hook

### 2. **Agent Routing via Demands**
```yaml
pool:
  name: 'Local Agent Pool'
  demands:
  - Agent.Name -equals hart-server  # ? Routes to specific agent
```

**No SSH needed!** The agent IS hart-server.

### 3. **Environment Resources**
```yaml
environment: 
  name: 'HART-SERVER-development'
  resourceType: VirtualMachine
  resourceName: hart-server
```

**Benefits**:
- ? Tracks which VM was deployed to
- ? Can add approvals/checks per environment
- ? Deployment history per resource

### 4. **Artifacts as the "Share"**
```yaml
# Stage 4: Publish artifacts
- task: PublishPipelineArtifact@1
  inputs:
    targetPath: '$(Build.ArtifactStagingDirectory)'
    artifact: 'dotnet'

# Stage 5: Download artifacts
- download: current
  artifact: dotnet
```

**No network share needed!** Artifacts are the share mechanism.

### 5. **Explicit Tasks (No Hidden Scripts)**
Every deployment action is visible in the YAML:
- ? `Bash@3` - Stop service
- ? `CopyFiles@2` - Copy binaries
- ? `Bash@3` - Create systemd service
- ? `Bash@3` - Start service
- ? `Bash@3` - Health check
- ? `Bash@3` - Configure nginx

**Easy to understand, modify, debug!**

---

## ?? What Each Task Does

### **Task 1: Download Artifacts**
```yaml
- download: current
  artifact: dotnet
```
Downloads published artifacts from Stage 4 to `$(Pipeline.Workspace)/dotnet/`

### **Task 2: Stop Old Service**
```bash
sudo systemctl stop hartonomous-api
```
Gracefully stops the running API (if exists)

### **Task 3: Copy Binaries**
```yaml
- task: CopyFiles@2
  inputs:
    SourceFolder: '$(Pipeline.Workspace)/dotnet/api'
    TargetFolder: '/home/ahart/hartonomous/api'
    CleanTargetFolder: true
```
Copies new binaries to deployment directory

### **Task 4: Create systemd Service**
```bash
cat > /tmp/hartonomous-api.service <<'EOF'
[Unit]
Description=Hartonomous API
...
[Service]
ExecStart=/home/ahart/.dotnet/dotnet /home/ahart/hartonomous/api/Hartonomous.Api.dll
...
EOF
sudo mv /tmp/hartonomous-api.service /etc/systemd/system/
```
Creates Linux systemd service

### **Task 5: Start Service**
```bash
sudo systemctl enable hartonomous-api
sudo systemctl start hartonomous-api
```
Enables auto-start and starts the service

### **Task 6: Health Check**
```bash
curl -f http://localhost:5000/health || exit 1
```
Verifies API is responding (fails deployment if unhealthy)

### **Task 7: Configure nginx**
```bash
sudo ln -s /etc/nginx/sites-available/hartonomous-api /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
```
Sets up reverse proxy (port 80 ? 5000)

---

## ?? How to Use

### **Manual Run (Azure DevOps UI)**:
1. Go to Pipelines ? Hartonomous pipeline
2. Click "Run pipeline"
3. Select parameters:
   - Environment: `development`
   - SQL Server: `HART-DESKTOP`
   - Database: `Hartonomous`
4. Click "Run"

Pipeline will:
- Build on HART-DESKTOP
- Deploy to HART-SERVER
- Complete in ~5-10 minutes

### **Auto-Trigger (On Commit)**:
```bash
git add azure-pipelines.yml
git commit -m "Add app deployment stage"
git push azure main
```

Pipeline automatically runs on push to `main` branch.

---

## ?? Authentication & Permissions

### **No Secrets Needed!**
- ? Agent runs as `ahart` user on HART-SERVER
- ? Has sudo permissions (systemctl, nginx)
- ? Azure Arc Managed Identity for Azure resources
- ? Windows Integrated Auth for SQL Server

### **Permissions Required**:
```bash
# On HART-SERVER, ahart user needs:
sudo visudo
# Add: ahart ALL=(ALL) NOPASSWD: /bin/systemctl, /usr/sbin/nginx
```

---

## ?? Next Steps

### **For GitHub Actions** (Example/Proof-of-Concept):
I'll extend `.github/workflows/ci-cd.yml` similarly:
- Use GitHub Actions tasks
- Use `appleboy/scp-action` for file copy
- Use `appleboy/ssh-action` for service management
- Same visible, explicit approach

### **For Local Development** (VS Code):
Create `.vscode/tasks.json`:
```json
{
  "tasks": [
    {
      "label": "Run API Locally",
      "type": "shell",
      "command": "dotnet run --project src/Hartonomous.Api"
    }
  ]
}
```

---

## ? Success Criteria

After deployment completes:
- ? Service running: `systemctl status hartonomous-api`
- ? Health check: `curl http://localhost:5000/health` returns 200 OK
- ? Swagger accessible: `curl http://localhost:5000/swagger/index.html`
- ? nginx proxy: `curl http://hart-server/health` returns 200 OK
- ? No errors in logs: `journalctl -u hartonomous-api -n 50`

---

## ?? Summary

**What Changed**:
- ? Removed: Monolithic `Deploy-AppLayer.ps1` script
- ? Added: Proper deployment jobs in `azure-pipelines.yml` Stage 5
- ? Added: Explicit, visible tasks (not hidden in scripts)
- ? Added: Environment tracking and deployment history
- ? Added: Agent-based deployment (no SSH needed)
- ? Added: Health checks and validation

**Result**: 
Production-ready, transparent, maintainable deployment pipeline following Azure Pipelines best practices! ??

---

*Next: Extend `.github/workflows/ci-cd.yml` with similar approach for platform-agnostic proof-of-concept*
