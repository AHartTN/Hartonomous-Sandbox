# API Reference

## Overview

- `Hartonomous.Api` (ASP.NET Core 8) exposes the public REST surface for embeddings, search, graph exploration, analytics, and operations.
- All routes live under `/api/...`. In local development the default host is `https://localhost:5001`.
- Swagger UI is available at `/swagger` when `ASPNETCORE_ENVIRONMENT=Development`.
- Health probes: `/health/startup`, `/health/ready`, `/health/live` (GET).

## Authentication

- The API is secured with Azure Active Directory via `AddMicrosoftIdentityWebApi`; there is no password-based token endpoint in this service.
- Configure an Entra ID application with the Web API platform and expose the scope `api://{ClientId}/access_as_user`. Provide `AzureAd:Instance`, `TenantId`, `ClientId`, and `Audience` through configuration (environment variables or user-secrets).
- Acquire tokens with MSAL or Azure CLI, e.g. `az account get-access-token --resource api://{ClientId}`. Send the token as `Authorization: Bearer {access_token}`.
- For Swagger/OAuth flows, register `https://localhost:5001/signin-oidc` (or environment host) as the redirect URI.

## Response Envelope

- Controllers inheriting `ApiControllerBase` return `Hartonomous.Shared.Contracts.Responses.ApiResponse<T>`:

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

- Legacy `/api/v1/...` controllers use `Hartonomous.Api.Common.ApiResponse<T>`:

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

- Validation failures populate `errors`/`error` with `ErrorDetail` metadata; rate-limit rejections respond with HTTP 429 using the same envelope.

## Rate Limiting

- `app.MapControllers().RequireRateLimiting("api")` enforces a default fixed window of 100 requests/minute.
- Sliding-window policies from `RateLimitOptions`:
  - `authenticated`: 100 requests/minute.
  - `anonymous`: 10 requests/minute.
  - `premium`: 1000 requests/minute.
- Specialized policies:
  - `inference`: concurrency limit 10, plus token bucket (20 token capacity, replenishes 5/min). Used by generation endpoints via `[EnableRateLimiting("inference")]`.
  - `embedding`: 50 requests/minute.
  - `graph`: 30 requests/minute.
  - `global`: 200 requests/minute per remote IP.
- Tenant-tier-aware limiters (`tenant-api`, `tenant-inference`) are resolved through `TenantRateLimitPolicy` and applied where annotated.

## Endpoint Catalogue

| Area | Base Route | Controller | Notes |
|------|------------|------------|-------|
| Embeddings | `/api/embeddings` | `EmbeddingsController` | Text and media embeddings with automatic atom ingestion. |
| Search | `/api/search` | `SearchController` | Hybrid, cross-modal, spatial, and temporal search strategies. |
| Generation | `/api/generation` | `GenerationController` | Multimodal generation backed by SQL stored procedures. |
| Inference Jobs | `/api/inference` | `InferenceController` | Async job submission for text generation and ensembles. |
| Bulk Ingestion | `/api/v1/bulk` | `BulkController` | Job-based ingestion pipeline helpers. |
| Analytics | `/api/v1/analytics` | `AnalyticsController` | Usage, model, embedding, and storage analytics. |
| Graph | `/api/v1/graph` | `GraphQueryController`, `GraphAnalyticsController`, `SqlGraphController` | Neo4j + SQL graph utilities. |
| Provenance | `/api/v1/provenance` | `ProvenanceController` | Inference lineage and generation streams. |
| Autonomy | `/api/autonomy` | `AutonomyController` | OODA loop orchestration and Service Broker controls. |
| Models | `/api/models` | `ModelsController` | Model registry CRUD, stats, and distillation. |
| Billing | `/api/billing` | `BillingController` | Usage capture, quota management, billing projections. |
| Feedback | `/api/v1/feedback` | `FeedbackController` | Feedback submission and fine-tune triggers. |
| Operations | `/api/v1/operations` | `OperationsController` | Health, diagnostics, cache/index management. |
| Ingestion | `/api/v1/ingestion` | `IngestionController` | Single-item ingestion helper. |
| Jobs | `/api/jobs` | `JobsController` | Background job status and scheduling. |

