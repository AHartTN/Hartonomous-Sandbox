# ?? QUICK START - Run the App Layer

**Current Status**: Database deployed ?, Entities scaffolded ?, App builds ?

---

## ?? Start API in 3 Commands

```powershell
# 1. Navigate to API project
cd D:\Repositories\Hartonomous\src\Hartonomous.Api

# 2. Run the API
dotnet run --configuration Release

# 3. Test it (new terminal)
curl http://localhost:5000/health
```

**Expected**: API starts on port 5000, health endpoint returns 200 OK

---

## ?? Full Stack (API + Workers)

### Terminal Layout

```
?????????????????????????????????????????????
?  Terminal 1: API    ?  Terminal 2: Web UI ?
?  Port: 5000         ?  Port: 5100         ?
?????????????????????????????????????????????
?????????????????????????????????????????????
?  Terminal 3: CES    ?  Terminal 4: Embed  ?
?  Worker (OODA)      ?  Worker (Vectors)   ?
?????????????????????????????????????????????
???????????????????????????????????????????????
?  Terminal 5: Neo4j Sync                     ?
?  Worker (Graph)                             ?
???????????????????????????????????????????????
```

### Commands

```powershell
# Terminal 1: API
cd src\Hartonomous.Api
dotnet run -c Release

# Terminal 2: Web UI
cd src\Hartonomous.Web
dotnet run -c Release

# Terminal 3: CES Consumer (OODA Loop)
cd src\Hartonomous.Workers.CesConsumer
dotnet run -c Release

# Terminal 4: Embedding Generator
cd src\Hartonomous.Workers.EmbeddingGenerator
dotnet run -c Release

# Terminal 5: Neo4j Sync
cd src\Hartonomous.Workers.Neo4jSync
dotnet run -c Release
```

---

## ?? Verify Everything Works

### 1. Database (Already Verified ?)
```bash
sqlcmd -S localhost -d Hartonomous -C -Q "SELECT COUNT(*) FROM dbo.Atom;"
```

### 2. API Endpoints
```bash
# Health check
curl http://localhost:5000/health

# Swagger UI
start http://localhost:5000/swagger

# Example endpoint (if exists)
curl http://localhost:5000/api/atoms
```

### 3. Web UI
```bash
start http://localhost:5100
```

### 4. Workers
Check console output - workers should show:
- ? Connected to database
- ? Connected to Service Broker queues
- ? Listening for messages

---

## ?? What Each Component Does

### API (Port 5000)
- REST endpoints for atoms, inference, concepts
- Triggers OODA loop via Service Broker
- Serves Swagger documentation

### Web UI (Port 5100)
- User interface for interacting with system
- Visualizes atoms, concepts, provenance
- Admin dashboard

### CES Consumer Worker
- Listens to Service Broker queues
- Processes OODA loop messages (Observe, Analyze, Hypothesize, Act)
- Executes autonomous decisions

### Embedding Generator Worker
- Generates vector embeddings for atoms
- Updates AtomEmbedding table
- Uses CLR functions for vector operations

### Neo4j Sync Worker
- Syncs provenance graph to Neo4j
- Maintains graph database consistency
- Enables graph queries

---

## ? Quick Commands Reference

```powershell
# Build everything
dotnet build Hartonomous.sln -c Release

# Run API
dotnet run --project src\Hartonomous.Api\Hartonomous.Api.csproj -c Release

# Run tests
dotnet test Hartonomous.Tests.sln

# Redeploy database
pwsh -File scripts\Deploy.ps1

# Check build status
dotnet build Hartonomous.sln -c Release 2>&1 | Select-String "succeeded|failed"
```

---

Ready to start the API now? Just run:
```powershell
cd src\Hartonomous.Api
dotnet run
```
