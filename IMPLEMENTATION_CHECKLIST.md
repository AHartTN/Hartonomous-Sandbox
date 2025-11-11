# IMPLEMENTATION CHECKLIST

**Generated**: November 11, 2025 (Updated: Database-First Architecture)  
**Purpose**: Complete technical checklist to achieve stable production state  
**Architecture**: Database-First - SQL Server Database Project as single source of truth, EF Core as ORM only, thin client applications  
**Source**: Consolidated from all audit documents and architecture planning

---

## Architecture Pivot: Database-First Strategy

**Key Principles**:

- SQL Server Database Project (`Hartonomous.Database.sqlproj`) owns ALL schema (tables, procedures, functions, views, Service Broker, CLR)
- EF Core reverse-engineered from deployed database (database-first scaffolding)
- No EF Core migrations - all database changes via SQL scripts in SQL project
- Applications are thin clients using EF Core as ORM for data access only
- CLR assemblies for vector operations, FILESTREAM management, complex computations

---

## Database Schema Consolidation (SQL Project as Source of Truth)

1. Delete entire `src/Hartonomous.Data/Migrations/` folder (no longer using code-first migrations)
2. Delete entire `src/Hartonomous.Data/Configurations/` folder (40 files - replaced by scaffolded configurations)
3. Delete `sql/ef-core/Hartonomous.Schema.sql` (8,136-line redundant EF Core schema dump)
4. Move ALL table scripts from `sql/tables/` into `Hartonomous.Database.sqlproj` as `<Build Include="Tables\*.sql" />`
5. Fix DACPAC build errors in excluded table scripts (remove `IF EXISTS`, `DROP TABLE`, `GO` separators, use declarative CREATE syntax)
6. Split `sql/procedures/` multi-procedure files into one file per procedure for DACPAC compatibility
7. Move all procedure scripts into `Hartonomous.Database.sqlproj` as `<Build Include="Procedures\*.sql" />`
8. Move all function scripts from `sql/functions/` into `Hartonomous.Database.sqlproj` as `<Build Include="Functions\*.sql" />`
9. Move all view scripts from `sql/views/` into `Hartonomous.Database.sqlproj` as `<Build Include="Views\*.sql" />`
10. Archive `sql/tables/dbo.BillingUsageLedger_InMemory.sql` and `sql/tables/dbo.BillingUsageLedger_Migrate_to_Ledger.sql` to `sql/archive/` (alternative implementations)
11. Remove duplicate `clr_SemanticFeaturesJson` definition in `sql/procedures/Common.ClrBindings.sql`
12. Implement missing `clr_BytesToFloatArrayJson` in `src/SqlClr/Functions.cs` (referenced in `dbo.sp_ExtractStudentModel.sql`)
13. Build `SqlClrFunctions.csproj` to generate updated `SqlClrFunctions.dll`
14. Update `Hartonomous.Database.sqlproj` ArtifactReference to point to `SqlClrFunctions.dll`
15. Create `Hartonomous.Database/Scripts/Pre-Deployment/Script.PreDeployment.sql` (enable FILESTREAM, CLR, Service Broker, create schemas)
16. Create `Hartonomous.Database/Scripts/Post-Deployment/Script.PostDeployment.sql` (deploy CLR assemblies, seed reference data)
17. Build DACPAC: `dotnet build src/Hartonomous.Database/Hartonomous.Database.sqlproj -c Release`
18. Verify DACPAC contains all tables, procedures, functions, views: `SqlPackage /Action:Script /SourceFile:Hartonomous.Database.dacpac`
19. Resolve TODO in `sql/Setup_FILESTREAM.sql`: implement migration strategy for existing Atoms to FILESTREAM
20. Resolve TODO in `sql/Setup_FILESTREAM.sql`: implement actual migration logic
21. Resolve NOTE in `sql/procedures/Inference.VectorSearchSuite.sql`: verify `sp_HybridSearch` moved
22. Resolve TODO in `sql/procedures/Stream.StreamOrchestration.sql`: capture result from dynamic query
23. Resolve NOTE in `sql/procedures/Generation.TextFromVector.sql`: document Stream column removal
24. Resolve TODO in `sql/procedures/dbo.sp_Hypothesize.sql`: implement error region
25. Resolve TODO in `sql/procedures/dbo.sp_Analyze.sql`: compute actual vector
26. Resolve TODO in `sql/procedures/dbo.ModelManagement.sql`: implement Win32 API CLR function for file copy
27. Resolve NOTE in `sql/tables/dbo.InferenceCache.sql`: document CDC conflict preventing MEMORY_OPTIMIZED
28. Resolve TODO in `sql/procedures/Autonomy.SelfImprovement.sql`: implement actual evaluation
29. Resolve TODO in `sql/procedures/Autonomy.SelfImprovement.sql`: implement actual performance metrics

