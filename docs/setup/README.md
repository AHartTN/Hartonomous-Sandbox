# Setup Guides

Installation, configuration, and deployment guides for Hartonomous.

---

## Quick Links

- **[Quickstart](../../QUICKSTART.md)** - Get running in 5 minutes
- **Database Setup** - Deploy SQL Server database with CLR (coming soon)
- **Worker Configuration** - Configure background workers (coming soon)
- **Neo4j Setup** - Configure provenance tracking (coming soon)
- **Production Deployment** - Deploy to production environment (coming soon)

---

## Prerequisites

### Required
- SQL Server 2019+ (Developer or Enterprise)
- .NET 8 SDK
- Visual Studio 2022 (with SQL Server Data Tools)

### Optional
- Neo4j 5.x (for provenance tracking)
- Docker (for containerized deployment)
- Azure subscription (for cloud deployment)

---

## Setup Workflow

```
1. Install Prerequisites
    ↓
2. Clone Repository
    ↓
3. Build SQL CLR Project (.NET Framework 4.8.1)
    ↓
4. Deploy Database (DACPAC)
    ↓
5. Configure Workers (appsettings.json)
    ↓
6. Run Workers (Ingestion, Embedding, Spatial Projector, Neo4j Sync)
    ↓
7. Enable OODA Loop (Service Broker)
    ↓
8. Verify System Health
```

See **[QUICKSTART.md](../../QUICKSTART.md)** for step-by-step instructions.

---

## Configuration Files

**appsettings.Development.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Hartonomous;Trusted_Connection=True;",
    "Neo4j": "bolt://localhost:7687"
  },
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "your-password"
  }
}
```

**appsettings.Production.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-sql;Database=Hartonomous;User Id=service-account;Password=xxx;",
    "Neo4j": "bolt://prod-neo4j:7687"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

---

## Detailed Setup Guides

### Database Setup
**Coming soon**: CLR assembly deployment, spatial index configuration, Service Broker setup

### Worker Configuration
**Coming soon**: Background worker setup, service accounts, monitoring

### Neo4j Configuration
**Coming soon**: Graph database schema, constraints, indexes

### Production Deployment
**Coming soon**: Docker containers, Azure deployment, high availability

### Security Hardening
**Coming soon**: SQL Server security, API authentication, network isolation

---

For immediate setup instructions, see **[QUICKSTART.md](../../QUICKSTART.md)**.

For architecture details, see **[ARCHITECTURE.md](../../ARCHITECTURE.md)**.

For complete technical specification, see **[Rewrite Guide](../rewrite-guide/)**.
