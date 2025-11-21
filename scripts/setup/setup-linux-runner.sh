#!/bin/bash
# GitHub Actions Runner Prerequisites
# Minimal setup script for self-hosted Linux runners
# Based on Microsoft best practices documentation

set -euo pipefail

if [ "$EUID" -ne 0 ]; then
    echo "ERROR: Must run as root"
    exit 1
fi

echo "=== GitHub Actions Runner Prerequisites Setup ==="
echo ""

# Detect runner service
RUNNER_SVC=$(systemctl list-units 'actions.runner*' --no-legend --full | awk '{print $1}' | head -1)

if [ -z "$RUNNER_SVC" ]; then
    echo "ERROR: No GitHub Actions runner service found"
    echo "Install the runner first: https://docs.github.com/en/actions/hosting-your-own-runners"
    exit 1
fi

# Get runner user
RUNNER_USER=$(systemctl show "$RUNNER_SVC" -p User --value)

if [ -z "$RUNNER_USER" ] || [ "$RUNNER_USER" = "root" ]; then
    echo "ERROR: Runner must not run as root"
    exit 1
fi

# Get runner home directory
RUNNER_HOME=$(getent passwd "$RUNNER_USER" | cut -d: -f6)

if [ -z "$RUNNER_HOME" ] || [ ! -d "$RUNNER_HOME" ]; then
    echo "ERROR: Runner user home directory not found"
    exit 1
fi

echo "Runner service: $RUNNER_SVC"
echo "Runner user: $RUNNER_USER"
echo "Runner home: $RUNNER_HOME"
echo ""

# Create tool cache directory with correct permissions
TOOL_CACHE="$RUNNER_HOME/actions-tool-cache"
if [ ! -d "$TOOL_CACHE" ]; then
    echo "Creating tool cache directory..."
    mkdir -p "$TOOL_CACHE"
    chown -R "$RUNNER_USER:$RUNNER_USER" "$TOOL_CACHE"
    chmod 755 "$TOOL_CACHE"
    echo "  Created: $TOOL_CACHE"
fi

# Git
echo -n "[1/2] Git... "
if command -v git &>/dev/null; then
    echo "OK ($(git --version | cut -d' ' -f3))"
else
    echo "installing"
    apt-get update -qq
    apt-get install -y git >/dev/null 2>&1
    echo "        Installed: $(git --version | cut -d' ' -f3)"
fi

# Docker + permissions
echo -n "[2/2] Docker... "
if command -v docker &>/dev/null; then
    VER=$(docker --version | cut -d' ' -f3 | tr -d ',')
    
    # Test if user can actually run docker commands
    if sudo -u "$RUNNER_USER" docker ps &>/dev/null; then
        echo "OK ($VER, permissions OK)"
    else
        echo "fixing permissions"
        usermod -aG docker "$RUNNER_USER"
        echo "        User $RUNNER_USER added to docker group"
    fi
else
    echo "installing"
    apt-get update -qq
    apt-get install -y docker.io >/dev/null 2>&1
    systemctl enable --now docker >/dev/null 2>&1
    usermod -aG docker "$RUNNER_USER"
    VER=$(docker --version | cut -d' ' -f3 | tr -d ',')
    echo "        Installed: $VER"
fi

# Restart runner service to apply group changes
echo ""
echo "Restarting $RUNNER_SVC..."
systemctl restart "$RUNNER_SVC"
sleep 2

if systemctl is-active --quiet "$RUNNER_SVC"; then
    echo "OK"
else
    echo "FAILED - check: systemctl status $RUNNER_SVC"
    exit 1
fi

echo ""
echo "=== Setup Complete ==="
echo ""
echo "IMPORTANT: .NET SDK Installation"
echo "  This script does NOT install .NET"
echo "  Add this to your GitHub Actions workflow:"
echo ""
echo "    - name: Setup .NET SDK"
echo "      uses: actions/setup-dotnet@v4"
echo "      with:"
echo "        dotnet-version: '10.x'"
echo ""
echo "  This is the Microsoft-recommended approach."
echo "  It automatically manages the tool cache at:"
echo "  $TOOL_CACHE"
echo ""
