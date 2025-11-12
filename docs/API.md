# API Reference

## Overview

`Hartonomous.Api` (ASP.NET Core 10) exposes the public REST surface for embeddings, search, graph exploration, analytics, and operations.

- All routes live under `/api/...`
- Local development host: `https://localhost:5001` (configurable via `appsettings.json` or environment variables)
- Swagger UI available at `/swagger` when `ASPNETCORE_ENVIRONMENT=Development`
- Health probes: `/health/startup`, `/health/ready`, `/health/live` (GET)
- **Testing Status**: API controllers have **0% test coverage** (15 controllers, 0 tests). See [TESTING_AUDIT_AND_COVERAGE_PLAN.md](TESTING_AUDIT_AND_COVERAGE_PLAN.md) for testing roadmap.

## Authentication

The API is secured with Azure Active Directory via `AddMicrosoftIdentityWebApi`:

- Configure an Entra ID application with the Web API platform
- Expose scope: `api://{ClientId}/access_as_user`
- Provide `AzureAd:Instance`, `TenantId`, `ClientId`, `Audience` through configuration (environment variables or user-secrets)
- Acquire tokens with MSAL or Azure CLI: `az account get-access-token --resource api://{ClientId}`
- Send token as `Authorization: Bearer {access_token}`
- For Swagger/OAuth flows, register `https://localhost:5001/signin-oidc` (or environment host) as the redirect URI

## Response Envelope

Controllers inheriting `ApiControllerBase` return `Hartonomous.Shared.Contracts.Responses.ApiResponse<T>`:

```json
{
  "succeeded": true,
  "data": {
    "atomId": 1285,
    "atomEmbeddingId": 9231,
    "wasExisting": false
  },
  "errors": [],
  "correlationId": "0HML8FVCPF9T2:00000002",
  "metadata": {
    "embeddingDimension": 1536
  }
}
```

Legacy `/api/v1/...` controllers use `Hartonomous.Api.Common.ApiResponse<T>`:

```json
{
  "success": true,
  "data": {
    "totalRequests": 4200
  },
  "error": null,
  "metadata": {
    "totalCount": 12
  }
}
```

Validation failures populate `errors`/`error` with `ErrorDetail` metadata. Rate-limit rejections respond with HTTP 429 using the same envelope.

## Rate Limiting

`app.MapControllers().RequireRateLimiting("api")` enforces rate limiting policies:

**Standard Policies** (sliding window):

- `authenticated`: 100 requests/minute
- `anonymous`: 10 requests/minute
- `premium`: 1000 requests/minute

**Specialized Policies**:

- `inference`: concurrency limit 10 + token bucket (20 token capacity, replenishes 5/min). Used by generation endpoints via `[EnableRateLimiting("inference")]`
- `embedding`: 50 requests/minute
- `graph`: 30 requests/minute
- `global`: 200 requests/minute per remote IP

**Tenant-Tier-Aware**:

- `tenant-api`, `tenant-inference`: Resolved through `TenantRateLimitPolicy` and applied where annotated

## Endpoint Catalogue

| Area | Base Route | Controller | Notes |
|------|------------|------------|-------|
| Embeddings | `/api/embeddings` | `EmbeddingsController` | Text and media embeddings with automatic atom ingestion |
| Search | `/api/search` | `SearchController` | Hybrid, cross-modal, spatial, and temporal search strategies |
| Generation | `/api/generation` | `GenerationController` | Multimodal generation backed by SQL stored procedures |
| Inference Jobs | `/api/inference` | `InferenceController` | Async job submission for text generation and ensembles |
| Bulk Ingestion | `/api/v1/bulk` | `BulkController` | Job-based ingestion pipeline helpers |
| Analytics | `/api/v1/analytics` | `AnalyticsController` | Usage, model, embedding, and storage analytics |
| Graph | `/api/v1/graph` | `GraphQueryController`, `GraphAnalyticsController`, `SqlGraphController` | Neo4j + SQL graph utilities |
| Provenance | `/api/v1/provenance` | `ProvenanceController` | Inference lineage and generation streams |
| Autonomy | `/api/autonomy` | `AutonomyController` | OODA loop orchestration and Service Broker controls |
| Models | `/api/models` | `ModelsController` | Model registry CRUD, stats, and distillation |
| Billing | `/api/billing` | `BillingController` | Usage capture, quota management, billing projections |
| Feedback | `/api/v1/feedback` | `FeedbackController` | Feedback submission and fine-tune triggers |
| Operations | `/api/v1/operations` | `OperationsController` | Health, diagnostics, cache/index management |
| Ingestion | `/api/v1/ingestion` | `IngestionController` | Single-item ingestion helper |
| Jobs | `/api/jobs` | `JobsController` | Background job status and scheduling |

