# Hartonomous REST API Reference

**Version**: 1.0  
**Base URL**: `/api` or `/api/v1`  
**Authentication**: Bearer token (External ID / Azure Entra ID)  
**Last Updated**: November 13, 2025

---

## Overview

The Hartonomous platform exposes **18 REST API controllers** providing comprehensive access to:

- **Semantic search** and vector embeddings
- **Model inference** and multimodal generation
- **Graph analytics** and Neo4j queries  
- **Autonomous system control** (OODA loop)
- **Billing and usage** tracking with quota enforcement
- **Content ingestion** with deduplication
- **Provenance tracking** for generation streams

**Base URLs**:
- Development: `https://localhost:5001`
- Production: Configured via `appsettings.json` or environment variables

**Swagger UI**: Available at `/swagger` when `ASPNETCORE_ENVIRONMENT=Development`

**Health Checks**:
- `/health/startup` - Startup probe
- `/health/ready` - Readiness probe  
- `/health/live` - Liveness probe

---

## Authentication

All endpoints require authentication unless otherwise specified.

**Authentication Method**: Azure Entra ID (formerly Azure Active Directory)

**Configuration**:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "{your-tenant-id}",
    "ClientId": "{your-client-id}",
    "Audience": "api://{your-client-id}"
  }
}
```

**Acquiring Tokens**:

```bash
# Using Azure CLI
az account get-access-token --resource api://{ClientId}

# Token format
Authorization: Bearer {access_token}
```

**Exposed Scope**: `api://{ClientId}/access_as_user`

---

## Table of Contents