## EF Core Database-First Scaffolding

30. Deploy database to HART-DESKTOP: `SqlPackage /Action:Publish /SourceFile:Hartonomous.Database.dacpac /TargetServerName:localhost /TargetDatabaseName:Hartonomous`
31. Scaffold DbContext from deployed database: `dotnet ef dbcontext scaffold "Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --context HartonomousDbContext --context-dir . --output-dir Entities --data-annotations --force --project src/Hartonomous.Data`
32. Review scaffolded `HartonomousDbContext.cs` - verify all tables represented as DbSet properties
33. Review scaffolded entity classes in `src/Hartonomous.Data/Entities/` - compare with `src/Hartonomous.Core/Entities/`
34. Decision: Keep domain entities in `Hartonomous.Core.Entities` (DDD) OR use scaffolded entities (anemic domain model) - document choice in `src/Hartonomous.Data/README.md`
35. If keeping Core entities: Create AutoMapper profiles to map scaffolded entities ↔ domain entities
36. If using scaffolded entities: Move domain logic from Core entities to domain services in `Hartonomous.Infrastructure`
37. Update all repositories in `src/Hartonomous.Data/Repositories/` to use scaffolded DbContext
38. Remove manual `OnModelCreating` configuration from `HartonomousDbContext.cs` (scaffolded version is source of truth)
39. Test repository queries against deployed database
40. Document scaffolding procedure in `src/Hartonomous.Data/README.md` for future schema changes

## Deployment Script Refactoring (DACPAC-Based)

41. Replace `scripts/deploy-database-unified.ps1` with DACPAC deployment approach
42. Create `scripts/deploy-dacpac.ps1` using SqlPackage for idempotent deployments
43. Configure SqlPackage options: `/Action:Publish`, `/AllowIncompatiblePlatform:True`, `/DropObjectsNotInSource:False`
44. Add pre-deployment validation: check SQL Server version, FILESTREAM enabled, CLR enabled
45. Add post-deployment verification: run smoke tests, verify CLR functions, check Service Broker status
46. Update `deploy/deploy-to-hart-server.ps1` to use DACPAC deployment
47. Document DACPAC deployment procedure in `docs/DEPLOYMENT.md`

## SQL Server Configuration (HART-DESKTOP)