## Embeddings (`/api/embeddings`)

### POST /api/embeddings/text

**Request**: `EmbeddingRequest`

```json
{
  "text": "Sample query text",
  "modelId": 7,
  "embeddingType": "semantic"
}
```

**Response**: `EmbeddingResponse`

```json
{
  "succeeded": true,
  "data": {
    "atomId": 1285,
    "atomEmbeddingId": 9231,
    "wasExisting": false,
    "duplicateReason": null,
    "semanticSimilarity": null
  },
  "errors": [],
  "metadata": {
    "embeddingDimension": 1536,
    "modelId": 7
  }
}
```

### POST /api/embeddings/image

**Request**: `multipart/form-data`

- `file`: Image file (≤64 MB)
- `sourceType`: (optional) Source identifier
- `metadata`: (optional) JSON metadata
- `modelId`: (optional) Model ID

**Response**: `MediaEmbeddingResponse` with `atomId`, `atomEmbeddingId`, deduplication info

### POST /api/embeddings/audio

**Request**: `multipart/form-data` (same as image)

**Response**: `MediaEmbeddingResponse`

### POST /api/embeddings/video-frame

**Request**: `multipart/form-data` (same as image)

**Response**: `MediaEmbeddingResponse`

Content is deduplicated via SHA-256 hashes. Requests are wrapped in `MediaEmbeddingRequest` internally.

## Search (`/api/search`)

### POST /api/search

**Request**: `SearchRequest`

```json
{
  "queryText": "machine learning embeddings",
  "queryEmbedding": null,
  "queryVector": null,
  "topK": 10,
  "topicFilter": null,
  "minSentiment": null,
  "maxAge": null
}
```

**Response**: `SearchResponse`

```json
{
  "succeeded": true,
  "data": {
    "results": [
      {
        "atomId": 512,
        "atomEmbeddingId": 1048576,
        "canonicalText": "Nearest neighbour result",
        "modality": "text",
        "similarityScore": 0.937,
        "spatialDistance": null,
        "contentHash": null
      }
    ],
    "totalResults": 10,
    "queryDuration": 47.3
  },
  "errors": [],
  "metadata": {
    "strategy": "hybrid",
    "requestedTopK": 10,
    "candidateCount": 200
  }
}
```

### POST /api/search/cross-modal

**Request**: `CrossModalSearchRequest`

- Optional `text`
- Target modality list
- Generates embeddings on-demand when vector not provided

**Response**: `CrossModalSearchResponse` with multi-modality results

### POST /api/search/spatial

**Request**: `SpatialSearchRequest`

```json
{
  "latitude": 47.6062,
  "longitude": -122.3321,
  "radiusKm": 10.0,
  "modality": "image",
  "modelId": null,
  "topK": 20
}
```

**Response**: Geography-aware search results with spatial distance

### POST /api/search/temporal

**Request**: `TemporalSearchRequest`

- Time-boxed semantic search
- Optional `startDate`, `endDate` filters

**Response**: `TemporalSearchResponse` with timestamp metadata

## Generation (`/api/generation`)

All routes require authorization and use `inference` rate-limit policy.

### POST /api/generation/text

**Request**: `GenerateTextRequest`

```json
{
  "prompt": "Write a haiku about databases",
  "modelId": 3,
  "maxTokens": 100,
  "temperature": 0.7
}
```

