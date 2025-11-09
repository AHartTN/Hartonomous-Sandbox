# deploy/deploy-to-hart-server.ps1

## Purpose and Context

- PowerShell deployment script targeting `HART-SERVER` over SSH to publish and manage Hartonomous services.
- Automates build, file transfer, systemd user service installation, and service lifecycle operations.

## Workflow Summary

1. Optionally builds/publishes API, CesConsumer, Neo4jSync, and ModelIngestion projects into local `publish/` folders (assumes project presence).
2. Validates remote `.NET` installation via `ssh`; provides manual install guidance if missing.
3. Stops existing user-level systemd services on the remote host.
4. Copies published binaries to `/srv/www/hartonomous/{service}` using `scp`.
5. Installs systemd service descriptors from `deploy/*.service`, reloads user daemon, enables and starts services.
6. Displays service status and outputs access/logging instructions.

## Notable Details

- Hard-coded remote user/host (`ahart@192.168.1.2`) and directory paths; script assumes passwordless SSH or prior key setup.
- Relies on `ModelIngestion` project despite repository state indicating it was deleted; running against current tree will fail.
- No error handling for failed `scp`/`systemctl` commands beyond PowerShell's default; partial deployments may leave the system inconsistent.

## Potential Risks / Follow-ups

- Update to reflect current service layout (e.g., consolidated worker project) and remove references to deleted projects.
- Parameterize remote server, paths, and port to support multiple environments.
- Add validation that local publish outputs exist before copying; consider cleaning or archiving old releases.
- Implement rollback or safe deployment practices (e.g., stop on first failure, restore previous versions).
