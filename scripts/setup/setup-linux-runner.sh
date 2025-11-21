#!/bin/bash
#
# Hartonomous Linux Runner Prerequisites - Fully Automated Installer
# Run with: sudo ./setup-linux-runner.sh
#
# This script is IDEMPOTENT - safe to run multiple times
# It will detect what's already installed and only install what's missing
#

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo ""
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${CYAN}  Hartonomous Linux Runner - Automated Setup${NC}"
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo ""

# Check root
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}ERROR: Must run as root${NC}"
    echo "Run: sudo $0"
    exit 1
fi

installed=0
skipped=0

# =============================================================================
# STEP 1: .NET 10 SDK (SYSTEM-WIDE via apt)
# =============================================================================
echo -e "${CYAN}[1/5] .NET 10 SDK${NC}"

if command -v dotnet &>/dev/null && [ $(dotnet --version | cut -d. -f1) -ge 10 ]; then
    VER=$(dotnet --version)
    echo -e "      ${GREEN}✓ Already installed: $VER${NC}"
    skipped=$((skipped + 1))
else
    echo "      Installing system-wide..."
    
    # Add Microsoft package repo
    wget -q https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
    dpkg -i /tmp/packages-microsoft-prod.deb >/dev/null 2>&1
    rm /tmp/packages-microsoft-prod.deb
    
    # Install .NET SDK
    apt-get update >/dev/null 2>&1
    apt-get install -y dotnet-sdk-10.0 >/dev/null 2>&1
    
    VER=$(dotnet --version)
    echo -e "      ${GREEN}✓ Installed: $VER (system-wide)${NC}"
    installed=$((installed + 1))
fi

# =============================================================================
# STEP 2: PowerShell 7
# =============================================================================
echo -e "${CYAN}[2/5] PowerShell 7${NC}"

if command -v pwsh &>/dev/null; then
    VER=$(pwsh --version | grep -oP '\d+\.\d+\.\d+' || echo "installed")
    echo -e "      ${GREEN}✓ Already installed: $VER${NC}"
    skipped=$((skipped + 1))
else
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        if [[ "$ID" == "ubuntu" ]]; then
            echo "      Installing..."
            apt-get update >/dev/null 2>&1
            apt-get install -y powershell >/dev/null 2>&1
            
            VER=$(pwsh --version | grep -oP '\d+\.\d+\.\d+' || echo "installed")
            echo -e "      ${GREEN}✓ Installed: $VER${NC}"
            installed=$((installed + 1))
        else
            echo -e "      ${YELLOW}⚠ Unsupported OS: $ID${NC}"
        fi
    fi
fi

# =============================================================================
# STEP 3: Git
# =============================================================================
echo -e "${CYAN}[3/5] Git${NC}"

if command -v git &>/dev/null; then
    VER=$(git --version | cut -d' ' -f3)
    echo -e "      ${GREEN}✓ Already installed: $VER${NC}"
    skipped=$((skipped + 1))
else
    echo "      Installing..."
    apt-get update >/dev/null 2>&1
    apt-get install -y git >/dev/null 2>&1
    
    VER=$(git --version | cut -d' ' -f3)
    echo -e "      ${GREEN}✓ Installed: $VER${NC}"
    installed=$((installed + 1))
fi

# =============================================================================
# STEP 4: Docker
# =============================================================================
echo -e "${CYAN}[4/5] Docker${NC}"

# Detect runner user
RUNNER_USER=$(ps aux | grep -E 'actions.runner.*Runner.Listener' | grep -v grep | awk '{print $1}' | head -1)
if [ -z "$RUNNER_USER" ]; then
    RUNNER_USER="ahart"  # fallback
fi

if command -v docker &>/dev/null; then
    VER=$(docker --version | cut -d' ' -f3 | tr -d ',')
    
    if sudo -u $RUNNER_USER docker ps &>/dev/null 2>&1; then
        echo -e "      ${GREEN}✓ Already installed: $VER (permissions OK)${NC}"
        skipped=$((skipped + 1))
    else
        echo "      Fixing permissions..."
        usermod -aG docker $RUNNER_USER
        echo -e "      ${GREEN}✓ Added $RUNNER_USER to docker group${NC}"
        installed=$((installed + 1))
    fi
else
    echo "      Installing..."
    apt-get update >/dev/null 2>&1
    apt-get install -y docker.io >/dev/null 2>&1
    systemctl enable docker >/dev/null 2>&1
    systemctl start docker >/dev/null 2>&1
    usermod -aG docker $RUNNER_USER
    
    VER=$(docker --version | cut -d' ' -f3 | tr -d ',')
    echo -e "      ${GREEN}✓ Installed: $VER${NC}"
    installed=$((installed + 1))
fi

# =============================================================================
# STEP 5: Restart Runner Service
# =============================================================================
echo -e "${CYAN}[5/5] Runner Service${NC}"

RUNNER_SERVICE=$(systemctl list-units --type=service --all | grep -i "actions.runner" | awk '{print $1}' | head -1)

if [ -n "$RUNNER_SERVICE" ]; then
    echo "      Restarting $RUNNER_SERVICE..."
    systemctl restart "$RUNNER_SERVICE"
    sleep 2
    
    if systemctl is-active --quiet "$RUNNER_SERVICE"; then
        echo -e "      ${GREEN}✓ Service restarted${NC}"
    else
        echo -e "      ${RED}✗ Service restart failed${NC}"
    fi
else
    echo -e "      ${YELLOW}⚠ Could not detect runner service${NC}"
fi

# =============================================================================
# SUMMARY
# =============================================================================
echo ""
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}✅ SETUP COMPLETE${NC}"
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo ""
echo "  Installed:       $installed"
echo "  Already present: $skipped"
echo ""
echo "Verify: dotnet --version"
echo ""

exit 0