1. [Analytics API](#analytics-api)
2. [Autonomy API (OODA Loop)](#autonomy-api)
3. [Billing API](#billing-api)
4. [Bulk Operations API](#bulk-operations-api)
5. [Embeddings API](#embeddings-api)
6. [Feedback API](#feedback-api)
7. [Generation API](#generation-api)
8. [Graph Analytics API](#graph-analytics-api)
9. [Graph Query API](#graph-query-api)
10. [Inference API](#inference-api)
11. [Ingestion API](#ingestion-api)
12. [Jobs API](#jobs-api)
13. [Models API](#models-api)
14. [Operations API](#operations-api)
15. [Provenance API](#provenance-api)
16. [Search API](#search-api)
17. [SQL Graph API](#sql-graph-api)
18. [Tokenizer API](#tokenizer-api)

---

## Analytics API

**Controller**: `AnalyticsController`  
**Base Path**: `/api/v1/Analytics`

### Endpoints

#### POST /api/v1/Analytics/usage
Get usage analytics with time series data.

**Request**: `UsageAnalyticsRequest`
```json
{
  "tenantId": "string",
  "startDate": "2025-11-01T00:00:00Z",
  "endDate": "2025-11-13T23:59:59Z",
  "groupBy": "day|hour|month"
}
```

**Response**: `UsageAnalyticsResponse`
```json
{
  "totalOperations": 125000,
  "totalCost": 1250.50,
  "breakdown": [
    { "date": "2025-11-01", "operations": 5000, "cost": 50.25 }
  ]
}
```

#### POST /api/v1/Analytics/models/performance
Get model performance metrics.

**Request**: `ModelPerformanceRequest`
**Response**: `ModelPerformanceResponse` (includes latency, accuracy, throughput)

#### GET /api/v1/Analytics/embeddings
Embedding statistics by type (text, image, audio, video).

**Response**: `EmbeddingStatsResponse`

#### GET /api/v1/Analytics/storage
Storage metrics and deduplication statistics.

**Response**: `StorageMetricsResponse` (includes total storage, deduplicated savings)

#### POST /api/v1/Analytics/top-atoms
Ranking of most-used atoms.

**Request**: `TopAtomsRequest` (limit, timeframe)  
**Response**: `TopAtomsResponse` (list of atoms with usage counts)

---

## Autonomy API

**Controller**: `AutonomyController`  
**Base Path**: `/api/Autonomy`  
**Authorization**: Requires **Admin** policy

### Endpoints

#### POST /api/Autonomy/ooda/analyze
Trigger OODA (Observe-Orient-Decide-Act) analysis phase.

**Request**: `OodaAnalyzeRequest`
**Response**: `OodaAnalyzeResponse` (analysis results, hypotheses generated)

#### GET /api/Autonomy/queues/status
Get Service Broker queue statuses for OODA loop.

**Response**: `QueueStatusResponse`
```json
{
  "queues": [
    { "name": "AnalyzeQueue", "messageCount": 15, "isActive": true },
    { "name": "HypothesizeQueue", "messageCount": 3, "isActive": true },
    { "name": "ActQueue", "messageCount": 0, "isActive": true },
    { "name": "LearnQueue", "messageCount": 7, "isActive": true }
  ]
}
```

#### GET /api/Autonomy/cycles/history
Get OODA cycle execution history.

**Response**: `OodaCycleHistoryResponse` (list of cycles with timestamps, actions taken)

#### POST /api/Autonomy/control/pause
Pause autonomous operations.

**Response**: `{ "status": "paused" }`

#### POST /api/Autonomy/control/resume
Resume autonomous operations.

**Response**: `{ "status": "running" }`

#### POST /api/Autonomy/control/reset
Reset all OODA conversations and clear queues.

**Response**: `{ "status": "reset" }`

---

## Billing API

**Controller**: `BillingController`  
**Base Path**: `/api/billing`  
**Authorization**: Tenant-based access control

### Endpoints

#### POST /api/billing/usage/report
Generate usage report for tenant.

**Request**: `UsageReportRequest`
```json
{
  "tenantId": "tenant-123",
  "startDate": "2025-11-01T00:00:00Z",
  "endDate": "2025-11-30T23:59:59Z",
  "usageType": "embedding|inference|search|all"
}
```

**Response**: `UsageReportResponse`
```json
{
  "tenantId": "tenant-123",
  "period": { "start": "2025-11-01", "end": "2025-11-30" },
  "usage": [
    { "type": "embedding", "count": 50000, "cost": 500.00 },
    { "type": "inference", "count": 25000, "cost": 750.00 }
  ],
  "totalCost": 1250.00
}
```

#### POST /api/billing/calculate
Calculate bill with volume discounts applied.

**Request**: `CalculateBillRequest`  
**Response**: `BillCalculationResponse` (base cost, discounts, final amount)

#### POST /api/billing/usage
Record usage event (with pre-execution quota checking).

**Request**: `RecordUsageRequest`
```json
{
  "tenantId": "tenant-123",
  "usageType": "inference",
  "quantity": 1,
  "metadata": { "modelId": "gpt-4", "tokens": 1500 }
}
```

**Response**: `RecordUsageResponse`
```json
{
  "recorded": true,
  "remainingQuota": 9750,
  "quotaExceeded": false
}
```

**Note**: Returns `quotaExceeded: true` if tenant exceeds quota (operation rejected).

#### GET /api/billing/quota
Get quota information for tenant and usage type.

**Query Parameters**: `tenantId`, `usageType`  
**Response**: `QuotaInfoResponse`

#### POST /api/billing/quota
Set or update quota limits (Admin only).

**Request**: `SetQuotaRequest`  
**Response**: `SetQuotaResponse`

---

## Bulk Operations API

**Controller**: `BulkController`  
**Base Path**: `/api/v1/Bulk`

### Endpoints

#### POST /api/v1/Bulk/ingest
Bulk ingest up to 10,000 items in a single request.

**Request**: `BulkIngestRequest`
```json
{
  "items": [
    { "content": "text content", "type": "text", "metadata": {} },
    { "content": "base64-encoded-data", "type": "image", "metadata": {} }
  ],
  "deduplicationStrategy": "content-hash|semantic-similarity"
}
```

**Response**: `BulkIngestResponse`
```json
{
  "jobId": "job-abc-123",
  "totalItems": 10000,
  "accepted": 9850,
  "duplicates": 150,
  "status": "processing"
}
```

**Limits**: Maximum 10,000 items per request

#### GET /api/v1/Bulk/jobs/{jobId}
Get job status with progress tracking.

**Response**: `JobStatusResponse`
```json
{
  "jobId": "job-abc-123",
  "status": "processing|completed|failed|cancelled",
  "progress": {
    "processed": 7500,
    "total": 10000,
    "percentComplete": 75
  },
  "results": {
    "atomsCreated": 7350,
    "duplicatesSkipped": 150,
    "errors": []
  }
}
```

#### POST /api/v1/Bulk/cancel/{jobId}
Cancel pending or processing job.

**Response**: `CancelJobResponse`

#### POST /api/v1/Bulk/upload
Upload multiple files (maximum 1GB total, 1000 files).

**Content-Type**: `multipart/form-data`  
**Response**: `BulkUploadResponse` (file IDs, ingestion job IDs)

#### GET /api/v1/Bulk/jobs
List jobs with pagination and filtering.

**Query Parameters**: `page`, `pageSize`, `status`, `tenantId`  
**Response**: `JobListResponse`

---

## Embeddings API

**Controller**: `EmbeddingsController`  
**Base Path**: `/api/embeddings`

### Endpoints

#### POST /api/embeddings/text
Generate embedding from text.

**Request**: `TextEmbeddingRequest`
```json
{
  "text": "The quick brown fox jumps over the lazy dog",
  "model": "text-embedding-ada-002|custom-model-id"
}
```

**Response**: `EmbeddingResponse`
```json
{
  "atomId": "atom-123",
  "embeddingId": "embed-456",
  "vector": [0.123, -0.456, 0.789, ...],
  "dimensions": 1536,
  "isDuplicate": false,
  "duplicateOf": null,
  "deduplicationMetadata": {
    "contentHash": "sha256:abc123...",
    "similarityScore": 1.0
  }
}
```

#### POST /api/embeddings/image
Generate embedding from image.

**Content-Type**: `multipart/form-data`  
**Form Data**: `file` (image file), `model` (optional)  
**Response**: `EmbeddingResponse`

**Supported Formats**: JPEG, PNG, GIF, WebP

#### POST /api/embeddings/audio
Generate embedding from audio.

**Content-Type**: `multipart/form-data`  
**Form Data**: `file` (audio file), `model` (optional)  
**Response**: `EmbeddingResponse`

**Supported Formats**: MP3, WAV, OGG, FLAC

#### POST /api/embeddings/video-frame
Generate embedding from video frame.

**Content-Type**: `multipart/form-data`  
**Form Data**: `file` (video file), `frameNumber` (which frame to extract), `model` (optional)  
**Response**: `EmbeddingResponse`

**Supported Formats**: MP4, AVI, MOV, WebM

---

## Feedback API

**Controller**: `FeedbackController`  
**Base Path**: `/api/v1/Feedback`

### Endpoints

#### POST /api/v1/Feedback/submit
Submit feedback for inference result.

**Request**: `SubmitFeedbackRequest`
```json
{
  "inferenceId": "infer-123",
  "rating": 5,
  "comment": "Excellent result",
  "metadata": { "helpful": true }
}
```

**Response**: `FeedbackResponse`

#### POST /api/v1/Feedback/importance
Update atom importance scores.

**Request**: `UpdateImportanceRequest`  
**Response**: `ImportanceUpdateResponse`

#### POST /api/v1/Feedback/fine-tune/trigger
Trigger model fine-tuning job based on feedback.

**Request**: `TriggerFineTuneRequest`  
**Response**: `FineTuneJobResponse`

#### GET /api/v1/Feedback/summary
Get feedback summary with trends.

**Query Parameters**: `startDate`, `endDate`, `modelId`  
**Response**: `FeedbackSummaryResponse`

---

## Generation API

**Controller**: `GenerationController`  
**Base Path**: `/api/generation` (exact path varies)  
**Authorization**: Required, rate-limited

### Endpoints

#### POST /api/generation/text
Generate text from prompt.

**Request**: `GenerateTextRequest`
```json
{
  "prompt": "Write a story about a robot",
  "model": "gpt-4|custom-model-id",
  "maxTokens": 1000,
  "temperature": 0.7,
  "stream": false
}
```

**Response**: `GenerationResponse`
```json
{
  "generationId": "gen-123",
  "text": "Once upon a time...",
  "tokensUsed": 150,
  "model": "gpt-4",
  "finishReason": "stop|length|content_filter"
}
```

**Additional Modalities**: Image, audio, and video generation endpoints (implementation varies)

---

## Graph Analytics API

**Controller**: `GraphAnalyticsController`  
**Base Path**: `/api/graph/analytics` (exact path varies)

### Endpoints

#### GET /api/graph/analytics/stats
Get graph statistics (Neo4j).

**Response**: `GraphStatsResponse`
```json
{
  "totalNodes": 1500000,
  "totalRelationships": 3200000,
  "density": 0.00142,
  "connectedComponents": 3,
  "modalityBreakdown": {
    "text": 800000,
    "image": 450000,
    "audio": 150000,
    "video": 100000
  },
  "relationshipTypes": {
    "SIMILAR_TO": 1200000,
    "DERIVED_FROM": 800000,
    "REFERENCES": 1200000
  },
  "componentAnalysis": [
    { "componentId": 1, "nodeCount": 1498000 },
    { "componentId": 2, "nodeCount": 1500 },
    { "componentId": 3, "nodeCount": 500 }
  ]
}
```

**Additional Analytics**: More endpoints for centrality, clustering, path analysis

---

## Graph Query API

**Controller**: `GraphQueryController`  
**Base Path**: `/api/graph` (exact path varies)

### Endpoints

#### POST /api/graph/query
Execute Cypher query against Neo4j.

**Request**: `CypherQueryRequest`
```json
{
  "query": "MATCH (n:Atom) WHERE n.type = $type RETURN n LIMIT 10",
  "parameters": { "type": "text" }
}
```

**Response**: `CypherQueryResponse`
```json
{
  "results": [
    { "n": { "atomId": "atom-123", "type": "text", ... } }
  ],
  "executionTime": 125
}
```

**Additional Endpoints**: Relationship traversal, concept exploration, path finding

---

## Inference API

**Controller**: `InferenceController`  
**Base Path**: `/api/inference`

### Endpoints

#### POST /api/inference/run
Run synchronous inference.

**Request**: `InferenceRequest`
```json
{
  "modelId": "model-123",
  "input": { "text": "What is AI?" },
  "parameters": { "temperature": 0.7, "maxTokens": 500 }
}
```

**Response**: `InferenceResponse`
```json
{
  "inferenceId": "infer-456",
  "output": { "text": "AI stands for..." },
  "latency": 125,
  "tokensUsed": 75,
  "model": "model-123"
}
```

#### POST /api/inference/async/text
Submit asynchronous text generation job.

**Request**: `AsyncTextGenerationRequest`  
**Response**: `AsyncJobResponse` (includes `jobId` for status polling)

---

## Ingestion API

**Controller**: `IngestionController`  
**Base Path**: `/api/ingestion` (exact path varies)

### Endpoints

#### POST /api/ingestion/ingest
Ingest content with atom creation.

**Request**: `IngestRequest`
```json
{
  "content": "Sample content",
  "type": "text|image|audio|video",
  "metadata": { "source": "api", "author": "user-123" },
  "deduplicationStrategy": "content-hash|semantic-similarity|none"
}
```

**Response**: `IngestResponse`
```json
{
  "atomId": "atom-789",
  "isDuplicate": false,
  "duplicateOf": null,
  "deduplicationMetadata": {
    "contentHash": "sha256:def456...",
    "similarityScore": 1.0,
    "matchedAtoms": []
  },
  "embeddingId": "embed-012",
  "created": true
}
```

---

## Jobs API

**Controller**: `JobsController`  
**Base Path**: `/api/Jobs`  
**Authorization**: Requires **Admin** policy

### Endpoints

#### GET /api/Jobs/{jobId}
Get specific job by ID.

**Response**: `JobDetailsResponse` (includes full job details, logs, timeline)

#### GET /api/Jobs/status/{status}
Get jobs by status with pagination.

**Path Parameter**: `status` (pending|processing|completed|failed|cancelled)  
**Query Parameters**: `page`, `pageSize`  
**Response**: `JobListResponse`

#### POST /api/Jobs/cleanup
Enqueue cleanup job for old data.

**Request**: `CleanupJobRequest`  
**Response**: `CleanupJobResponse`

---

## Models API

**Controller**: `ModelsController`  
**Base Path**: `/api/models`

### Endpoints

#### GET /api/models
Get models with pagination.

**Query Parameters**: `page`, `pageSize`, `type`, `status`  
**Response**: `ModelListResponse`

**Additional Endpoints**: Model management, registration, versioning (implementation varies)

---

## Operations API

**Controller**: `OperationsController`  
**Base Path**: `/api/v1/Operations`

### Endpoints

#### GET /api/v1/Operations/health
Comprehensive health check.

**Response**: `HealthCheckResponse`
```json
{
  "status": "healthy|degraded|unhealthy",
  "checks": {
    "database": "healthy",
    "tables": "healthy",
    "clr": "healthy",
    "neo4j": "healthy",
    "serviceBroker": "healthy"
  },
  "timestamp": "2025-11-13T15:30:00Z"
}
```

#### POST /api/v1/Operations/indexes/maintenance
Trigger index maintenance and optimization.

**Request**: `IndexMaintenanceRequest`  
**Response**: `IndexMaintenanceResponse` (indexes rebuilt, fragmentation stats)

---

## Provenance API

**Controller**: `ProvenanceController`  
**Base Path**: `/api/provenance` (exact path varies)

### Endpoints

#### GET /api/provenance/stream/{streamId}
Get generation stream details.

**Response**: `GenerationStreamResponse` (includes segments, sources, transformations)

#### GET /api/provenance/inference/{inferenceId}
Get inference provenance details.

**Response**: `InferenceProvenanceResponse` (includes input atoms, model used, output atoms, lineage)

---

## Search API

**Controller**: `SearchController`  
**Base Path**: `/api/search`

### Endpoints

#### POST /api/search
Semantic/hybrid search with optional filters.

**Request**: `SearchRequest`
```json
{
  "query": "machine learning fundamentals",
  "type": "semantic|hybrid|vector",
  "filters": {
    "modality": ["text", "image"],
    "dateRange": { "start": "2025-01-01", "end": "2025-11-13" }
  },
  "limit": 10,
  "offset": 0
}
```

**Response**: `SearchResponse`
```json
{
  "results": [
    {
      "atomId": "atom-123",
      "score": 0.95,
      "content": "Machine learning is...",
      "metadata": { "type": "text", "created": "2025-11-01" },
      "embedding": { "id": "embed-456", "similarity": 0.95 }
    }
  ],
  "total": 150,
  "took": 45
}
```

**Search Types**:
- `semantic` - Vector similarity search
- `hybrid` - Vector + keyword + spatial
- `vector` - Pure vector search

---

## SQL Graph API

**Controller**: `SqlGraphController`  
**Base Path**: `/api/graph/sql` (exact path varies)

### Endpoints

#### POST /api/graph/sql/node
Create SQL Server graph node.

**Request**: `CreateGraphNodeRequest`  
**Response**: `GraphNodeResponse`

---

## Tokenizer API

**Controller**: `TokenizerController`  
**Base Path**: `/api/tokenizer` (exact path varies)

### Endpoints

#### POST /api/tokenizer/tokenize
Tokenize text.

**Request**: `TokenizeRequest`
```json
{
  "text": "The quick brown fox",
  "model": "gpt-4|custom-tokenizer"
}
```

**Response**: `TokenizeResponse`
```json
{
  "tokens": [464, 4062, 14198, 39935],
  "count": 4,
  "text": ["The", " quick", " brown", " fox"]
}
```

---

## Error Handling

All endpoints follow standard HTTP status codes:

- `200 OK` - Success
- `201 Created` - Resource created
- `400 Bad Request` - Invalid request
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Server error
- `503 Service Unavailable` - Service temporarily unavailable

**Error Response Format**:
```json
{
  "error": {
    "code": "QUOTA_EXCEEDED",
    "message": "Tenant quota exceeded for inference operations",
    "details": {
      "tenantId": "tenant-123",
      "usageType": "inference",
      "currentUsage": 10000,
      "quota": 10000
    }
  }
}
```

---

## Rate Limiting

Rate limits are enforced per tenant:

- **Default**: 1000 requests/minute
- **Burst**: 100 requests/second
- **Quota**: Enforced pre-execution (billing system)

**Rate Limit Headers**:
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 750
X-RateLimit-Reset: 1699891200
```

---

## Pagination

List endpoints support pagination:

**Query Parameters**:
- `page` - Page number (1-based)
- `pageSize` - Items per page (default: 20, max: 100)

**Response Format**:
```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "total": 1500,
    "totalPages": 75
  }
}
```

---

## Testing Status

**Current Test Coverage**: 
- API Controllers: **Minimal coverage** (some unit tests exist)
- Integration Tests: **25 failing** (infrastructure dependencies)
- See [Testing Guide](../development/testing-guide.md) for details

---

## Source Code

Controller implementations: `src/Hartonomous.Api/Controllers/`

- AnalyticsController.cs
- AutonomyController.cs
- BillingController.cs
- BulkController.cs
- EmbeddingsController.cs
- FeedbackController.cs
- GenerationController.cs
- GraphAnalyticsController.cs
- GraphQueryController.cs
- InferenceController.cs
- IngestionController.cs
- JobsController.cs
- ModelsController.cs
- OperationsController.cs
- ProvenanceController.cs
- SearchController.cs
- SqlGraphController.cs
- TokenizerController.cs

---

## Additional Resources

- [Architecture Overview](../ARCHITECTURE.md)
- [Deployment Guide](../deployment/deployment-guide.md)
- [Testing Guide](../development/testing-guide.md)
- [Security Documentation](../security/)