48. Enable FILESTREAM on HART-DESKTOP SQL Server instance
49. Configure FILESTREAM filegroup path (e.g., `D:\SQL_FILESTREAM`)
50. Enable CLR: `EXEC sp_configure 'clr enabled', 1; RECONFIGURE;`
51. Set database TRUSTWORTHY: `ALTER DATABASE Hartonomous SET TRUSTWORTHY ON;` (or implement certificate signing)
52. Deploy database using DACPAC: `SqlPackage /Action:Publish /SourceFile:Hartonomous.Database.dacpac /TargetServerName:localhost /TargetDatabaseName:Hartonomous`
53. Verify CLR assemblies deployed: `SELECT name, permission_set_desc FROM sys.assemblies WHERE is_user_defined = 1;`
54. Test CLR function: `SELECT dbo.clr_VectorDotProduct(@test_vector1, @test_vector2);`
55. Enable Service Broker: `ALTER DATABASE Hartonomous SET ENABLE_BROKER;`
56. Configure Service Broker endpoint on port 4022
57. Grant SQL Server service account Read & Execute permissions on `C:\ProgramData\AzureConnectedMachineAgent\Tokens\` folder
58. Add SQL Server service account to `Hybrid agent extension applications` Windows group
59. Enable managed identity in Azure portal (Settings → Microsoft Entra ID and Purview → Use a primary managed identity)
60. Restart SQL Server service to apply managed identity configuration
61. Verify managed identity: `SELECT * FROM sys.dm_server_external_policy_principals;`

## SQL Server Configuration (HART-SERVER)

62. Create linked server to HART-DESKTOP: `EXEC sp_addlinkedserver @server='HART_DESKTOP_LINK', @datasrc='HART-DESKTOP';`
63. Configure linked server authentication (SQL auth or managed identity if available)
64. Store linked server credentials in Azure Key Vault
65. Test linked server: `SELECT TOP 10 * FROM [HART_DESKTOP_LINK].[Hartonomous].[dbo].[Atoms];`
66. Enable Service Broker: `ALTER DATABASE Hartonomous SET ENABLE_BROKER;` (if separate Hartonomous database)
67. Configure Service Broker endpoint on port 4022
68. Create Service Broker route to HART-DESKTOP: `CREATE ROUTE HartDesktopRoute WITH SERVICE_NAME = 'HartDesktopProcessingService', ADDRESS = 'TCP://HART-DESKTOP:4022';`
69. Enable CDC (Change Data Capture): run `scripts/enable-cdc.sql`
70. Configure CDC for tables to replicate to HART-SERVER (AtomEmbeddings, TensorAtomCoefficients, BillingUsageLedger, InferenceCache)

## Neo4j Configuration

71. Configure Neo4j Desktop on HART-DESKTOP (development environment)
72. Install Neo4j Community on HART-SERVER (production environment)
73. Configure Neo4j authentication (username/password)
74. Store Neo4j credentials in Azure Key Vault
75. Test Neo4j connectivity from HART-SERVER: `cypher-shell -u neo4j -p <password> "MATCH (n) RETURN count(n);"`
76. Create Neo4j database: `CREATE DATABASE hartonomous;`
77. Configure Neo4j connection string in `appsettings.Production.json` (or App Configuration)

## Azure External ID Configuration

78. Create app registration in `hartonomous.onmicrosoft.com` External ID tenant
79. Configure redirect URIs for Hartonomous.Api (e.g., `https://hart-server:5001/signin-oidc`)
80. Add app roles: Admin, DataScientist, User
81. Configure token claims (tenantId, userId, role)
82. Generate client secret for app registration
83. Store client secret in Azure Key Vault
84. Update `appsettings.Production.json` with External ID tenant configuration
85. Create test user accounts for each role (Admin, DataScientist, User)
86. Test authentication flow: obtain JWT token via OAuth2 flow
87. Verify JWT token contains required claims (tenantId, role, userId)

## Azure App Configuration Optimization

88. Monitor App Configuration request count for 7 days using Azure Monitor metrics
89. If usage < 1,000 requests/day, downgrade to Free tier: `az appconfig update --name appconfig-hartonomous --resource-group rg-hartonomous --sku Free`
90. Implement App Configuration local caching with 30-minute refresh interval in `Hartonomous.Infrastructure`
91. Configure `SetCacheExpiration(TimeSpan.FromMinutes(30))` in App Configuration client setup

## Application Deployment (HART-SERVER)

92. Run `deploy/setup-hart-server.sh` to install .NET 10, configure systemd
93. Copy systemd unit files to `/etc/systemd/system/`: `hartonomous-api.service`, `hartonomous-ces-consumer.service`, `hartonomous-neo4j-sync.service`
94. Configure environment variables in systemd unit files (App Configuration endpoint, Key Vault URI)
95. Run `deploy/deploy-to-hart-server.ps1` to copy application binaries
96. Reload systemd daemon: `sudo systemctl daemon-reload`
97. Enable services: `sudo systemctl enable hartonomous-api hartonomous-ces-consumer hartonomous-neo4j-sync`
98. Start services: `sudo systemctl start hartonomous-api hartonomous-ces-consumer hartonomous-neo4j-sync`
99. Verify service status: `sudo systemctl status hartonomous-api`
100. Check logs for errors: `journalctl -u hartonomous-api -f`
101. Test API health endpoint: `curl http://localhost:5000/api/health`
102. Test API with authentication: `curl -H "Authorization: Bearer <token>" http://localhost:5000/api/infer`