### Embeddings (`/api/embeddings`)

- `POST /api/embeddings/text` — `EmbeddingRequest` (`text`, optional `modelId`, `embeddingType`). Responds with `EmbeddingResponse` inside the shared envelope and metadata describing the resulting vector (dimension, model id).
- `POST /api/embeddings/image` | `/audio` | `/video-frame` — `multipart/form-data` with `file` (≤64 MB), optional `sourceType`, `metadata` (JSON), `modelId`. Requests are wrapped in `MediaEmbeddingRequest` and deduplicate content via SHA256 hashes.

Example (text embedding):

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

### Search (`/api/search`)

- `POST /api/search` — Hybrid search (`SearchRequest`) accepting `queryText`, `queryEmbedding`/`queryVector`, and optional filters (`topicFilter`, `minSentiment`, `maxAge`). Returns `SearchResponse` with scoring metadata.
- `POST /api/search/cross-modal` — `CrossModalSearchRequest` combining optional text with target modality list. Generates embeddings on-demand when a vector is not provided.
- `POST /api/search/spatial` — `SpatialSearchRequest` for geography-aware queries (lat/long, radius, optional modality/model filters).
- `POST /api/search/temporal` — `TemporalSearchRequest` enabling time-boxed semantic search.

Hybrid search response sample:

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

### Generation (`/api/generation`)

All routes require authorization and opt into the `inference` rate-limit policy.

- `POST /api/generation/text` — `GenerateTextRequest`; executes `dbo.sp_GenerateText` and returns `GenerationResponse` with generated text and atom linkage.
- `POST /api/generation/image` — `GenerateImageRequest`; responds with metadata (dimensions, format) and stored `AtomId` when successful.
- `POST /api/generation/audio` — `GenerateAudioRequest`; returns reference metadata for generated audio.
- `POST /api/generation/video` — `GenerateVideoRequest` for short clips.

### Inference Jobs (`/api/inference`)

- `POST /api/inference/generate/text` — Queues an asynchronous text generation job via `InferenceJobService`, returning `202 Accepted` and `JobSubmittedResponse`.
- `POST /api/inference/ensemble` — Submits an ensemble inference request using multiple models.

> `GetJobStatusAsync` presently lacks an `[HttpGet]` attribute, so job status is not publicly reachable until the route is decorated.

### Bulk Ingestion (`/api/v1/bulk`)

Responses use `ApiResponse<T>` from `Hartonomous.Api.Common`.

- `POST /ingest` — Creates a job from `BulkIngestRequest`, persisting records and optionally queueing Service Broker processing.
- `GET /status/{jobId}` — Returns `BulkJobStatusResponse` with progress counts.
- `POST /cancel` — Cancels a pending job.
- `POST /upload` — Accepts CSV payloads for ingestion jobs.
- `GET /jobs` — Lists jobs with paging metadata.

### Analytics (`/api/v1/analytics`)

- `POST /usage` — `UsageAnalyticsRequest` producing `UsageAnalyticsResponse` (time-series datapoints + summary).
- `POST /models/performance` — Aggregates per-model metrics (`ModelPerformanceResponse`).
- `POST /embeddings/stats` — Embedding type statistics.
- `GET /storage` — Returns `StorageMetricsResponse` with size/deduplication breakdowns.
- `POST /top-atoms` — Ranked list of frequently referenced atoms.

### Graph (`/api/v1/graph`)

Neo4j query endpoints:

- `POST /query` — Executes arbitrary Cypher via `GraphQueryRequest`.
- `POST /related` — Finds related atoms by traversing relationships.
- `POST /traverse` — Guided traversal between nodes with depth/relationship filters.
- `POST /explore` — Builds annotated exploration paths (see `GraphQueryController`).
Analytics endpoints (`GraphAnalyticsController`):