**Response**: `GenerationResponse`

- Executes `dbo.sp_GenerateText` stored procedure
- Returns generated text and atom linkage

### POST /api/generation/image

**Request**: `GenerateImageRequest`

**Response**: Metadata (dimensions, format) and stored `AtomId`

### POST /api/generation/audio

**Request**: `GenerateAudioRequest`

**Response**: Reference metadata for generated audio

### POST /api/generation/video

**Request**: `GenerateVideoRequest`

**Response**: Short clip reference metadata

## Inference Jobs (`/api/inference`)

### POST /api/inference/generate/text

**Request**: Async text generation job

**Response**: `202 Accepted`, `JobSubmittedResponse`

Queues asynchronous job via `InferenceJobService`.

### POST /api/inference/ensemble

**Request**: Ensemble inference request (multiple models)

**Response**: `202 Accepted`, `JobSubmittedResponse`

## Bulk Ingestion (`/api/v1/bulk`)

Responses use `ApiResponse<T>` from `Hartonomous.Api.Common`.

### POST /ingest

**Request**: `BulkIngestRequest`

**Response**: Job creation confirmation

Creates job, persists records, optionally queues Service Broker processing.

### GET /status/{jobId}

**Response**: `BulkJobStatusResponse` with progress counts

### POST /cancel

**Request**: Job ID

**Response**: Cancellation confirmation

### POST /upload

**Request**: CSV payload

**Response**: Ingestion job ID

### GET /jobs

**Response**: List of jobs with paging metadata

## Analytics (`/api/v1/analytics`)

### POST /usage

**Request**: `UsageAnalyticsRequest`

**Response**: `UsageAnalyticsResponse` with time-series datapoints and summary

### POST /models/performance

**Response**: `ModelPerformanceResponse` (per-model metrics)

### POST /embeddings/stats

**Response**: Embedding type statistics

### GET /storage

**Response**: `StorageMetricsResponse` with size/deduplication breakdowns

### POST /top-atoms

**Response**: Ranked list of frequently referenced atoms

## Graph (`/api/v1/graph`)

### Neo4j Query Endpoints

#### POST /query

**Request**: `GraphQueryRequest` (arbitrary Cypher)

**Response**: Query results

#### POST /related

**Request**: Atom ID

**Response**: Related atoms by traversing relationships

#### POST /traverse

**Request**: Guided traversal between nodes (depth/relationship filters)

**Response**: Traversal path

#### POST /explore

**Request**: Exploration parameters

**Response**: Annotated exploration paths

### Graph Analytics (`GraphAnalyticsController`)

#### GET /stats

**Response**: Global node/edge counts, density, component metrics

#### POST /relationship-analysis

**Request**: Optional modality filter

**Response**: Relationship type summaries

#### POST /centrality

**Response**: Centrality metrics through GDS procedures

#### POST /create-relationship

**Request**: Relationship creation parameters

**Response**: Confirmation

### SQL Graph Bridge (`SqlGraphController`)

- `POST /sql/nodes`: Query SQL Server graph nodes
- `POST /sql/edges`: Query SQL Server graph edges
- `POST /sql/traverse`: Traverse SQL graph
- `POST /sql/shortest-path`: Shortest path query

All reuse common response envelope.

## Provenance (`/api/v1/provenance`)

### GET /streams/{streamId}

**Response**: `GenerationStreamDetail` (scope, model, created time, persisted stream blob)

### GET /inference/{inferenceId}

**Response**: `InferenceDetail` (inference metadata)

### GET /inference/{inferenceId}/steps

**Response**: List of `InferenceStepDetail` records (step order, duration, metadata)

## Autonomy (`/api/autonomy`)

Requires `Admin` policy.

### POST /ooda/analyze

**Response**: Executes `sp_Analyze` stored procedure, surfaces parsed observations

### GET /queues/status

**Response**: Service Broker queue depth and conversation counts

### GET /cycles/history

**Response**: OODA cycle history

### POST /control/pause

**Response**: Pause Service Broker queues

### POST /control/resume

