#!/bin/bash
set -e

echo ""
echo "=== Hartonomous Linux Runner Setup ==="
echo "Target: hart-server (Linux)"
echo ""

declare -a checks=()
fail_count=0
warn_count=0

# Check 1: .NET SDK
echo -n "[1/5] Checking .NET SDK..."
if command -v dotnet &> /dev/null; then
    dotnet_version=$(dotnet --version)
    major_version=$(echo "$dotnet_version" | cut -d. -f1)
    
    if [ "$major_version" -ge 10 ]; then
        echo " ? $dotnet_version"
        checks+=(".NET SDK|?|$dotnet_version")
    else
        echo " ? Version $dotnet_version is too old (need 10.0+)"
        echo "      Install: wget https://dot.net/v1/dotnet-install.sh && bash dotnet-install.sh --channel 10.0"
        checks+=(".NET SDK|?|$dotnet_version")
        fail_count=$((fail_count + 1))
    fi
else
    echo " ? Not installed"
    echo "      Install: wget https://dot.net/v1/dotnet-install.sh && bash dotnet-install.sh --channel 10.0"
    echo "               export PATH=\"\$HOME/.dotnet:\$PATH\""
    echo "               echo 'export PATH=\"\$HOME/.dotnet:\$PATH\"' >> ~/.bashrc"
    checks+=(".NET SDK|?|Not found")
    fail_count=$((fail_count + 1))
fi

# Check 2: PowerShell
echo -n "[2/5] Checking PowerShell..."
if command -v pwsh &> /dev/null; then
    pwsh_version=$(pwsh --version | grep -oP '\d+\.\d+\.\d+' || echo "unknown")
    echo " ? $pwsh_version"
    checks+=("PowerShell|?|$pwsh_version")
else
    echo " ? Not installed"
    echo "      Install: https://aka.ms/powershell-release?tag=stable"
    echo "      Ubuntu: wget https://github.com/PowerShell/PowerShell/releases/download/v7.4.0/powershell_7.4.0-1.deb_amd64.deb"
    echo "              sudo dpkg -i powershell_7.4.0-1.deb_amd64.deb"
    echo "              sudo apt-get install -f"
    checks+=("PowerShell|?|Not found")
    fail_count=$((fail_count + 1))
fi

# Check 3: Git
echo -n "[3/5] Checking Git..."
if command -v git &> /dev/null; then
    git_version=$(git --version | cut -d' ' -f3)
    echo " ? $git_version"
    checks+=("Git|?|$git_version")
else
    echo " ? Not installed"
    echo "      Install: sudo apt install git"
    checks+=("Git|?|Not found")
    fail_count=$((fail_count + 1))
fi

# Check 4: Docker
echo -n "[4/5] Checking Docker..."
if command -v docker &> /dev/null; then
    docker_version=$(docker --version | cut -d' ' -f3 | tr -d ',')
    # Check if current user can run docker
    if docker ps &> /dev/null; then
        echo " ? $docker_version (permissions OK)"
        checks+=("Docker|?|$docker_version")
    else
        echo " ?? $docker_version (user needs docker group)"
        echo "      Fix: sudo usermod -aG docker \$USER"
        echo "           newgrp docker  # Or logout/login"
        checks+=("Docker|??|No permissions")
        warn_count=$((warn_count + 1))
    fi
else
    echo " ?? Not installed (optional, for test containers)"
    echo "      Install: sudo apt install docker.io"
    echo "               sudo usermod -aG docker \$USER"
    checks+=("Docker|??|Not found")
    warn_count=$((warn_count + 1))
fi

# Check 5: GitHub Actions Runner
echo -n "[5/5] Checking GitHub Actions Runner..."
if systemctl --user is-active --quiet actions.runner.* 2>/dev/null || systemctl is-active --quiet actions.runner.* 2>/dev/null; then
    echo " ? Running"
    checks+=("GitHub Runner|?|Running")
else
    echo " ? Not running or not installed"
    echo "      Install: See docs/ci-cd/RUNNER-SETUP.md"
    checks+=("GitHub Runner|?|Not found")
    fail_count=$((fail_count + 1))
fi

# Summary
echo ""
echo "=== Summary ==="
printf "%-20s %-10s %-20s\n" "Name" "Status" "Version"
printf "%-20s %-10s %-20s\n" "----" "------" "-------"
for check in "${checks[@]}"; do
    IFS='|' read -r name status version <<< "$check"
    printf "%-20s %-10s %-20s\n" "$name" "$status" "$version"
done

echo ""
if [ $fail_count -eq 0 ] && [ $warn_count -eq 0 ]; then
    echo "? ALL CHECKS PASSED - Runner is ready!"
    exit 0
elif [ $fail_count -eq 0 ]; then
    echo "?? WARNINGS FOUND ($warn_count) - Runner will work but some features may be limited"
    exit 0
else
    echo "? CRITICAL ISSUES FOUND ($fail_count) - Please install missing prerequisites"
    exit 1
fi
