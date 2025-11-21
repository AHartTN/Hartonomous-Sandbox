#!/bin/bash
set -e

echo ""
echo "=== Hartonomous Linux Runner Setup ==="
echo "Target: hart-server (Linux)"
echo "This script will automatically install missing prerequisites"
echo ""

# Detect the user running the GitHub Actions runner service
RUNNER_USER=$(ps aux | grep -E 'actions.runner.*Runner.Listener' | grep -v grep | awk '{print $1}' | head -1)

if [ -z "$RUNNER_USER" ]; then
    echo "?? Warning: Could not detect GitHub Actions runner user"
    echo "   Will install for current user: $USER"
    RUNNER_USER=$USER
else
    echo "? Detected GitHub Actions runner user: $RUNNER_USER"
fi

RUNNER_HOME=$(eval echo "~$RUNNER_USER")
echo "? Runner home directory: $RUNNER_HOME"
echo ""

declare -a checks=()
fail_count=0
warn_count=0
installed_count=0

# Check 1: .NET SDK
echo -n "[1/5] Checking .NET SDK..."
if command -v dotnet &> /dev/null; then
    dotnet_version=$(dotnet --version)
    major_version=$(echo "$dotnet_version" | cut -d. -f1)
    
    if [ "$major_version" -ge 10 ]; then
        echo " ? $dotnet_version (already installed)"
        checks+=(".NET SDK|?|$dotnet_version")
    else
        echo " ?? Version $dotnet_version is too old (need 10.0+)"
        echo "      Upgrading to .NET 10..."
        
        # Install for runner user
        sudo -u $RUNNER_USER bash -c "wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install-runner.sh && chmod +x /tmp/dotnet-install-runner.sh && /tmp/dotnet-install-runner.sh --channel 10.0 --install-dir $RUNNER_HOME/.dotnet"
        
        # Add to runner user's .bashrc
        if ! sudo -u $RUNNER_USER grep -q 'export PATH="$HOME/.dotnet:$PATH"' $RUNNER_HOME/.bashrc; then
            sudo -u $RUNNER_USER bash -c "echo 'export PATH=\"\$HOME/.dotnet:\$PATH\"' >> $RUNNER_HOME/.bashrc"
        fi
        
        # Add to runner user's .profile (for systemd service)
        if ! sudo -u $RUNNER_USER grep -q 'export PATH="$HOME/.dotnet:$PATH"' $RUNNER_HOME/.profile 2>/dev/null; then
            sudo -u $RUNNER_USER bash -c "echo 'export PATH=\"\$HOME/.dotnet:\$PATH\"' >> $RUNNER_HOME/.profile"
        fi
        
        dotnet_version=$($RUNNER_HOME/.dotnet/dotnet --version)
        echo "      ? Installed .NET $dotnet_version for user $RUNNER_USER"
        checks+=(".NET SDK|?|$dotnet_version (installed)")
        installed_count=$((installed_count + 1))
    fi
else
    echo " ? Not installed"
    echo "      Installing .NET 10 for runner user: $RUNNER_USER..."
    
    # Install for runner user
    sudo -u $RUNNER_USER bash -c "wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install-runner.sh && chmod +x /tmp/dotnet-install-runner.sh && /tmp/dotnet-install-runner.sh --channel 10.0 --install-dir $RUNNER_HOME/.dotnet"
    
    # Add to runner user's .bashrc
    if ! sudo -u $RUNNER_USER grep -q 'export PATH="$HOME/.dotnet:$PATH"' $RUNNER_HOME/.bashrc; then
        sudo -u $RUNNER_USER bash -c "echo 'export PATH=\"\$HOME/.dotnet:\$PATH\"' >> $RUNNER_HOME/.bashrc"
    fi
    
    # Add to runner user's .profile (for systemd service)
    if ! sudo -u $RUNNER_USER grep -q 'export PATH="$HOME/.dotnet:$PATH"' $RUNNER_HOME/.profile 2>/dev/null; then
        sudo -u $RUNNER_USER bash -c "echo 'export PATH=\"\$HOME/.dotnet:\$PATH\"' >> $RUNNER_HOME/.profile"
    fi
    
    # Restart runner service to pick up new PATH
    echo "      Restarting GitHub Actions runner service..."
    if systemctl --user is-active --quiet actions.runner.* 2>/dev/null; then
        sudo -u $RUNNER_USER systemctl --user restart actions.runner.* || true
    elif systemctl is-active --quiet actions.runner.* 2>/dev/null; then
        sudo systemctl restart actions.runner.* || true
    fi
    
    dotnet_version=$($RUNNER_HOME/.dotnet/dotnet --version)
    echo "      ? Installed .NET $dotnet_version for user $RUNNER_USER"
    checks+=(".NET SDK|?|$dotnet_version (installed)")
    installed_count=$((installed_count + 1))
fi

# Check 2: PowerShell
echo -n "[2/5] Checking PowerShell..."
if command -v pwsh &> /dev/null; then
    pwsh_version=$(pwsh --version | grep -oP '\d+\.\d+\.\d+' || echo "unknown")
    echo " ? $pwsh_version (already installed)"
    checks+=("PowerShell|?|$pwsh_version")
