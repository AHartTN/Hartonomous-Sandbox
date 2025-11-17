-- Create SQL logins for each service principal
CREATE LOGIN [Hartonomous-GitHub-Actions-Production] FROM EXTERNAL PROVIDER;
CREATE USER [Hartonomous-GitHub-Actions-Production] FROM LOGIN [Hartonomous-GitHub-Actions-Production];
ALTER ROLE db_owner ADD MEMBER [Hartonomous-GitHub-Actions-Production];
GRANT VIEW SERVER STATE TO [Hartonomous-GitHub-Actions-Production];
GRANT ALTER ANY DATABASE TO [Hartonomous-GitHub-Actions-Production];

CREATE LOGIN [Hartonomous-GitHub-Actions-Development] FROM EXTERNAL PROVIDER;
CREATE USER [Hartonomous-GitHub-Actions-Development] FROM LOGIN [Hartonomous-GitHub-Actions-Development];
ALTER ROLE db_owner ADD MEMBER [Hartonomous-GitHub-Actions-Development];
GRANT VIEW SERVER STATE TO [Hartonomous-GitHub-Actions-Development];
GRANT ALTER ANY DATABASE TO [Hartonomous-GitHub-Actions-Development];

CREATE LOGIN [Hartonomous-GitHub-Actions-Staging] FROM EXTERNAL PROVIDER;
CREATE USER [Hartonomous-GitHub-Actions-Staging] FROM LOGIN [Hartonomous-GitHub-Actions-Staging];
ALTER ROLE db_owner ADD MEMBER [Hartonomous-GitHub-Actions-Staging];
GRANT VIEW SERVER STATE TO [Hartonomous-GitHub-Actions-Staging];
GRANT ALTER ANY DATABASE TO [Hartonomous-GitHub-Actions-Staging];
