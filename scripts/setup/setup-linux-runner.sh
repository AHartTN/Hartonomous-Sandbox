#!/bin/bash
#
# Hartonomous Linux Runner Prerequisites - Fully Automated Installer
# Run with: sudo ./setup-linux-runner.sh
#
# IDEMPOTENT - Safe to run multiple times
# Based on official Microsoft documentation for Ubuntu 22.04

set -e

if [ "$EUID" -ne 0 ]; then
    echo "ERROR: Must run as root"
    echo "Run: sudo $0"
    exit 1
fi

echo ""
echo "=== Hartonomous Linux Runner Prerequisites Setup ==="
echo ""

installed=0
skipped=0

# =============================================================================
# STEP 1: .NET 10 SDK (Official Microsoft method for Ubuntu 22.04)
# =============================================================================
echo "[1/4] .NET 10 SDK"

if command -v dotnet &>/dev/null && [ $(dotnet --version 2>/dev/null | cut -d. -f1) -ge 10 ] 2>/dev/null; then
    VER=$(dotnet --version)
    echo "      ✓ Already installed: $VER"
    skipped=$((skipped + 1))
else
    echo "      Installing via Ubuntu backports PPA..."
    
    # Add .NET backports repository (official method for .NET 10 on Ubuntu 22.04)
    add-apt-repository -y ppa:dotnet/backports >/dev/null 2>&1 || true
    apt-get update >/dev/null 2>&1
    
    # Install .NET 10 SDK
    apt-get install -y dotnet-sdk-10.0 >/dev/null 2>&1
    
    if command -v dotnet &>/dev/null; then
        VER=$(dotnet --version)
        echo "      ✓ Installed: $VER"
        installed=$((installed + 1))
    else
        echo "      ✗ Installation failed"
        exit 1
    fi
fi

# =============================================================================
# STEP 2: PowerShell 7
# =============================================================================
echo "[2/4] PowerShell 7"

if command -v pwsh &>/dev/null; then
    VER=$(pwsh --version | grep -oP '\d+\.\d+\.\d+' || echo "installed")
    echo "      ✓ Already installed: $VER"
    skipped=$((skipped + 1))
else
    echo "      Installing..."
    apt-get update >/dev/null 2>&1
    apt-get install -y powershell >/dev/null 2>&1
    
    if command -v pwsh &>/dev/null; then
        VER=$(pwsh --version | grep -oP '\d+\.\d+\.\d+' || echo "installed")
        echo "      ✓ Installed: $VER"
        installed=$((installed + 1))
    fi
fi

# =============================================================================
# STEP 3: Git
# =============================================================================
echo "[3/4] Git"

if command -v git &>/dev/null; then
    VER=$(git --version | cut -d' ' -f3)
    echo "      ✓ Already installed: $VER"
    skipped=$((skipped + 1))
else
    echo "      Installing..."
    apt-get install -y git >/dev/null 2>&1
    VER=$(git --version | cut -d' ' -f3)
    echo "      ✓ Installed: $VER"
    installed=$((installed + 1))
fi

# =============================================================================
# STEP 4: Docker
# =============================================================================
echo "[4/4] Docker"

RUNNER_USER=$(ps aux | grep -E 'actions.runner.*Runner.Listener' | grep -v grep | awk '{print $1}' | head -1)
if [ -z "$RUNNER_USER" ]; then
    RUNNER_USER="ahart"
fi

if command -v docker &>/dev/null; then
    VER=$(docker --version | cut -d' ' -f3 | tr -d ',')
    
    if groups $RUNNER_USER | grep -q docker; then
        echo "      ✓ Already installed: $VER (permissions OK)"
        skipped=$((skipped + 1))
    else
        echo "      Adding $RUNNER_USER to docker group..."
        usermod -aG docker $RUNNER_USER
        echo "      ✓ Permissions fixed"
        installed=$((installed + 1))
    fi
else
    echo "      Installing..."
    apt-get install -y docker.io >/dev/null 2>&1
    systemctl enable docker >/dev/null 2>&1
    systemctl start docker >/dev/null 2>&1
    usermod -aG docker $RUNNER_USER
    
    VER=$(docker --version | cut -d' ' -f3 | tr -d ',')
    echo "      ✓ Installed: $VER"
    installed=$((installed + 1))
fi

# =============================================================================
# Restart Runner Service
# =============================================================================
RUNNER_SERVICE=$(systemctl list-units --type=service --all | grep -i "actions.runner" | awk '{print $1}' | head -1)

if [ -n "$RUNNER_SERVICE" ]; then
    echo ""
    echo "Restarting $RUNNER_SERVICE..."
    systemctl restart "$RUNNER_SERVICE" 2>/dev/null || true
    sleep 2
    
    if systemctl is-active --quiet "$RUNNER_SERVICE"; then
        echo "✓ Service restarted"
    fi
fi

# =============================================================================
# SUMMARY
# =============================================================================
echo ""
echo "=== Setup Complete ==="
echo "  Already installed: $skipped"
echo "  Newly installed:   $installed"
echo ""
echo "Verify: dotnet --version"
echo ""

exit 0
