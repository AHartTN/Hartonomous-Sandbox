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
echo -e "${CYAN}???????????????????????????????????????????????????????????????${NC}"
echo -e "${CYAN}  Hartonomous Linux Runner - Automated Setup${NC}"
echo -e "${CYAN}???????????????????????????????????????????????????????????????${NC}"
echo ""

# Check root
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}ERROR: Must run as root${NC}"
    echo "Run: sudo $0"
    exit 1
fi

# Detect runner user
RUNNER_USER=$(ps aux | grep -E 'actions.runner.*Runner.Listener' | grep -v grep | awk '{print $1}' | head -1)
if [ -z "$RUNNER_USER" ]; then
    echo -e "${YELLOW}Could not auto-detect runner user. Enter username:${NC}"
    read -p "Username: " RUNNER_USER
fi

if ! id "$RUNNER_USER" &>/dev/null; then
    echo -e "${RED}ERROR: User '$RUNNER_USER' does not exist${NC}"
    exit 1
fi

RUNNER_HOME=$(eval echo "~$RUNNER_USER")
RUNNER_SERVICE=$(systemctl list-units --type=service --all | grep -i "actions.runner" | awk '{print $1}' | head -1)

echo -e "${GREEN}? Runner user: $RUNNER_USER${NC}"
echo -e "${GREEN}? Runner home: $RUNNER_HOME${NC}"
echo -e "${GREEN}? Service: $RUNNER_SERVICE${NC}"
echo ""

installed=0
skipped=0

# =============================================================================
# STEP 1: .NET 10 SDK
# =============================================================================
echo -e "${CYAN}[1/5] .NET 10 SDK${NC}"

if sudo -u $RUNNER_USER bash -c "export PATH=\"$RUNNER_HOME/.dotnet:\$PATH\" && command -v dotnet &>/dev/null && [ \$(dotnet --version | cut -d. -f1) -ge 10 ]"; then
    VER=$(sudo -u $RUNNER_USER bash -c "export PATH=\"$RUNNER_HOME/.dotnet:\$PATH\" && dotnet --version")
    echo -e "      ${GREEN}? Already installed: $VER${NC}"
    skipped=$((skipped + 1))
else
    echo "      Installing..."
    sudo -u $RUNNER_USER bash -c "
        wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
        chmod +x /tmp/dotnet-install.sh
        /tmp/dotnet-install.sh --channel 10.0 --install-dir $RUNNER_HOME/.dotnet --no-path >/dev/null 2>&1
        rm /tmp/dotnet-install.sh
    "
    
    # Update shell configs
    grep -qxF 'export PATH="$HOME/.dotnet:$PATH"' "$RUNNER_HOME/.bashrc" || echo 'export PATH="$HOME/.dotnet:$PATH"' >> "$RUNNER_HOME/.bashrc"
    grep -qxF 'export PATH="$HOME/.dotnet:$PATH"' "$RUNNER_HOME/.profile" || echo 'export PATH="$HOME/.dotnet:$PATH"' >> "$RUNNER_HOME/.profile"
    
    VER=$(sudo -u $RUNNER_USER bash -c "export PATH=\"$RUNNER_HOME/.dotnet:\$PATH\" && dotnet --version")
    echo -e "      ${GREEN}? Installed: $VER${NC}"
    installed=$((installed + 1))
fi

# =============================================================================
# STEP 2: PowerShell 7
# =============================================================================
echo -e "${CYAN}[2/5] PowerShell 7${NC}"

if command -v pwsh &>/dev/null; then
    VER=$(pwsh --version | grep -oP '\d+\.\d+\.\d+' || echo "installed")
    echo -e "      ${GREEN}? Already installed: $VER${NC}"
    skipped=$((skipped + 1))
