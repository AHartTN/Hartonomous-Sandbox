# Hartonomous Worker Services Deployment Guide

This directory contains SystemD service files for the Hartonomous worker services.

## Worker Services

### 1. CES Consumer Worker (`hartonomous-ces-consumer.service`)
- **Purpose**: Polls SQL Server Service Broker IngestionQueue
- **Function**: Atomizes incoming files and data
- **Dependencies**: SQL Server (mssql-server.service)
- **Resources**: 2GB RAM, 200% CPU

### 2. Neo4j Sync Worker (`hartonomous-neo4j-sync.service`)
- **Purpose**: Polls SQL Server Service Broker Neo4jSyncQueue
- **Function**: Synchronizes provenance graph to Neo4j
- **Dependencies**: SQL Server, Neo4j
- **Resources**: 1GB RAM, 100% CPU

### 3. Embedding Generator Worker (`hartonomous-embedding-generator.service`)
- **Purpose**: Generates embeddings for atoms without embeddings
- **Function**: Background embedding generation (optional)
- **Dependencies**: SQL Server
- **Resources**: 4GB RAM, 400% CPU (supports GPU if available)

## Installation

### Prerequisites

1. Create hartonomous user:
```bash
sudo useradd -r -s /bin/false hartonomous
sudo mkdir -p /srv/www/hartonomous/workers/{ces-consumer,neo4j-sync,embedding-generator}
sudo mkdir -p /var/log/hartonomous
sudo chown -R hartonomous:hartonomous /srv/www/hartonomous /var/log/hartonomous
```

2. Deploy worker binaries to:
- `/srv/www/hartonomous/workers/ces-consumer/`
- `/srv/www/hartonomous/workers/neo4j-sync/`
- `/srv/www/hartonomous/workers/embedding-generator/`

### Install Services

```bash
# Copy service files
sudo cp hartonomous-ces-consumer.service /etc/systemd/system/
sudo cp hartonomous-neo4j-sync.service /etc/systemd/system/
sudo cp hartonomous-embedding-generator.service /etc/systemd/system/

# Reload systemd
sudo systemctl daemon-reload

# Enable services (start on boot)
sudo systemctl enable hartonomous-ces-consumer
sudo systemctl enable hartonomous-neo4j-sync
sudo systemctl enable hartonomous-embedding-generator

# Start services
sudo systemctl start hartonomous-ces-consumer
sudo systemctl start hartonomous-neo4j-sync
sudo systemctl start hartonomous-embedding-generator
```

## Management

### Check Status
```bash
sudo systemctl status hartonomous-ces-consumer
sudo systemctl status hartonomous-neo4j-sync
sudo systemctl status hartonomous-embedding-generator
```

### View Logs
```bash
# Real-time logs
sudo journalctl -u hartonomous-ces-consumer -f
sudo journalctl -u hartonomous-neo4j-sync -f
sudo journalctl -u hartonomous-embedding-generator -f

# Last 100 lines
sudo journalctl -u hartonomous-ces-consumer -n 100
```

### Restart Services
```bash
sudo systemctl restart hartonomous-ces-consumer
sudo systemctl restart hartonomous-neo4j-sync
sudo systemctl restart hartonomous-embedding-generator
```

### Stop Services
```bash
sudo systemctl stop hartonomous-ces-consumer
sudo systemctl stop hartonomous-neo4j-sync
sudo systemctl stop hartonomous-embedding-generator
```

## Configuration

All workers read configuration from:
- `/srv/www/hartonomous/workers/{worker-name}/appsettings.json`
- `/srv/www/hartonomous/workers/{worker-name}/appsettings.Production.json`
- Azure App Configuration (if enabled)
- Azure Key Vault (if enabled)

### Environment Variables

The service files set:
- `DOTNET_ENVIRONMENT=Production`
- `ASPNETCORE_ENVIRONMENT=Production`

Additional environment variables can be added to the service files.

## Security

All services run with:
- **User**: `hartonomous` (non-root)
- **NoNewPrivileges**: Prevents privilege escalation
- **PrivateTmp**: Isolated temporary directory
- **ProtectSystem**: Read-only access to /usr, /boot, /efi
- **ProtectHome**: No access to home directories
- **ReadWritePaths**: Only /var/log/hartonomous is writable

## Monitoring

### Health Checks

Workers log health information to journald. Monitor with:
```bash
# Check for errors in last hour
sudo journalctl -u hartonomous-ces-consumer --since "1 hour ago" --priority err
```

### Application Insights

All workers send telemetry to Application Insights:
- Request duration metrics
- Dependency tracking (SQL Server, Neo4j)
- Custom events (ingestion completed, sync completed)
- Exception tracking

### Prometheus/Grafana (Optional)

Workers expose metrics compatible with OpenTelemetry exporters.

## Troubleshooting

### Worker won't start
```bash
# Check service status
sudo systemctl status hartonomous-ces-consumer

# Check logs
sudo journalctl -u hartonomous-ces-consumer -n 50

# Check file permissions
ls -la /srv/www/hartonomous/workers/ces-consumer

# Verify dependencies
sudo systemctl status mssql-server
sudo systemctl status neo4j
```

### High CPU/Memory usage
```bash
# Check resource usage
systemctl show hartonomous-ces-consumer | grep -E 'MemoryCurrent|CPUUsage'

# Adjust limits in service file
sudo systemctl edit hartonomous-ces-consumer
```

### Messages not processing
```bash
# Check Service Broker queues
sqlcmd -S localhost -d Hartonomous -Q "SELECT name, messages_count FROM sys.service_queues"

# Verify worker is polling
sudo journalctl -u hartonomous-ces-consumer -f | grep "Received message"
```

## Deployment Automation

See `Deploy.ps1` for automated deployment script that:
1. Builds worker projects
2. Publishes to deployment folder
3. Copies binaries to servers
4. Installs/updates SystemD services
5. Restarts workers

## Production Checklist

- [ ] Workers deployed to /srv/www/hartonomous/workers/
- [ ] SystemD service files installed
- [ ] Services enabled for auto-start
- [ ] Configuration files present (appsettings.json)
- [ ] Azure App Config connection working
- [ ] Azure Key Vault secrets accessible
- [ ] Application Insights connection working
- [ ] SQL Server Service Broker queues created
- [ ] Neo4j connection verified
- [ ] Health checks passing
- [ ] Logs visible in journalctl
- [ ] No errors in last 24 hours
