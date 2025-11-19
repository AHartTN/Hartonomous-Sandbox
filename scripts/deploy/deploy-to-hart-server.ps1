#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy Hartonomous services to HART-SERVER
.DESCRIPTION
    Deploys API, CesConsumer, Neo4jSync, and ModelIngestion to HART-SERVER via SSH
.PARAMETER SkipBuild
    Skip building the services (use existing publish folders)
#>
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$server = "ahart@192.168.1.2"
$deployRoot = "/srv/www/hartonomous"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Hartonomous Deployment to HART-SERVER" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Build services
if (-not $SkipBuild) {
    Write-Host "Building services..." -ForegroundColor Yellow
    
    dotnet publish src/Hartonomous.Api/Hartonomous.Api.csproj -c Release -o publish/api --self-contained false
    dotnet publish src/CesConsumer/CesConsumer.csproj -c Release -o publish/ces-consumer --self-contained false
    dotnet publish src/Neo4jSync/Neo4jSync.csproj -c Release -o publish/neo4j-sync --self-contained false
    dotnet publish src/ModelIngestion/ModelIngestion.csproj -c Release -o publish/model-ingestion --self-contained false
    
    Write-Host "✓ Build complete" -ForegroundColor Green
    Write-Host ""
}

# Check .NET installation
Write-Host "Checking .NET installation..." -ForegroundColor Yellow
$dotnetCheck = ssh $server "dotnet --version 2>/dev/null || echo 'NOT_INSTALLED'"
if ($dotnetCheck -eq "NOT_INSTALLED") {
    Write-Host "ERROR: .NET is not installed on HART-SERVER" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install .NET 8.0 Runtime or later:" -ForegroundColor Yellow
    Write-Host "  wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh" -ForegroundColor White
    Write-Host "  bash /tmp/dotnet-install.sh --channel 8.0 --runtime aspnetcore --install-dir ~/.dotnet" -ForegroundColor White
    Write-Host "  echo 'export PATH=`$PATH:`$HOME/.dotnet' >> ~/.bashrc" -ForegroundColor White
    Write-Host "  source ~/.bashrc" -ForegroundColor White
    exit 1
} else {
    Write-Host "✓ .NET $dotnetCheck installed" -ForegroundColor Green
}
Write-Host ""

# Stop services
Write-Host "Stopping services on HART-SERVER..." -ForegroundColor Yellow
ssh $server "systemctl --user stop hartonomous-api hartonomous-ces-consumer hartonomous-neo4j-sync hartonomous-model-ingestion 2>/dev/null || echo 'Services not yet installed'"
Write-Host "✓ Services stopped" -ForegroundColor Green
Write-Host ""

# Deploy files
Write-Host "Deploying API..." -ForegroundColor Yellow
scp -r publish/api/* ${server}:${deployRoot}/api/
Write-Host "✓ API deployed" -ForegroundColor Green

Write-Host "Deploying CES Consumer..." -ForegroundColor Yellow
scp -r publish/ces-consumer/* ${server}:${deployRoot}/ces-consumer/
Write-Host "✓ CES Consumer deployed" -ForegroundColor Green

Write-Host "Deploying Neo4j Sync..." -ForegroundColor Yellow
scp -r publish/neo4j-sync/* ${server}:${deployRoot}/neo4j-sync/
Write-Host "✓ Neo4j Sync deployed" -ForegroundColor Green

Write-Host "Deploying Model Ingestion..." -ForegroundColor Yellow
scp -r publish/model-ingestion/* ${server}:${deployRoot}/model-ingestion/
Write-Host "✓ Model Ingestion deployed" -ForegroundColor Green
Write-Host ""

# Install systemd services
Write-Host "Installing systemd user services..." -ForegroundColor Yellow
ssh $server "mkdir -p ~/.config/systemd/user"
scp deploy/*.service ${server}:~/.config/systemd/user/
ssh $server "systemctl --user daemon-reload"
Write-Host "✓ Systemd services installed" -ForegroundColor Green
Write-Host ""

# Start services
Write-Host "Starting services..." -ForegroundColor Yellow
ssh $server "systemctl --user enable hartonomous-api hartonomous-ces-consumer hartonomous-neo4j-sync hartonomous-model-ingestion"
ssh $server "systemctl --user start hartonomous-api hartonomous-ces-consumer hartonomous-neo4j-sync hartonomous-model-ingestion"
Write-Host "✓ Services started" -ForegroundColor Green
Write-Host ""

# Check status
Write-Host "Service Status:" -ForegroundColor Cyan
ssh $server "systemctl --user status hartonomous-api hartonomous-ces-consumer hartonomous-neo4j-sync hartonomous-model-ingestion --no-pager; exit 0"
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "API should be available at: http://192.168.1.2:5000" -ForegroundColor Yellow
Write-Host ""
Write-Host "To view logs:" -ForegroundColor Cyan
Write-Host "  journalctl --user -u hartonomous-api -f" -ForegroundColor White
Write-Host "  journalctl --user -u hartonomous-ces-consumer -f" -ForegroundColor White
Write-Host "  journalctl --user -u hartonomous-neo4j-sync -f" -ForegroundColor White
Write-Host "  journalctl --user -u hartonomous-model-ingestion -f" -ForegroundColor White
