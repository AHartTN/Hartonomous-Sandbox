# GitHub Actions Secrets Configuration

This document describes the GitHub repository secrets required for the CI/CD pipeline.

## Required Secrets

Navigate to: **Settings** → **Secrets and variables** → **Actions** → **New repository secret**

### Arc Server Connection

| Secret Name | Description | Example Value |
|-------------|-------------|---------------|
| `ARC_SERVER_HOST` | Hostname or IP of Azure Arc-enabled SQL Server | `hart-server` or `192.168.1.100` |
| `ARC_SERVER_USER` | SSH username for deployment | `hartonomous` |
| `ARC_SERVER_SSH_KEY` | Private SSH key for authentication | `-----BEGIN OPENSSH PRIVATE KEY-----\n...` |
| `ARC_SERVER_SSH_PORT` | SSH port (optional, defaults to 22) | `22` |

### SQL Server Credentials

| Secret Name | Description | Example Value |
|-------------|-------------|---------------|
| `SQL_DATABASE` | Database name (optional, defaults to "Hartonomous") | `Hartonomous` |
| `SQL_USERNAME` | SQL Server authentication username | `sa` or `hartonomous_deploy` |
| `SQL_PASSWORD` | SQL Server authentication password | `YourSecurePassword123!` |

## Generating SSH Key

If you don't have an SSH key for deployment:

```bash
# On your local machine
ssh-keygen -t ed25519 -C "github-actions-hartonomous" -f ~/.ssh/github-actions-hartonomous

# Copy public key to Arc server
ssh-copy-id -i ~/.ssh/github-actions-hartonomous.pub hartonomous@hart-server

# Get private key content for GitHub secret
cat ~/.ssh/github-actions-hartonomous
```

## GitHub Environments

The workflow uses two environments for approval gates:

### hart-server-database
- **Purpose:** Database deployment approval
- **Protection rules:** Require reviewers before deployment
- **Secrets:** All Arc server and SQL credentials

### hart-server-production
- **Purpose:** Service deployment approval
- **Protection rules:** Require reviewers before deployment
- **Secrets:** All Arc server credentials

## Testing the Workflow

1. Push to a feature branch to trigger build/test only
2. Create a pull request to run build/test on PR
3. Merge to `main` to trigger full deployment (requires approval)

## Manual Workflow Trigger

The workflow can be manually triggered:

1. Go to **Actions** tab
2. Select **CI/CD Pipeline**
3. Click **Run workflow**
4. Select branch and click **Run workflow**

## Troubleshooting

### SSH Connection Failed

```bash
# Test SSH connection from local machine
ssh -i ~/.ssh/github-actions-hartonomous hartonomous@hart-server

# If connection fails, check:
# 1. SSH service is running on Arc server
# 2. Firewall allows port 22
# 3. Public key is in ~/.ssh/authorized_keys on server
```

### SQL Authentication Failed

```bash
# Test SQL connection from Arc server
sqlcmd -S localhost -U hartonomous_deploy -P 'YourPassword' -C -Q "SELECT @@VERSION"

# If fails, check:
# 1. SQL Server allows SQL authentication (not just Windows auth)
# 2. User has db_owner or sysadmin role
# 3. Password is correct and not expired
```

### Permission Denied on /srv/www/hartonomous

```bash
# On Arc server, ensure deployment user owns service directories
sudo chown -R hartonomous:hartonomous /srv/www/hartonomous
sudo chmod -R 755 /srv/www/hartonomous
```

## Security Best Practices

1. **Rotate secrets regularly** - Update SSH keys and SQL passwords quarterly
2. **Use least privilege** - Deployment user should only have necessary permissions
3. **Enable environment protection** - Require manual approval for production deployments
4. **Audit secret access** - Review GitHub Actions logs for unauthorized access attempts
5. **Use separate accounts** - Don't use personal accounts for deployment automation
