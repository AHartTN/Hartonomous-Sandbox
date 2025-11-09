# docs/AZURE_ARC_MANAGED_IDENTITY.md

## Purpose and Context

- Guides configuration of Azure Arc–enabled SQL Server with system-assigned managed identity for the four Hartonomous services.
- Documents managed identity connection strings, Arc onboarding steps, database permission grants, and operational validation procedures.

## Key Configuration Steps

- Establish Azure Arc connection on on-prem SQL Server via Azure Connected Machine agent install, identity assignment, and verification commands.
- Configure Azure SQL database user mapped from Arc managed identity with `db_owner` (or custom role) and optional Key Vault access policies.
- Ensure SQL Server service account has permissions to read Arc token files (`Hybrid agent extension applications` group, ACL on token folder) and restart service.
- Provide appsettings Production connection strings per service specifying managed identity authentication and pool sizing.

## Verification and Troubleshooting

- Includes PowerShell snippets to acquire tokens, test SQL connections, and query active sessions authenticating as the managed identity.
- Addresses common errors (anonymous logon, expired token, missing database access) with remediation steps.
- Lists security best practices for least privilege roles, firewall restrictions, and audit logging.

## Potential Risks / Follow-ups

- Ensure instructions match current Azure CLI/PowerShell syntax and Arc agent paths; tooling updates may require adjustments.
- Token folder permission changes should be coordinated with security policies to avoid exposing credentials.
- Revisit pool size recommendations based on real workload metrics once services are live.