## Application Insights Configuration

103. Enable adaptive sampling in `Program.cs`: `options.EnableAdaptiveSampling = true; options.SamplingPercentage = 50;`
104. Verify telemetry is flowing to Application Insights in Azure portal
105. Create custom dashboard for API performance metrics (response time, error rate, throughput)
106. Set up alert rule for API error rate > 5%
107. Set up alert rule for API response time > 1000ms (p95)

## Integration Test Fixes

108. Configure Neo4j connection string in `appsettings.Testing.json`
109. Deploy test database: `SqlPackage /Action:Publish /SourceFile:Hartonomous.Database.dacpac /TargetServerName:localhost /TargetDatabaseName:Hartonomous_Test`
110. Generate test JWT tokens from External ID tenant for each role (Admin, DataScientist, User)
111. Update integration test setup to inject test JWT tokens
112. Run integration tests: `dotnet test tests/Hartonomous.IntegrationTests --filter "Category!=RequiresInfrastructure"`
113. Fix Neo4j integration test failures (3 tests)
114. Fix Inference integration test failures (6 tests)
115. Fix Authentication/Authorization integration test failures (15 tests)
116. Add `[Trait("Category", "RequiresInfrastructure")]` to tests requiring live Azure/Neo4j/SQL connections
117. Target: 28/28 integration tests passing

## Code Refactoring (P0 - Critical for Thin Client Architecture)

118. Refactor repositories to call stored procedures instead of LINQ queries (thin client principle)
119. Extract duplicate tenant validation logic in `Hartonomous.Api.Authorization` to `TenantValidationService`
120. Replace magic string "default-tenant" in `TenantIsolationHandler.cs` with configuration constant
121. Extract hardcoded ONNX model path `D:\models\all-MiniLM-L6-v2.onnx` to configuration
122. Replace magic number 1536 (embedding dimensions) with named constant `EmbeddingDimensions`
123. Implement `IDisposable` pattern for `Neo4jGraphService` (manages Neo4j driver lifecycle)
124. Remove commented-out code blocks (47 instances across codebase)
125. Replace `// TODO: Implement` stubs with actual implementations or `NotImplementedException` (23 instances)
126. Add XML documentation comments to all public API methods in `Hartonomous.Api.Controllers`
127. Fix inconsistent null-checking patterns: standardize on `is null` instead of mixing `== null` and `is null`

## Code Refactoring (P1 - High Priority for Thin Client Architecture)

128. Move business logic from repositories to SQL stored procedures (thin client principle)
129. Replace inline SQL queries in repositories with stored procedure calls
130. Consolidate 3 duplicate embedding generation methods - move to SQL CLR function
131. Consolidate 5 duplicate vector operation implementations - move to SQL CLR functions
132. Extract SIMD/AVX intrinsics code to dedicated SQL CLR assembly
133. Replace `dynamic` types in `ModelIngestionService` with strongly-typed DTOs
134. Add input validation attributes to all DTOs (e.g., `[Required]`, `[Range]`, `[StringLength]`)
135. Replace `Task.Run()` with proper async/await patterns in worker services
136. Implement cancellation token support in all long-running operations
137. Add structured logging with semantic properties instead of string interpolation
138. Extract billing calculation logic from API controllers to SQL stored procedures

## Package Version Fixes