else
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        if [[ "$ID" == "ubuntu" ]]; then
            echo "      Installing..."
            wget -q https://packages.microsoft.com/config/ubuntu/$VERSION_ID/packages-microsoft-prod.deb -O /tmp/ms-prod.deb
            dpkg -i /tmp/ms-prod.deb >/dev/null 2>&1
            rm /tmp/ms-prod.deb
            apt-get update >/dev/null 2>&1
            apt-get install -y powershell >/dev/null 2>&1
            
            VER=$(pwsh --version | grep -oP '\d+\.\d+\.\d+' || echo "installed")
            echo -e "      ${GREEN}? Installed: $VER${NC}"
            installed=$((installed + 1))
        else
            echo -e "      ${YELLOW}? Unsupported OS: $ID${NC}"
        fi
    fi
fi

# =============================================================================
# STEP 3: Git
# =============================================================================
echo -e "${CYAN}[3/5] Git${NC}"

if command -v git &>/dev/null; then
    VER=$(git --version | cut -d' ' -f3)
    echo -e "      ${GREEN}? Already installed: $VER${NC}"
    skipped=$((skipped + 1))
else
    echo "      Installing..."
    apt-get update >/dev/null 2>&1
    apt-get install -y git >/dev/null 2>&1
    
    VER=$(git --version | cut -d' ' -f3)
    echo -e "      ${GREEN}? Installed: $VER${NC}"
    installed=$((installed + 1))
fi

# =============================================================================
# STEP 4: Docker
# =============================================================================
echo -e "${CYAN}[4/5] Docker${NC}"

if command -v docker &>/dev/null; then
    VER=$(docker --version | cut -d' ' -f3 | tr -d ',')
    
    if sudo -u $RUNNER_USER docker ps &>/dev/null; then
        echo -e "      ${GREEN}? Already installed: $VER (permissions OK)${NC}"
        skipped=$((skipped + 1))
    else
        echo "      Fixing permissions..."
        usermod -aG docker $RUNNER_USER
        echo -e "      ${GREEN}? Added $RUNNER_USER to docker group${NC}"
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
    echo -e "      ${GREEN}? Installed: $VER${NC}"
    installed=$((installed + 1))
fi

# =============================================================================
# STEP 5: Update systemd service with .NET PATH
# =============================================================================
echo -e "${CYAN}[5/5] Runner Service Configuration${NC}"

if [ -n "$RUNNER_SERVICE" ]; then
    SERVICE_FILE="/etc/systemd/system/$RUNNER_SERVICE"
    
    if [ -f "$SERVICE_FILE" ]; then
        if grep -q "Environment=.*PATH.*\.dotnet" "$SERVICE_FILE"; then
            echo -e "      ${GREEN}? Service already configured${NC}"
            skipped=$((skipped + 1))
        else
            echo "      Updating service..."
            cp "$SERVICE_FILE" "$SERVICE_FILE.backup"
            sed -i '/^\[Service\]/a Environment="PATH=/home/'$RUNNER_USER'/.dotnet:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"' "$SERVICE_FILE"
            
            systemctl daemon-reload
            systemctl restart "$RUNNER_SERVICE"
            sleep 2
            
            if systemctl is-active --quiet "$RUNNER_SERVICE"; then
                echo -e "      ${GREEN}? Service updated and restarted${NC}"
                installed=$((installed + 1))
            else
                echo -e "      ${RED}? Service restart failed${NC}"
                echo "      Check: systemctl status $RUNNER_SERVICE"
            fi
        fi
    else
        echo -e "      ${YELLOW}? Service file not found: $SERVICE_FILE${NC}"
        echo "      .NET will be available via shell PATH only"
    fi
else
    echo -e "      ${YELLOW}? Could not detect runner service${NC}"
fi

# =============================================================================
# SUMMARY
# =============================================================================
echo ""
echo -e "${CYAN}???????????????????????????????????????????????????????????????${NC}"
echo -e "${GREEN}? SETUP COMPLETE${NC}"
echo -e "${CYAN}???????????????????????????????????????????????????????????????${NC}"
echo ""
echo "  Installed:       $installed"
echo "  Already present: $skipped"
echo ""
echo "Runner: $RUNNER_USER"
echo ".NET:   $RUNNER_HOME/.dotnet"
echo ""
echo -e "${CYAN}Verify:${NC}"
echo "  sudo -u $RUNNER_USER $RUNNER_HOME/.dotnet/dotnet --version"
echo ""

exit 0