- `GET /stats` — Global node/edge counts, density, component metrics.
- `POST /relationship-analysis` — Summaries of relationship types with optional modality filter.
- `POST /centrality` — Centrality metrics through GDS procedures.
- `POST /create-relationship` — Convenience method to append relationships.
SQL Graph bridge (`SqlGraphController`):

- `POST /sql/nodes`, `/sql/edges`, `/sql/traverse`, `/sql/shortest-path` — Interact with SQL Server graph tables while reusing the common response envelope.

### Provenance (`/api/v1/provenance`)

- `GET /streams/{streamId}` — Returns `GenerationStreamDetail` (scope, model, created time, persisted stream blob).
- `GET /inference/{inferenceId}` — Fetches inference metadata (`InferenceDetail`).
- `GET /inference/{inferenceId}/steps` — Lists `InferenceStepDetail` records with step order, duration, and metadata.

### Autonomy (`/api/autonomy`)

- `POST /ooda/analyze` — Executes `sp_Analyze` stored procedure and surfaces parsed observations (requires `Admin` policy).
- `GET /queues/status` — Service Broker queue depth and conversation counts.
- `GET /cycles/history` — Returns the current placeholder history shape (future persistence planned).
- `POST /control/pause`, `/control/resume`, `/control/reset` — Enable/disable queues and cleanup conversations.

### Models (`/api/models`)

- `GET /` — Lists registered models with optional filtering.
- `GET /{modelId:int}` — Retrieves detailed metadata.
- `GET /stats` — Aggregated usage metrics.
- `POST /` — Creates or updates model definitions.
- `POST /{modelId:int}/distill` — Kicks off a distillation job.
- `GET /{modelId:int}/layers` — Retrieves layer metrics from SQL (`ModelLayers`).

### Billing (`/api/billing`)

- `POST /usage/report` — Bulk usage ingestion.
- `POST /calculate` — Produces billing projections.
- `POST /usage/record` — Inserts a single usage event.
- `GET /quota` — Returns tenant quota status.
- `POST /quota` — Updates quota thresholds.

### Feedback (`/api/v1/feedback`)

- `POST /submit` — Accepts feedback entries tied to atoms/inferences.
- `POST /importance/update` — Adjusts importance scores.
- `POST /fine-tune/trigger` — Enqueues a fine-tuning job.
- `POST /summary` — Generates aggregated feedback summaries.

### Operations (`/api/v1/operations`)

- `GET /health` — Performs database and Service Broker checks.
- `POST /indexes/maintenance` — Launches index maintenance job.
- `POST /cache/manage` — Warm/flush caches through background workers.
- `POST /diagnostics` — Executes diagnostic stored procedures.
- `GET /querystore/stats` — Summaries from SQL Query Store.
- `POST /autonomous/trigger` — Triggers autonomous background workflows.
- `GET /metrics` & `GET /metrics/{tenantId}` — Aggregated operational metrics.

### Ingestion (`/api/v1/ingestion`)

- `POST /content` — Wraps atom ingestion for a single payload (`IngestionRequest`) with deduplication support and queue handoff.

### Jobs (`/api/jobs`)

- `GET /{jobId:long}` — Retrieves job status (`JobStatusResponse`).
- `GET /` — Lists jobs with filters/paging.
- `POST /cleanup`, `/index-maintenance`, `/analytics` — Immediately submit background jobs.
- `POST /{jobId:long}/cancel` — Cancels queue jobs when supported.
- `GET /stats` — Job-level aggregation.
- `POST /schedule/cleanup`, `/schedule/analytics` — Schedule future recurring jobs.

### Supporting Endpoints & Configuration Notes

- Health: `/health/startup`, `/health/ready`, `/health/live` (all `AllowAnonymous`).
- Metrics: `/metrics` (enabled when OpenTelemetry exporters are configured).
- Controllers require `HartonomousDb` and/or `DefaultConnection` connection strings; provide both for local testing. Neo4j connectivity relies on `Neo4j:Uri`, `Neo4j:Username`, `Neo4j:Password`. Azure Storage clients expect `AzureStorage:ConnectionString` or service endpoints.
