#!/bin/bash
#
# Hartonomous Linux Runner Prerequisites Installer
# Automatically installs and configures all prerequisites for GitHub Actions runner
#
# Usage: sudo ./install-linux-runner-prerequisites.sh
#
# This script:
# 1. Detects the GitHub Actions runner user automatically
# 2. Installs .NET 10 SDK to runner user's home directory
# 3. Installs PowerShell 7 system-wide
# 4. Installs Git system-wide
# 5. Installs Docker and configures permissions
# 6. Updates systemd service to include .NET in PATH
# 7. Restarts runner service to apply all changes
#

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo ""
echo -e "${CYAN}???????????????????????????????????????????????????????????????${NC}"
echo -e "${CYAN}  Hartonomous Linux Runner Prerequisites Installer${NC}"
echo -e "${CYAN}???????????????????????????????????????????????????????????????${NC}"
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}? ERROR: This script must be run as root${NC}"
    echo "   Please run: sudo $0"
    exit 1
fi

# Detect the user running the GitHub Actions runner service
echo -e "${CYAN}[DETECT] Finding GitHub Actions runner user...${NC}"
RUNNER_USER=$(ps aux | grep -E 'actions.runner.*Runner.Listener' | grep -v grep | awk '{print $1}' | head -1)

if [ -z "$RUNNER_USER" ]; then
    echo -e "${YELLOW}??  Could not detect GitHub Actions runner user${NC}"
    echo -e "${YELLOW}   Please enter the username that runs the GitHub Actions runner:${NC}"
    read -p "   Username: " RUNNER_USER
    
    if [ -z "$RUNNER_USER" ]; then
        echo -e "${RED}? No username provided. Exiting.${NC}"
        exit 1
    fi
fi

# Verify user exists
if ! id "$RUNNER_USER" &>/dev/null; then
    echo -e "${RED}? ERROR: User '$RUNNER_USER' does not exist${NC}"
    exit 1
fi

RUNNER_HOME=$(eval echo "~$RUNNER_USER")
RUNNER_SERVICE=$(systemctl list-units --type=service --all | grep -i "actions.runner.*$RUNNER_USER" | awk '{print $1}' | head -1)

echo -e "${GREEN}? Runner user: $RUNNER_USER${NC}"
echo -e "${GREEN}? Runner home: $RUNNER_HOME${NC}"
echo -e "${GREEN}? Runner service: $RUNNER_SERVICE${NC}"
echo ""

# Statistics
installed_count=0
already_installed=0
failed_count=0

# ============================================================================
# STEP 1: Install .NET 10 SDK
# ============================================================================
echo -e "${CYAN}[1/5] Installing .NET 10 SDK...${NC}"

if sudo -u $RUNNER_USER bash -c "export PATH=\"$RUNNER_HOME/.dotnet:\$PATH\" && command -v dotnet &> /dev/null && [ \$(dotnet --version | cut -d. -f1) -ge 10 ]"; then
    DOTNET_VERSION=$(sudo -u $RUNNER_USER bash -c "export PATH=\"$RUNNER_HOME/.dotnet:\$PATH\" && dotnet --version")
    echo -e "${GREEN}? .NET $DOTNET_VERSION already installed${NC}"
    already_installed=$((already_installed + 1))
else
    echo "  Downloading and installing .NET 10..."
    
    # Install as runner user
    sudo -u $RUNNER_USER bash -c "
        wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install-$RUNNER_USER.sh
        chmod +x /tmp/dotnet-install-$RUNNER_USER.sh
        /tmp/dotnet-install-$RUNNER_USER.sh --channel 10.0 --install-dir $RUNNER_HOME/.dotnet --no-path
        rm /tmp/dotnet-install-$RUNNER_USER.sh
    "
    
    # Add to .bashrc
    if ! sudo -u $RUNNER_USER grep -q "export PATH=\"\$HOME/.dotnet:\$PATH\"" "$RUNNER_HOME/.bashrc" 2>/dev/null; then
        sudo -u $RUNNER_USER bash -c "echo 'export PATH=\"\$HOME/.dotnet:\$PATH\"' >> $RUNNER_HOME/.bashrc"
    fi
    
    # Add to .profile
    if ! sudo -u $RUNNER_USER grep -q "export PATH=\"\$HOME/.dotnet:\$PATH\"" "$RUNNER_HOME/.profile" 2>/dev/null; then
        sudo -u $RUNNER_USER bash -c "echo 'export PATH=\"\$HOME/.dotnet:\$PATH\"' >> $RUNNER_HOME/.profile"
    fi
    
    DOTNET_VERSION=$(sudo -u $RUNNER_USER bash -c "export PATH=\"$RUNNER_HOME/.dotnet:\$PATH\" && dotnet --version")
    echo -e "${GREEN}? Installed .NET $DOTNET_VERSION${NC}"
    installed_count=$((installed_count + 1))
