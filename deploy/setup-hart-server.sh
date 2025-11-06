#!/bin/bash
set -e

echo "========================================"
echo "  HART-SERVER Setup for Hartonomous"
echo "========================================"
echo ""

# Check if running as correct user
if [ "$USER" != "ahart" ]; then
    echo "ERROR: This script must be run as user 'ahart'"
    exit 1
fi

# Install .NET 10 SDK/Runtime
echo "Installing .NET 10 Runtime..."
if ! command -v dotnet &> /dev/null; then
    # Add Microsoft package repository
    wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
    sudo dpkg -i /tmp/packages-microsoft-prod.deb
    rm /tmp/packages-microsoft-prod.deb
    
    # Install .NET Runtime
    sudo apt-get update
    sudo apt-get install -y dotnet-runtime-10.0 || sudo apt-get install -y dotnet-runtime-9.0 || sudo apt-get install -y dotnet-runtime-8.0
    
    echo "✓ .NET Runtime installed"
else
    echo "✓ .NET already installed: $(dotnet --version)"
fi
echo ""

# Create directory structure in /srv/www
echo "Creating directory structure in /srv/www/hartonomous..."
mkdir -p /srv/www/hartonomous/{api,ces-consumer,neo4j-sync,model-ingestion}
chmod 755 /srv/www/hartonomous
echo "✓ Directories created"
echo ""

# Check disk space
echo "Disk space on /srv/www:"
df -h /srv/www
echo ""

echo "========================================"
echo "  Setup Complete!"
echo "========================================"
echo ""
echo "Next steps:"
echo "  1. Run deploy-to-hart-server.ps1 from HART-DESKTOP"
echo "  2. Verify services with: sudo systemctl status hartonomous-*"
echo ""