else
    echo " ? Not installed"
    echo "      Installing PowerShell 7..."
    
    # Detect OS
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        
        if [[ "$ID" == "ubuntu" ]]; then
            # Ubuntu installation (system-wide)
            wget -q https://packages.microsoft.com/config/ubuntu/$VERSION_ID/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
            sudo dpkg -i /tmp/packages-microsoft-prod.deb
            sudo apt-get update > /dev/null 2>&1
            sudo apt-get install -y powershell > /dev/null 2>&1
            
            pwsh_version=$(pwsh --version | grep -oP '\d+\.\d+\.\d+' || echo "installed")
            echo "      ? Installed PowerShell $pwsh_version"
            checks+=("PowerShell|?|$pwsh_version (installed)")
            installed_count=$((installed_count + 1))
        else
            echo "      ?? Manual installation required for $ID"
            echo "      See: https://aka.ms/powershell-release?tag=stable"
            checks+=("PowerShell|??|Manual install needed")
            warn_count=$((warn_count + 1))
        fi
    else
        echo "      ?? Cannot detect OS, manual installation required"
        checks+=("PowerShell|??|Manual install needed")
        warn_count=$((warn_count + 1))
    fi
fi

# Check 3: Git
echo -n "[3/5] Checking Git..."
if command -v git &> /dev/null; then
    git_version=$(git --version | cut -d' ' -f3)
    echo " ? $git_version (already installed)"
    checks+=("Git|?|$git_version")
else
    echo " ? Not installed"
    echo "      Installing Git..."
    
    sudo apt-get update > /dev/null 2>&1
    sudo apt-get install -y git > /dev/null 2>&1
    
    git_version=$(git --version | cut -d' ' -f3)
    echo "      ? Installed Git $git_version"
    checks+=("Git|?|$git_version (installed)")
    installed_count=$((installed_count + 1))
fi

# Check 4: Docker
echo -n "[4/5] Checking Docker..."
if command -v docker &> /dev/null; then
    docker_version=$(docker --version | cut -d' ' -f3 | tr -d ',')
    
    # Check if runner user can run docker
    if sudo -u $RUNNER_USER docker ps &> /dev/null; then
        echo " ? $docker_version (already installed, permissions OK)"
        checks+=("Docker|?|$docker_version")
    else
        echo " ?? $docker_version (fixing permissions...)"
        echo "      Adding $RUNNER_USER to docker group..."
        
        sudo usermod -aG docker $RUNNER_USER
        
        echo "      ? User $RUNNER_USER added to docker group"
        echo "      ?? Runner service will be restarted to apply group changes"
        
        # Restart runner service to pick up group membership
        if systemctl --user is-active --quiet actions.runner.* 2>/dev/null; then
            sudo -u $RUNNER_USER systemctl --user restart actions.runner.* || true
        elif systemctl is-active --quiet actions.runner.* 2>/dev/null; then
            sudo systemctl restart actions.runner.* || true
        fi
        
        checks+=("Docker|?|$docker_version (permissions fixed)")
        installed_count=$((installed_count + 1))
    fi
else
    echo " ? Not installed"
    echo "      Installing Docker..."
    
    sudo apt-get update > /dev/null 2>&1
    sudo apt-get install -y docker.io > /dev/null 2>&1
    sudo systemctl enable docker > /dev/null 2>&1
    sudo systemctl start docker > /dev/null 2>&1
    
    # Add runner user to docker group
    sudo usermod -aG docker $RUNNER_USER
    
    docker_version=$(docker --version | cut -d' ' -f3 | tr -d ',')
    echo "      ? Installed Docker $docker_version"
    echo "      ? User $RUNNER_USER added to docker group"
    
    # Restart runner service to pick up group membership
    if systemctl --user is-active --quiet actions.runner.* 2>/dev/null; then
        sudo -u $RUNNER_USER systemctl --user restart actions.runner.* || true
    elif systemctl is-active --quiet actions.runner.* 2>/dev/null; then
        sudo systemctl restart actions.runner.* || true
    fi
    
    checks+=("Docker|?|$docker_version (installed)")
    installed_count=$((installed_count + 1))
fi

# Check 5: GitHub Actions Runner
echo -n "[5/5] Checking GitHub Actions Runner..."
if systemctl --user is-active --quiet actions.runner.* 2>/dev/null || systemctl is-active --quiet actions.runner.* 2>/dev/null; then
    echo " ? Running"
    checks+=("GitHub Runner|?|Running")
else
    echo " ?? Not running or not installed"
    echo "      ?? Runner requires manual setup with GitHub token"
    echo "      See: docs/ci-cd/RUNNER-SETUP.md"
    checks+=("GitHub Runner|??|Manual setup needed")
    warn_count=$((warn_count + 1))
fi

# Summary
echo ""
echo "=== Summary ==="
printf "%-20s %-10s %-30s\n" "Name" "Status" "Version"
printf "%-20s %-10s %-30s\n" "----" "------" "-------"
for check in "${checks[@]}"; do
    IFS='|' read -r name status version <<< "$check"
    printf "%-20s %-10s %-30s\n" "$name" "$status" "$version"
done

echo ""
echo "=== Installation Summary ==="
echo "  ? Already installed: $((5 - installed_count - fail_count - warn_count)) components"
echo "  ? Newly installed: $installed_count components"
if [ $warn_count -gt 0 ]; then
    echo "  ?? Manual setup needed: $warn_count components"
fi
echo ""
echo "Runner user: $RUNNER_USER"
echo "Runner home: $RUNNER_HOME"
echo ""

if [ $fail_count -eq 0 ] && [ $warn_count -eq 0 ]; then
    echo "? ALL PREREQUISITES INSTALLED - Runner is ready!"
    echo ""
    echo "?? GitHub Actions runner service has been restarted to apply changes"
    exit 0
elif [ $fail_count -eq 0 ]; then
    echo "?? SETUP COMPLETE with $warn_count warnings"
    echo "   Some components require manual configuration (see above)"
    echo ""
    echo "?? GitHub Actions runner service has been restarted to apply changes"
    exit 0
else
    echo "? INSTALLATION FAILED - $fail_count critical issues"
    exit 1
fi
