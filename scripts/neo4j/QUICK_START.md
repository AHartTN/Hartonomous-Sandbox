# 🚀 Neo4j Schema - Quick Start Card

**Status:** ✅ Production Ready | **Version:** 2.0 | **Date:** 2025-11-21

## One-Command Deploy (Idempotent)

```powershell
.\scripts\neo4j\Deploy-Neo4jSchema.ps1
```

**That's it!** Run anytime to update/verify schema. No side effects.

---

## Files You Need to Know

| File | Purpose | When to Use |
|------|---------|-------------|
| `Deploy-Neo4jSchema.ps1` | Deploy/update schema | Every deployment, after changes |
| `CoreSchema.cypher` | Complete schema definition | Reference, manual deploy |
| `ProvenanceQueries.cypher` | 80+ ready queries | Copy queries for your code |
| `README.md` | Full documentation | Troubleshooting, deep dive |
| `DEPLOYMENT_SUMMARY.md` | What was built | Understanding deliverables |

---

## Common Tasks

### Deploy Schema
```powershell
.\scripts\neo4j\Deploy-Neo4jSchema.ps1
```

### Verify It Worked
```cypher
SHOW CONSTRAINTS; -- Should see 9
SHOW INDEXES;     -- Should see 13
MATCH (r:ReasoningMode) RETURN count(r); -- Should be 5
```

### Test Atom Creation
```cypher
MERGE (a:Atom {id: 12345, tenantId: 0})
SET a.atomType = 'test', a.syncType = 'TEST'
RETURN a;
```

### Test Lineage Query
```cypher
MATCH path = (a:Atom {id: 12345})-[:DERIVED_FROM*1..5]->(ancestor)
RETURN ancestor.id, length(path);
```

---

## What's Inside

### Node Types (20+)
`Atom`, `Tenant`, `User`, `Model`, `ModelVersion`, `Inference`, `Decision`, `Evidence`, `Operation`, `GenerationStream`, `Error`, `Validation`, `AtomVersion`, `DataClassification`, `ComplianceRule`, `AuditLog`, `ReasoningMode`, `Context`, `Alternative`, `Feedback`

### Relationship Types (30+)
`DERIVED_FROM`, `MERGED_FROM`, `CREATED_BY`, `ACCESSED_BY`, `USED_MODEL`, `RESULTED_IN`, `SUPPORTED_BY`, `INFLUENCED_BY`, `FAILED_WITH`, `CLASSIFIED_AS`, `LOGGED_ACTION_ON`, `EVOLVED_TO`, `HAS_VERSION`, `SUPERSEDED_BY`, and more...

### Constraints
- 9 uniqueness constraints (prevent duplicates)
- Composite keys: `(Atom.id, Atom.tenantId)`, `(User.userId, User.tenantId)`, etc.

### Indexes
- 13 performance indexes on common query fields
- Temporal indexes (createdAt, timestamp)
- Type indexes (atomType, operationType, taskType)

---

## Integration Points

### Worker Already Exists ✅
```csharp
// Neo4jSyncWorker.cs processes Service Broker messages
// Location: src/Hartonomous.Workers.Neo4jSync/
```

### API Already Exists ✅
```csharp
// Neo4jProvenanceService.cs - Query service
// ProvenanceController.cs - REST endpoints
```

### What's Ready
- [x] Schema deployed
- [x] Constraints active
- [x] Indexes built
- [x] Query library created
- [x] Worker code exists
- [ ] Worker tested with real data ← **Next step**

---

## Next Steps

1. **Start Worker** (if not running)
   ```
   In VS Code: Run task "Run Neo4jSyncWorker"
   ```

2. **Test Sync** (once Service Broker configured)
   ```sql
   EXEC sp_SendAtomToNeo4j @AtomId = 999;
   ```

3. **Verify in Neo4j**
   ```cypher
   MATCH (a:Atom {id: 999}) RETURN a;
   ```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Constraint already exists" | Ignore - script is idempotent |
| Worker not creating atoms | Check Service Broker queue configured |
| Query slow | Check indexes: `SHOW INDEXES;` |
| Need more queries | See `ProvenanceQueries.cypher` |

---

## Query Cheat Sheet

```cypher
// Find atom ancestry
MATCH p=(a:Atom {id: $id})-[:DERIVED_FROM*]->(anc) RETURN p;

// Find downstream impact
MATCH p=(a:Atom {id: $id})<-[:DERIVED_FROM*]-(dep) RETURN p;

// Track who created what
MATCH (u:User)-[:CREATED_BY]-(o:Operation)-[:OUTPUT_ATOM]->(a:Atom)
WHERE u.userId = $userId RETURN a;

// Find sensitive data
MATCH (a:Atom)-[:CLASSIFIED_AS]->(dc:DataClassification)
WHERE dc.level IN ['confidential', 'restricted'] RETURN a;

// Explain AI decision
MATCH (i:Inference {id: $id})-[:RESULTED_IN]->(d:Decision)
MATCH (d)-[:SUPPORTED_BY]->(ev:Evidence)
RETURN d, collect(ev);
```

---

**📚 Full docs:** `scripts/neo4j/README.md`  
**🔍 All queries:** `scripts/neo4j/queries/ProvenanceQueries.cypher`  
**📊 What's deployed:** `scripts/neo4j/DEPLOYMENT_SUMMARY.md`

---

**Remember:** The deployment script is idempotent - run it anytime! 🎯