fi

# ============================================================================
# STEP 2: Install PowerShell 7
# ============================================================================
echo ""
echo -e "${CYAN}[2/5] Installing PowerShell 7...${NC}"

if command -v pwsh &> /dev/null; then
    PWSH_VERSION=$(pwsh --version | grep -oP '\d+\.\d+\.\d+' || echo "unknown")
    echo -e "${GREEN}? PowerShell $PWSH_VERSION already installed${NC}"
    already_installed=$((already_installed + 1))
else
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        
        if [[ "$ID" == "ubuntu" ]]; then
            echo "  Installing PowerShell for Ubuntu $VERSION_ID..."
            wget -q https://packages.microsoft.com/config/ubuntu/$VERSION_ID/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
            dpkg -i /tmp/packages-microsoft-prod.deb
            rm /tmp/packages-microsoft-prod.deb
            apt-get update > /dev/null 2>&1
            apt-get install -y powershell > /dev/null 2>&1
            
            PWSH_VERSION=$(pwsh --version | grep -oP '\d+\.\d+\.\d+' || echo "installed")
            echo -e "${GREEN}? Installed PowerShell $PWSH_VERSION${NC}"
            installed_count=$((installed_count + 1))
        else
            echo -e "${YELLOW}??  Manual installation required for $ID${NC}"
            echo "   See: https://aka.ms/powershell-release?tag=stable"
        fi
    fi
fi

# ============================================================================
# STEP 3: Install Git
# ============================================================================
echo ""
echo -e "${CYAN}[3/5] Installing Git...${NC}"

if command -v git &> /dev/null; then
    GIT_VERSION=$(git --version | cut -d' ' -f3)
    echo -e "${GREEN}? Git $GIT_VERSION already installed${NC}"
    already_installed=$((already_installed + 1))
else
    echo "  Installing Git..."
    apt-get update > /dev/null 2>&1
    apt-get install -y git > /dev/null 2>&1
    
    GIT_VERSION=$(git --version | cut -d' ' -f3)
    echo -e "${GREEN}? Installed Git $GIT_VERSION${NC}"
    installed_count=$((installed_count + 1))
fi

# ============================================================================
# STEP 4: Install Docker
# ============================================================================
echo ""
echo -e "${CYAN}[4/5] Installing Docker...${NC}"

if command -v docker &> /dev/null; then
    DOCKER_VERSION=$(docker --version | cut -d' ' -f3 | tr -d ',')
    
    # Check if runner user can run docker
    if sudo -u $RUNNER_USER docker ps &> /dev/null; then
        echo -e "${GREEN}? Docker $DOCKER_VERSION already installed (permissions OK)${NC}"
        already_installed=$((already_installed + 1))
    else
        echo "  Docker installed but $RUNNER_USER needs permissions..."
        usermod -aG docker $RUNNER_USER
        echo -e "${GREEN}? Added $RUNNER_USER to docker group${NC}"
        installed_count=$((installed_count + 1))
    fi
else
    echo "  Installing Docker..."
    apt-get update > /dev/null 2>&1
    apt-get install -y docker.io > /dev/null 2>&1
    systemctl enable docker > /dev/null 2>&1
    systemctl start docker > /dev/null 2>&1
    
    # Add runner user to docker group
    usermod -aG docker $RUNNER_USER
    
    DOCKER_VERSION=$(docker --version | cut -d' ' -f3 | tr -d ',')
    echo -e "${GREEN}? Installed Docker $DOCKER_VERSION${NC}"
    echo -e "${GREEN}? Added $RUNNER_USER to docker group${NC}"
    installed_count=$((installed_count + 1))
