#!/bin/bash
# Hartonomous Linux Runner Prerequisites Setup
# IDEMPOTENT - Safe to run multiple times without side effects
# Based on official Microsoft documentation

set -e

if [ "$EUID" -ne 0 ]; then
    echo "ERROR: Must run as root"
    exit 1
fi

echo "=== Hartonomous Linux Runner Setup ==="
echo ""

# .NET 10 SDK
echo -n "[1/4] .NET 10 SDK... "
if command -v dotnet &>/dev/null; then
    VER=$(dotnet --version 2>/dev/null | cut -d. -f1)
    if [ "$VER" -ge 10 ] 2>/dev/null; then
        echo "OK ($(dotnet --version))"
    else
        echo "upgrading from $(dotnet --version)"
        add-apt-repository -y ppa:dotnet/backports >/dev/null 2>&1
        apt-get update >/dev/null 2>&1
        apt-get install -y dotnet-sdk-10.0 >/dev/null 2>&1
        echo "        Installed: $(dotnet --version)"
    fi
else
    echo "installing"
    add-apt-repository -y ppa:dotnet/backports >/dev/null 2>&1
    apt-get update >/dev/null 2>&1
    apt-get install -y dotnet-sdk-10.0 >/dev/null 2>&1
    echo "        Installed: $(dotnet --version)"
fi

# PowerShell
echo -n "[2/4] PowerShell... "
if command -v pwsh &>/dev/null; then
    echo "OK ($(pwsh --version | grep -oP '\d+\.\d+\.\d+'))"
else
    echo "installing"
    apt-get update >/dev/null 2>&1
    apt-get install -y powershell >/dev/null 2>&1
    echo "        Installed: $(pwsh --version | grep -oP '\d+\.\d+\.\d+')"
fi

# Git
echo -n "[3/4] Git... "
if command -v git &>/dev/null; then
    echo "OK ($(git --version | cut -d' ' -f3))"
else
    echo "installing"
    apt-get install -y git >/dev/null 2>&1
    echo "        Installed: $(git --version | cut -d' ' -f3)"
fi

# Docker
echo -n "[4/4] Docker... "
RUNNER_USER=$(ps aux | grep 'Runner.Listener' | grep -v grep | awk '{print $1}' | head -1)
RUNNER_USER=${RUNNER_USER:-ahart}

if command -v docker &>/dev/null; then
    if groups $RUNNER_USER 2>/dev/null | grep -q docker; then
        echo "OK ($(docker --version | cut -d' ' -f3 | tr -d ','))"
    else
        echo "fixing permissions"
        usermod -aG docker $RUNNER_USER
        echo "        User $RUNNER_USER added to docker group"
    fi
else
    echo "installing"
    apt-get install -y docker.io >/dev/null 2>&1
    systemctl enable --now docker >/dev/null 2>&1
    usermod -aG docker $RUNNER_USER
    echo "        Installed: $(docker --version | cut -d' ' -f3 | tr -d ',')"
fi

# Restart runner
RUNNER_SVC=$(systemctl list-units --type=service | grep 'actions.runner' | awk '{print $1}' | head -1)
if [ -n "$RUNNER_SVC" ]; then
    echo ""
    echo "Restarting $RUNNER_SVC..."
    systemctl restart "$RUNNER_SVC"
    sleep 2
    systemctl is-active --quiet "$RUNNER_SVC" && echo "OK" || echo "Failed (check: systemctl status $RUNNER_SVC)"
fi

echo ""
echo "Setup complete. Verify: dotnet --version"