139. Upgrade all `OpenTelemetry.*` packages to consistent version 1.13.1 (currently mixed 1.9.0/1.12.0/1.13.1)
140. Upgrade `Microsoft.Extensions.Hosting` from 9.0.10 to 10.0.0-rc.2 for consistency
141. Upgrade `Microsoft.Extensions.Logging.Abstractions` from 9.0.10 to 10.0.0-rc.2 for consistency
142. Upgrade `Newtonsoft.Json` from 13.0.3 to 13.0.4 in projects using older version
143. Remove duplicate `SqlClrFunctions-BACKUP.csproj` and `SqlClrFunctions-CLEAN.csproj` (keep only `SqlClrFunctions.csproj`)
144. Monitor 14 preview/RC packages for GA releases (expected Q1 2026) and upgrade when stable

## SQL Server 2025 Feature Migration (Backlog)

145. Plan migration from `VARBINARY(MAX)` to native `VECTOR(1536)` type for AtomEmbeddings.Embedding column
146. Create SQL script to change column type: `ALTER TABLE AtomEmbeddings ALTER COLUMN Embedding VECTOR(1536);`
147. Update CLR functions to accept `SqlVector<float>` instead of `VARBINARY(MAX)`
148. Test vector operations with native VECTOR type (expect 50x performance improvement)
149. Re-scaffold EF Core entities after VECTOR type migration
150. Plan migration from `nvarchar(max)` to native `json` type for Atoms.Metadata, Atoms.Semantics
151. Enable compatibility level 170 or `UseAzureSql()` in DbContext for native JSON support
152. Create SQL script for JSON columns (automatic conversion)
153. Test JSON queries (`JSON_VALUE`, `JSON_QUERY`, `JSON_MODIFY`) after migration
154. Re-scaffold EF Core entities after JSON type migration
155. Add columnstore indexes to `BillingUsageLedger` and `AutonomousImprovementHistory` tables
156. Optimize temporal table retention policies for `TensorAtomCoefficients_History` and `Weights_History`

## Security Hardening

157. Configure Windows Firewall on HART-DESKTOP to allow SQL Server (port 1433) only from HART-SERVER IP
158. Disable `sa` account: `ALTER LOGIN sa DISABLE;`
159. Disable remote DAC: `sp_configure 'remote admin connections', 0; RECONFIGURE;`
160. Configure UFW firewall on HART-SERVER: allow SSH (22), HTTP (80), HTTPS (443), SQL from HART-DESKTOP only
161. Migrate all connection strings from `appsettings.json` to Azure Key Vault
162. Migrate all API keys from environment variables to Azure Key Vault
163. Remove `appsettings.Production.json` from Git history (contains potential secrets)
164. Add `*.secrets.json`, `*.env`, `appsettings.Production.json` to `.gitignore`
165. Document UNSAFE CLR deployment prerequisites in `docs/CLR_DEPLOYMENT.md` (TRUSTWORTHY, certificate signing)
166. Document Terms of Service for UNSAFE CLR acceptance in user agreement

## Monitoring and Alerting

167. Create Application Insights availability test for API health endpoint
168. Create alert for SQL Server CPU > 80% for 5 minutes
169. Create alert for SQL Server memory pressure (page life expectancy < 300 seconds)
170. Create alert for disk space < 10% free on HART-DESKTOP (FILESTREAM storage)
171. Create alert for failed login attempts > 10 per minute (potential attack)
172. Configure Azure Monitor Workbook for SQL Server Arc performance metrics
173. Enable Best Practices Assessment on SQL Server Arc instances
174. Schedule Best Practices Assessment to run weekly, review recommendations

## Backup and Disaster Recovery

175. Configure SQL Server automated backups with FILESTREAM support: `BACKUP DATABASE Hartonomous TO DISK='D:\Backups\Hartonomous.bak' WITH FILESTREAM;`
176. Schedule daily full backups of Hartonomous database (11 PM)
177. Schedule hourly transaction log backups
178. Configure file system backup for FILESTREAM folder (`D:\SQL_FILESTREAM`)
179. Test database restore from backup (including FILESTREAM files)
180. Document failover procedure: HART-SERVER failure → restore application on HART-DESKTOP
181. Document failover procedure: HART-DESKTOP SQL failure → promote HART-SERVER to primary (linked server reversal)
182. Test disaster recovery procedure in non-production environment

## Documentation Updates

