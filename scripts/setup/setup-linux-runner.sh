#!/bin/bash
set -e

echo ""
echo "=== Hartonomous Linux Runner Setup ==="
echo "Target: hart-server (Linux)"
echo "This script will automatically install missing prerequisites"
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
        
        wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
        chmod +x /tmp/dotnet-install.sh
        /tmp/dotnet-install.sh --channel 10.0 --install-dir $HOME/.dotnet
        
        # Add to PATH for current session
        export PATH="$HOME/.dotnet:$PATH"
        
        # Add to .bashrc if not already there
        if ! grep -q 'export PATH="$HOME/.dotnet:$PATH"' ~/.bashrc; then
            echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc
        fi
        
        dotnet_version=$(dotnet --version)
        echo "      ? Installed .NET $dotnet_version"
        checks+=(".NET SDK|?|$dotnet_version (installed)")
        installed_count=$((installed_count + 1))
    fi
else
    echo " ? Not installed"
    echo "      Installing .NET 10..."
    
    wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel 10.0 --install-dir $HOME/.dotnet
    
    # Add to PATH for current session
    export PATH="$HOME/.dotnet:$PATH"
    
    # Add to .bashrc if not already there
    if ! grep -q 'export PATH="$HOME/.dotnet:$PATH"' ~/.bashrc; then
        echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc
    fi
    
    dotnet_version=$(dotnet --version)
    echo "      ? Installed .NET $dotnet_version"
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
            # Ubuntu installation
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
    
    # Check if current user can run docker
    if docker ps &> /dev/null; then
        echo " ? $docker_version (already installed, permissions OK)"
        checks+=("Docker|?|$docker_version")
    else
        echo " ?? $docker_version (fixing permissions...)"
        echo "      Adding $USER to docker group..."
        
        sudo usermod -aG docker $USER
        
        echo "      ? User added to docker group"
        echo "      ?? You must logout/login or run: newgrp docker"
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
    
    # Add user to docker group
    sudo usermod -aG docker $USER
    
    docker_version=$(docker --version | cut -d' ' -f3 | tr -d ',')
    echo "      ? Installed Docker $docker_version"
    echo "      ?? You must logout/login or run: newgrp docker"
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

if [ $fail_count -eq 0 ] && [ $warn_count -eq 0 ]; then
    echo "? ALL PREREQUISITES INSTALLED - Runner is ready!"
    exit 0
elif [ $fail_count -eq 0 ]; then
    echo "?? SETUP COMPLETE with $warn_count warnings"
    echo "   Some components require manual configuration (see above)"
    exit 0
else
    echo "? INSTALLATION FAILED - $fail_count critical issues"
    exit 1
fi
