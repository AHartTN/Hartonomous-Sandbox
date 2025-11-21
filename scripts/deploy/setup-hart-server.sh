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

# Install Azure CLI
echo "Installing Azure CLI..."
if ! command -v az &> /dev/null; then
    curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
    echo "✓ Azure CLI installed"
else
    echo "✓ Azure CLI already installed: $(az version --query '\"azure-cli\"' -o tsv)"
fi
echo ""

# Install Azure Arc agent
echo "Installing Azure Arc Connected Machine agent..."
if [ ! -f "/opt/azcmagent/bin/azcmagent" ]; then
    # Download and install Arc agent for Linux
    wget https://aka.ms/azcmagent -O /tmp/install_linux_azcmagent.sh
    sudo bash /tmp/install_linux_azcmagent.sh
    rm /tmp/install_linux_azcmagent.sh
    echo "✓ Azure Arc agent installed"
else
    echo "✓ Azure Arc agent already installed"
fi
echo ""

# Configure Azure Arc agent (requires Azure credentials)
echo "========================================"
echo "  Azure Arc Configuration"
echo "========================================"
echo ""
echo "To connect this server to Azure Arc, you will need:"
echo "  - Azure Subscription ID"
echo "  - Resource Group name (e.g., hartonomous-rg)"
echo "  - Tenant ID"
echo "  - Service Principal credentials OR interactive login"
echo ""
read -p "Do you want to configure Azure Arc now? (y/n): " configure_arc

if [ "$configure_arc" = "y" ] || [ "$configure_arc" = "Y" ]; then
    read -p "Enter Azure Subscription ID: " subscription_id
    read -p "Enter Resource Group name: " resource_group
    read -p "Enter Tenant ID: " tenant_id
    read -p "Enter Azure Location (e.g., eastus): " location
    read -p "Enter machine name (default: hart-server): " machine_name
    machine_name=${machine_name:-hart-server}
    
    echo ""
    echo "Connecting to Azure Arc..."
    sudo /opt/azcmagent/bin/azcmagent connect \
        --resource-group "$resource_group" \
        --tenant-id "$tenant_id" \
        --location "$location" \
        --subscription-id "$subscription_id" \
        --resource-name "$machine_name" \
        --cloud "AzureCloud"
    
    echo "✓ Azure Arc agent connected"
    echo ""
    
    # Enable system-assigned managed identity
    echo "Enabling system-assigned managed identity..."
    az connectedmachine identity assign \
        --resource-group "$resource_group" \
        --name "$machine_name"
    
    echo "✓ Managed Identity enabled"
    echo ""
    
    # Get principal ID for role assignments
    principal_id=$(az connectedmachine show \
        --resource-group "$resource_group" \
        --name "$machine_name" \
        --query identity.principalId -o tsv)
    
    echo "========================================"
    echo "  Managed Identity Principal ID"
    echo "========================================"
    echo "$principal_id"
    echo ""
    echo "Use this Principal ID to assign Azure role permissions."
    echo ""
    
    # Optional: Assign Azure roles
    echo "Recommended Azure role assignments for Hartonomous:"
    echo "  1. Storage Blob Data Contributor (for Azure Storage)"
    echo "  2. Key Vault Secrets User (for Key Vault secrets)"
    echo ""
    read -p "Do you want to assign these roles now? (y/n): " assign_roles
    
    if [ "$assign_roles" = "y" ] || [ "$assign_roles" = "Y" ]; then
        read -p "Enter Storage Account name: " storage_account
        read -p "Enter Key Vault name: " key_vault
        
        # Assign Storage Blob Data Contributor
        echo "Assigning Storage Blob Data Contributor role..."
        storage_id=$(az storage account show \
            --name "$storage_account" \
            --resource-group "$resource_group" \
            --query id -o tsv)
        
        az role assignment create \
            --assignee "$principal_id" \
            --role "Storage Blob Data Contributor" \
            --scope "$storage_id"
        
        echo "✓ Storage Blob Data Contributor role assigned"
        
        # Assign Key Vault Secrets User
        echo "Assigning Key Vault Secrets User role..."
        az keyvault set-policy \
            --name "$key_vault" \
            --object-id "$principal_id" \
            --secret-permissions get list
        
        echo "✓ Key Vault Secrets User permissions granted"
        echo ""
    fi
    
    # Install Azure Arc extensions
    echo "========================================"
    echo "  Azure Arc Extensions"
    echo "========================================"
    echo ""
    echo "Available extensions:"
    echo "  1. AzureMonitorLinuxAgent (recommended)"
    echo "  2. Microsoft.Azure.Security.Monitoring (Defender for Cloud)"
    echo ""
    read -p "Install AzureMonitorLinuxAgent extension? (y/n): " install_monitor
    
    if [ "$install_monitor" = "y" ] || [ "$install_monitor" = "Y" ]; then
        echo "Installing AzureMonitorLinuxAgent extension..."
        az connectedmachine extension create \
            --machine-name "$machine_name" \
            --resource-group "$resource_group" \
            --name AzureMonitorLinuxAgent \
            --publisher Microsoft.Azure.Monitor \
            --type AzureMonitorLinuxAgent \
            --location "$location" \
            --enable-auto-upgrade true
        
        echo "✓ AzureMonitorLinuxAgent extension installed"
    fi
    
    read -p "Install Microsoft Defender for Cloud extension? (y/n): " install_defender
    
    if [ "$install_defender" = "y" ] || [ "$install_defender" = "Y" ]; then
        echo "Installing Defender for Cloud extension..."
        az connectedmachine extension create \
            --machine-name "$machine_name" \
            --resource-group "$resource_group" \
            --name MDE.Linux \
            --publisher Microsoft.Azure.AzureDefenderForServers \
            --type MDE.Linux \
            --location "$location" \
            --enable-auto-upgrade true
        
        echo "✓ Defender for Cloud extension installed"
    fi
    echo ""
else
    echo "⚠ Azure Arc configuration skipped"
    echo "Run this script again or manually configure Arc agent with:"
    echo "  sudo /opt/azcmagent/bin/azcmagent connect --resource-group <rg> --tenant-id <tenant> --location <location> --subscription-id <sub>"
    echo ""
fi

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
echo "  1. Verify Azure Arc connection: sudo /opt/azcmagent/bin/azcmagent show"
echo "  2. Run deploy-to-hart-server.ps1 from your dev machine"
echo "  3. Verify services with: sudo systemctl status hartonomous-*"
echo ""