183. Update `README.md` with database-first architecture explanation (SQL project as source of truth, EF Core scaffolding)
184. Update `DEVELOPMENT.md` with local development setup instructions (HART-DESKTOP only, DACPAC deployment)
185. Update `docs/DEPLOYMENT.md` with HART-SERVER deployment procedure (DACPAC-based)
186. Document linked server configuration in `docs/DEPLOYMENT.md`
187. Document Service Broker cross-server routing in `docs/DEPLOYMENT.md`
188. Create `docs/UNSAFE_CLR_SECURITY.md` documenting security model and Terms of Service
189. Create `docs/EXTERNAL_ID_INTEGRATION.md` documenting authentication flows
190. Create `src/Hartonomous.Data/README.md` explaining database-first approach and scaffolding procedure
191. Document which CLR functions require UNSAFE permission and why in `docs/CLR_DEPLOYMENT.md`
192. Create runbook for common operational tasks (restart services, check logs, failover, re-scaffold EF Core)
193. Document thin client architecture principles in `docs/ARCHITECTURE.md`

## CI/CD Pipeline (Azure DevOps)

194. Review existing `azure-pipelines.yml` configuration
195. Add build stage for SQL Server Database Project (DACPAC)
196. Add build stage for SQL CLR project (targets .NET Framework 4.8.1)
197. Add build stage for all .NET projects
198. Add test stage running unit tests only (skip integration tests requiring infrastructure)
199. Add database deployment stage: `SqlPackage /Action:Publish` on HART-DESKTOP
200. Add EF Core scaffolding stage: regenerate entities after database deployment
201. Add application deployment stage: run `deploy-to-hart-server.ps1` to copy binaries to HART-SERVER
202. Add smoke test stage: verify API health endpoint returns 200 OK
203. Configure pipeline secrets in Azure DevOps for Key Vault access
204. Configure pipeline service connection for SSH to HART-SERVER
205. Enable CI trigger on `main` branch commits
206. Enable PR validation (build + unit tests) on pull requests

## Performance Testing

207. Measure baseline linked server query latency (HART-SERVER → HART-DESKTOP)
208. Measure Service Broker message throughput (messages/second across servers)
209. Load test API with 100 concurrent requests using Apache Bench or k6
210. Measure p50, p95, p99 response times under load
211. Identify bottlenecks (SQL queries, CLR functions, Neo4j writes)
212. Optimize slow queries using execution plans and indexes (update SQL project)
213. Consider adding read replicas for frequently-accessed data on HART-SERVER

## Final Validation

214. Deploy complete system (HART-DESKTOP SQL + HART-SERVER apps)
215. Execute end-to-end test: user sign-up → atom creation → inference → graph projection → billing update
216. Verify FILESTREAM data written to disk
217. Verify CLR functions executing correctly
218. Verify Service Broker messages routing across servers
219. Verify CDC replication updating HART-SERVER tables
220. Verify Neo4j graph updates from worker
221. Verify billing ledger entries created
222. Verify Application Insights telemetry flowing
223. Verify all 28 integration tests passing
224. Verify all 110 unit tests passing
225. Document remaining known issues in GitHub Issues
226. Tag stable release: `git tag v1.0.0-stable && git push origin v1.0.0-stable`

---

**Total Action Items**: 226  
**Current Completion**: ~10% (build succeeds, Azure infrastructure exists, Arc servers connected, but EF Core still code-first)  
**Estimated Remaining Effort**: 140-180 hours

**Critical Path** (Must Complete First):

1. Items 1-29: Database schema consolidation (SQL project as source of truth)
2. Items 30-40: EF Core scaffolding (database-first)
3. Items 41-47: DACPAC deployment scripts
4. Items 48-70: Infrastructure setup (SQL Server, linked servers, CDC)
5. Items 118-138: Thin client refactoring (move logic to SQL/CLR)

**Architecture Benefits**:

- Single source of truth: SQL Server Database Project
- Reduced application complexity: thin clients, minimal ORM configuration
- Database change management: DACPAC idempotent deployments
- Performance: business logic in SQL/CLR closer to data
- Maintainability: clear separation between database and application layers