**Response**: Resume Service Broker queues

### POST /control/reset

**Response**: Cleanup conversations and reset queues

## Models (`/api/models`)

### GET /

**Response**: List of registered models (optional filtering)

### GET /{modelId:int}

**Response**: Detailed model metadata

### GET /stats

**Response**: Aggregated usage metrics

### POST /

**Request**: Model definition

**Response**: Create or update confirmation

### POST /{modelId:int}/distill

**Response**: Distillation job submission confirmation

### GET /{modelId:int}/layers

**Response**: Layer metrics from SQL (`ModelLayers`)

## Billing (`/api/billing`)

### POST /usage/report

**Request**: Bulk usage ingestion

**Response**: Confirmation

### POST /calculate

**Response**: Billing projections

### POST /usage/record

**Request**: Single usage event

**Response**: Confirmation

### GET /quota

**Response**: Tenant quota status

### POST /quota

**Request**: Quota threshold updates

**Response**: Confirmation

## Feedback (`/api/v1/feedback`)

### POST /submit

**Request**: Feedback entries tied to atoms/inferences

**Response**: Confirmation

### POST /importance/update

**Request**: Importance score adjustments

**Response**: Confirmation

### POST /fine-tune/trigger

**Response**: Fine-tuning job submission confirmation

### POST /summary

**Response**: Aggregated feedback summaries

## Operations (`/api/v1/operations`)

### GET /health

**Response**: Database and Service Broker health checks

### POST /indexes/maintenance

**Response**: Index maintenance job submission

### POST /cache/manage

**Request**: Warm/flush cache parameters

**Response**: Cache management job confirmation

### POST /diagnostics

**Response**: Diagnostic stored procedure results

### GET /querystore/stats

**Response**: SQL Query Store summaries

### POST /autonomous/trigger

**Response**: Autonomous background workflow trigger confirmation

### GET /metrics

**Response**: Aggregated operational metrics (all tenants)

### GET /metrics/{tenantId}

**Response**: Tenant-specific operational metrics

## Ingestion (`/api/v1/ingestion`)

### POST /content

**Request**: `IngestionRequest` (single payload)

**Response**: Atom ingestion confirmation with deduplication support

## Jobs (`/api/jobs`)

### GET /{jobId:long}

**Response**: `JobStatusResponse`

### GET /

**Response**: List of jobs with filters/paging

### POST /cleanup

**Response**: Cleanup job submission

### POST /index-maintenance

**Response**: Index maintenance job submission

### POST /analytics

**Response**: Analytics job submission

### POST /{jobId:long}/cancel

**Response**: Job cancellation confirmation

### GET /stats

**Response**: Job-level aggregation

### POST /schedule/cleanup

**Response**: Schedule recurring cleanup job

### POST /schedule/analytics

**Response**: Schedule recurring analytics job

## Configuration

**Connection Strings**:

- `HartonomousDb`, `DefaultConnection`: SQL Server database
- `Neo4j:Uri`, `Neo4j:Username`, `Neo4j:Password`: Neo4j connectivity
- `AzureStorage:ConnectionString`: Azure Storage clients

**Azure AD**:

- `AzureAd:Instance`, `TenantId`, `ClientId`, `Audience`

**Health Endpoints** (`AllowAnonymous`):

- `/health/startup`
- `/health/ready`
- `/health/live`

**Metrics**:

- `/metrics` (enabled when OpenTelemetry exporters are configured)

## References

- [README.md](README.md) - Getting started guide
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture
- [DEPLOYMENT_ARCHITECTURE_PLAN.md](DEPLOYMENT_ARCHITECTURE_PLAN.md) - Deployment strategy
- [TESTING_AUDIT_AND_COVERAGE_PLAN.md](TESTING_AUDIT_AND_COVERAGE_PLAN.md) - API testing roadmap (0% current coverage)
- `src/Hartonomous.Api/Program.cs` - API configuration
- `src/Hartonomous.Api/Controllers/` - Controller implementations
- `src/Hartonomous.Shared.Contracts/` - Request/response DTOs