fi

# ============================================================================
# STEP 5: Update Runner Service Environment
# ============================================================================
echo ""
echo -e "${CYAN}[5/5] Configuring GitHub Actions runner service...${NC}"

if [ -n "$RUNNER_SERVICE" ]; then
    # Update systemd service to include .NET in PATH
    SERVICE_FILE="/etc/systemd/system/$RUNNER_SERVICE"
    
    if [ -f "$SERVICE_FILE" ]; then
        echo "  Updating service environment..."
        
        # Check if Environment line with .dotnet already exists
        if grep -q "Environment=.*PATH.*\.dotnet" "$SERVICE_FILE"; then
            echo -e "${GREEN}? Service already configured with .NET in PATH${NC}"
        else
            # Add Environment line after [Service] section
            if grep -q "^\[Service\]" "$SERVICE_FILE"; then
                # Create backup
                cp "$SERVICE_FILE" "$SERVICE_FILE.backup"
                
                # Add PATH with .dotnet after [Service]
                sed -i '/^\[Service\]/a Environment="PATH=/home/'$RUNNER_USER'/.dotnet:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"' "$SERVICE_FILE"
                
                echo -e "${GREEN}? Updated service configuration${NC}"
                installed_count=$((installed_count + 1))
            fi
        fi
        
        # Reload systemd and restart service
        echo "  Reloading systemd and restarting runner..."
        systemctl daemon-reload
        systemctl restart "$RUNNER_SERVICE"
        sleep 2
        
        if systemctl is-active --quiet "$RUNNER_SERVICE"; then
            echo -e "${GREEN}? Runner service restarted successfully${NC}"
        else
            echo -e "${RED}? Warning: Runner service may not have restarted properly${NC}"
            echo "   Check status with: systemctl status $RUNNER_SERVICE"
        fi
    else
        echo -e "${YELLOW}??  Service file not found at: $SERVICE_FILE${NC}"
        echo "   PATH will be available from .bashrc/.profile when runner starts new shells"
    fi
else
    echo -e "${YELLOW}??  Could not detect runner service${NC}"
    echo "   PATH will be available from .bashrc/.profile when runner starts new shells"
fi

# ============================================================================
# SUMMARY
# ============================================================================
echo ""
echo -e "${CYAN}???????????????????????????????????????????????????????????????${NC}"
echo -e "${CYAN}  Installation Summary${NC}"
echo -e "${CYAN}???????????????????????????????????????????????????????????????${NC}"
echo ""
echo -e "${GREEN}? Already installed: $already_installed components${NC}"
echo -e "${GREEN}? Newly installed: $installed_count components${NC}"
if [ $failed_count -gt 0 ]; then
    echo -e "${RED}? Failed: $failed_count components${NC}"
fi
echo ""
echo -e "${CYAN}Configuration:${NC}"
echo "  Runner user:    $RUNNER_USER"
echo "  Runner home:    $RUNNER_HOME"
echo "  .NET location:  $RUNNER_HOME/.dotnet"
echo "  Runner service: $RUNNER_SERVICE"
echo ""

if [ $failed_count -eq 0 ]; then
    echo -e "${GREEN}? ALL PREREQUISITES INSTALLED SUCCESSFULLY!${NC}"
    echo ""
    echo -e "${CYAN}Next steps:${NC}"
    echo "  1. Verify .NET is accessible: sudo -u $RUNNER_USER $RUNNER_HOME/.dotnet/dotnet --version"
    echo "  2. Trigger a GitHub Actions workflow to test the runner"
    echo "  3. Check runner logs: journalctl -u $RUNNER_SERVICE -f"
    echo ""
    exit 0
else
    echo -e "${RED}? INSTALLATION COMPLETED WITH ERRORS${NC}"
    echo "   Please review the errors above"
    echo ""
    exit 1
fi
