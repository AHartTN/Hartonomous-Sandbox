# Ingestion API Reference

**Endpoint Prefix**: `/api/ingestion`  
**Authentication**: Required (Bearer token)  
**Rate Limit**: 100 requests/minute per tenant  
**Max File Size**: 1GB per upload  

---

## Overview

The Ingestion API atomizes data from various sources into 64-byte semantic atoms with SHA-256 content-addressable storage. Supports files, URLs, databases, Git repositories, HuggingFace models, and Ollama endpoints.

### Supported Input Types

**Documents**: PDF, DOCX, TXT, MD, HTML, RTF  
**Images**: JPEG, PNG, GIF, BMP, TIFF, WebP  
**Video**: MP4, AVI, MOV, MKV, WebM  
**Audio**: MP3, WAV, FLAC, OGG  
**Archives**: ZIP, TAR, 7Z, RAR (recursive extraction)  
**Code**: C#, Python, JavaScript, Java, Go, Rust, SQL  
**Models**: GGUF, SafeTensors, ONNX, PyTorch, TensorFlow  
**Databases**: SQL Server, PostgreSQL, MySQL  

---

## Endpoints

### 1. File Upload Ingestion

Ingest a single file with automatic format detection and atomization.

**Endpoint**: `POST /api/ingestion/file`

#### Request

**Content-Type**: `multipart/form-data`

**Form Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `file` | File | Yes | File to ingest (max 1GB) |
| `tenantId` | Integer | No | Tenant isolation ID (default: 0) |

#### Response

**Success (200 OK)**:

```json
{
  "jobId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "completed",
  "atoms": {
    "total": 2847,
    "unique": 1923,
    "deduplicationRate": 32.5
  },
  "durationMs": 4582,
  "childJobs": ["child-job-1", "child-job-2"]
}
```

**Error (400 Bad Request)**:

```json
{
  "error": "No file provided"
}
```

**Error (500 Internal Server Error)**:

```json
{
  "jobId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "failed",
  "error": "Unsupported file format: .xyz"
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `jobId` | String | Unique job identifier for tracking |
| `status` | String | `processing`, `completed`, `failed` |
| `atoms.total` | Integer | Total atoms created (including duplicates) |
| `atoms.unique` | Integer | Unique atoms stored (after deduplication) |
| `atoms.deduplicationRate` | Float | Percentage of duplicates found |
| `durationMs` | Integer | Processing time in milliseconds |
| `childJobs` | String[] | Child job IDs for recursive extraction (archives) |

#### Example cURL Request

```bash
curl -X POST "https://api.hartonomous.ai/api/ingestion/file" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@document.pdf" \
  -F "tenantId=1"
```

#### Processing Pipeline

1. **File Type Detection**: Magic number + extension analysis
2. **Atomizer Selection**: Priority-based atomizer matching
3. **Content Extraction**: Format-specific parsing
4. **Atomization**: 64-byte chunks with SHA-256 hashing
5. **Deduplication**: Content-addressable storage (CAS)
6. **Composition Tracking**: Parent-child relationships
7. **Spatial Indexing**: 1536D → 3D projection + R-Tree

---

### 2. URL Ingestion

Fetch and atomize content from a web URL.

**Endpoint**: `POST /api/ingestion/url`

#### Request

**Content-Type**: `application/json`

**Body**:

```json
{
  "url": "https://example.com/article",
  "tenantId": 1
}
```

**Request Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `url` | String | Yes | Valid HTTP/HTTPS URL |
| `tenantId` | Integer | No | Tenant isolation ID (default: 0) |

#### Response

Same structure as file upload ingestion.

#### Example cURL Request

```bash
curl -X POST "https://api.hartonomous.ai/api/ingestion/url" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://example.com/article",
    "tenantId": 1
  }'
```

#### Use Cases

- Web scraping and knowledge base ingestion
- RSS feed processing
- API endpoint data capture
- Documentation crawling

---

### 3. Database Ingestion

Atomize database schema and row data.

**Endpoint**: `POST /api/ingestion/database`

#### Request

**Content-Type**: `application/json`

**Body**:

```json
{
  "connectionString": "Server=localhost;Database=MyDB;Trusted_Connection=True;",
  "tenantId": 1,
  "maxTables": 50,
  "maxRowsPerTable": 1000
}
```

**Request Fields**:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `connectionString` | String | Yes | - | Database connection string |
| `tenantId` | Integer | No | 0 | Tenant isolation ID |
| `maxTables` | Integer | No | 50 | Limit on tables to process |
| `maxRowsPerTable` | Integer | No | 1000 | Limit on rows per table |

#### Response

Same structure as file upload ingestion.

#### Example cURL Request

```bash
curl -X POST "https://api.hartonomous.ai/api/ingestion/database" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "connectionString": "Server=localhost;Database=MyDB;Integrated Security=True;",
    "maxTables": 20,
    "maxRowsPerTable": 500
  }'
```

#### Supported Databases

- SQL Server (2016+)
- PostgreSQL (10+)
- MySQL (8.0+)

#### Security Considerations

- Use read-only credentials
- Enable SSL/TLS connections
- Store connection strings in Azure Key Vault
- Audit all database access

---

### 4. Git Repository Ingestion

Atomize Git repository metadata, commit history, and file contents.

**Endpoint**: `POST /api/ingestion/git`

#### Request

**Content-Type**: `application/json`

**Body**:

```json
{
  "repositoryPath": "/path/to/repo",
  "tenantId": 1,
  "maxBranches": 50,
  "maxCommits": 100,
  "maxFiles": 1000,
  "includeFileHistory": true
}
```

**Request Fields**:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `repositoryPath` | String | Yes | - | Local path to Git repository |
| `tenantId` | Integer | No | 0 | Tenant isolation ID |
| `maxBranches` | Integer | No | 50 | Limit on branches to process |
| `maxCommits` | Integer | No | 100 | Limit on commits per branch |
| `maxFiles` | Integer | No | 1000 | Limit on files to atomize |
| `includeFileHistory` | Boolean | No | true | Track file changes across commits |

#### Response

Same structure as file upload ingestion.

#### Example cURL Request

```bash
curl -X POST "https://api.hartonomous.ai/api/ingestion/git" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryPath": "/home/user/myproject",
    "maxBranches": 10,
    "maxCommits": 200,
    "includeFileHistory": true
  }'
```

#### Use Cases

- Code repository analysis
- Commit history tracking
- Developer contribution patterns
- Provenance tracking for code changes

---

### 5. HuggingFace Model Ingestion

Ingest AI models from HuggingFace Hub with model atomization.

**Endpoint**: `POST /api/ingestion/huggingface`

#### Request

**Content-Type**: `application/json`

**Body**:

```json
{
  "modelId": "meta-llama/Llama-2-7b-hf",
  "tenantId": 1,
  "includeFiles": ["*.safetensors", "config.json", "tokenizer.json"],
  "atomizeWeights": true
}
```

**Request Fields**:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `modelId` | String | Yes | - | HuggingFace model ID (`owner/repo`) |
| `tenantId` | Integer | No | 0 | Tenant isolation ID |
| `includeFiles` | String[] | No | `["*"]` | File patterns to download |
| `atomizeWeights` | Boolean | No | true | Atomize model weights into tensors |

#### Response

```json
{
  "jobId": "model-job-12345",
  "status": "completed",
  "atoms": {
    "total": 291,
    "unique": 247,
    "deduplicationRate": 15.1
  },
  "modelMetadata": {
    "format": "SafeTensors",
    "parameters": "7B",
    "architecture": "llama",
    "tensors": 291
  },
  "durationMs": 142000
}
```

#### Example cURL Request

```bash
curl -X POST "https://api.hartonomous.ai/api/ingestion/huggingface" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "modelId": "meta-llama/Llama-2-7b-hf",
    "includeFiles": ["*.safetensors", "config.json"],
    "atomizeWeights": true
  }'
```

#### Model Atomization

1. **Download**: Fetch model files from HuggingFace
2. **Parse**: Extract tensor metadata (GGUF, SafeTensors, ONNX)
3. **Chunk**: Split tensors into atoms (configurable size)
4. **Hash**: SHA-256 content-addressable storage
5. **Deduplicate**: Shared weights across layers
6. **Spatialize**: 3D projection + spatial indexing

**Storage Reduction**: ~65% via deduplication (typical for transformer models)

---

### 6. Ollama Model Ingestion

Ingest models from local Ollama instance.

**Endpoint**: `POST /api/ingestion/ollama`

#### Request

**Content-Type**: `application/json`

**Body**:

```json
{
  "modelName": "llama2:7b",
  "ollamaEndpoint": "http://localhost:11434",
  "tenantId": 1,
  "atomizeWeights": true
}
```

**Request Fields**:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `modelName` | String | Yes | - | Ollama model name (`model:tag`) |
| `ollamaEndpoint` | String | No | `http://localhost:11434` | Ollama API endpoint |
| `tenantId` | Integer | No | 0 | Tenant isolation ID |
| `atomizeWeights` | Boolean | No | true | Atomize model weights |

#### Response

Same structure as HuggingFace ingestion.

#### Example cURL Request

```bash
curl -X POST "https://api.hartonomous.ai/api/ingestion/ollama" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "modelName": "llama2:7b",
    "atomizeWeights": true
  }'
```

---

### 7. Job Status Query

Retrieve ingestion job status and progress.

**Endpoint**: `GET /api/ingestion/jobs/{jobId}`

#### Request

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `jobId` | String | Job ID from ingestion response |

#### Response

```json
{
  "jobId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "fileName": "document.pdf",
  "status": "completed",
  "detectedType": "application/pdf",
  "detectedCategory": "Document",
  "atoms": {
    "total": 2847,
    "unique": 1923
  },
  "startedAt": "2025-01-28T12:34:56Z",
  "completedAt": "2025-01-28T12:35:01Z",
  "durationMs": 4582,
  "childJobs": ["child-job-1", "child-job-2"],
  "error": null
}
```

#### Example cURL Request

```bash
curl -X GET "https://api.hartonomous.ai/api/ingestion/jobs/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

### 8. Atom Query by Hash

Query atoms by content hash (SHA-256).

**Endpoint**: `GET /api/ingestion/atoms?hash={sha256}`

#### Request

**Query Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `hash` | String | Yes | SHA-256 hash (hex encoded) |

#### Response

```json
{
  "hash": "a3f5b2c4d1e678...",
  "atomId": 12345,
  "atomType": "Tensor",
  "sizeBytes": 16384,
  "referenceCount": 3,
  "createdAt": "2025-01-28T12:00:00Z",
  "lastAccessed": "2025-01-28T14:30:00Z"
}
```

#### Example cURL Request

```bash
curl -X GET "https://api.hartonomous.ai/api/ingestion/atoms?hash=a3f5b2c4d1e678..." \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## Error Codes

### HTTP Status Codes

| Code | Meaning | Description |
|------|---------|-------------|
| 200 | OK | Request successful |
| 400 | Bad Request | Invalid parameters or file format |
| 401 | Unauthorized | Missing or invalid authentication token |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Job ID or resource not found |
| 413 | Payload Too Large | File exceeds 1GB limit |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Atomization or storage failure |
| 503 | Service Unavailable | System overload or maintenance |

### Custom Error Codes

| Code | Message | Resolution |
|------|---------|------------|
| `E1001` | Unsupported file format | Check supported formats list |
| `E1002` | Atomization failed | Verify file integrity |
| `E1003` | Deduplication error | Contact support |
| `E1004` | Spatial indexing failed | Retry after 1 minute |
| `E1005` | Provenance tracking error | Check Neo4j connectivity |

---

## Authentication & Authorization

### Bearer Token Authentication

Include authentication token in request header:

```
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Token Acquisition

**Azure Entra ID (Internal)**:

```bash
curl -X POST "https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id={client_id}" \
  -d "scope=api://hartonomous/.default" \
  -d "client_secret={client_secret}" \
  -d "grant_type=client_credentials"
```

**External ID (Public)**:

```bash
curl -X POST "https://login.hartonomous.ai/oauth2/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id={client_id}" \
  -d "username={email}" \
  -d "password={password}" \
  -d "grant_type=password"
```

### Role-Based Access Control

| Role | Permissions |
|------|-------------|
| **Admin** | Full access (all endpoints) |
| **Analyst** | Read + write (ingestion, query) |
| **User** | Read only (query endpoints) |

---

## Rate Limiting

### Limits by Endpoint

| Endpoint | Limit | Window |
|----------|-------|--------|
| File Upload | 10 requests | 1 minute |
| URL Ingestion | 100 requests | 1 minute |
| Database Ingestion | 5 requests | 1 minute |
| Job Status | 1000 requests | 1 minute |

### Rate Limit Headers

Response includes rate limit information:

```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 87
X-RateLimit-Reset: 1706457600
```

### Exceeding Limits

**Response (429 Too Many Requests)**:

```json
{
  "error": "Rate limit exceeded",
  "retryAfter": 42,
  "limit": 100,
  "window": "1 minute"
}
```

---

## Batch Operations

### Batch File Upload

Upload multiple files in a single request.

**Endpoint**: `POST /api/ingestion/batch`

#### Request

**Content-Type**: `multipart/form-data`

**Form Parameters**:

- `files[]`: Multiple files (max 10 files, 1GB total)
- `tenantId`: Tenant ID (optional)

#### Response

```json
{
  "batchId": "batch-12345",
  "status": "processing",
  "jobs": [
    {
      "jobId": "job-1",
      "fileName": "file1.pdf",
      "status": "processing"
    },
    {
      "jobId": "job-2",
      "fileName": "file2.docx",
      "status": "queued"
    }
  ]
}
```

#### Example cURL Request

```bash
curl -X POST "https://api.hartonomous.ai/api/ingestion/batch" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: multipart/form-data" \
  -F "files[]=@file1.pdf" \
  -F "files[]=@file2.docx" \
  -F "files[]=@file3.txt" \
  -F "tenantId=1"
```

---

## Advanced Features

### Archive Recursive Extraction

ZIP, TAR, 7Z archives are automatically extracted and processed recursively.

**Example**:

1. Upload `project.zip` containing:
   - `README.md`
   - `src/code.py`
   - `docs.tar.gz` (nested archive)
2. System creates parent job for `project.zip`
3. Child jobs created for each file
4. Nested `docs.tar.gz` triggers grandchild jobs
5. All jobs tracked via `childJobs` field

### Model Weight Atomization

Transformer models atomized into tensor weights with deduplication:

**GGUF Format**:
- Parse header metadata
- Extract 291 tensors for Llama-2-7B
- Hash each tensor (SHA-256)
- Deduplicate shared weights
- Store with spatial indexing

**SafeTensors Format**:
- Parse JSON header
- Stream tensor data
- Chunk large tensors
- Hash and deduplicate
- Link to original model

**Storage Reduction**: ~65% via content-addressable deduplication

---

## Performance Optimization

### Parallel Processing

Ingestion jobs processed in parallel with configurable worker count:

```json
{
  "workerThreads": 8,
  "maxConcurrentJobs": 50
}
```

### Streaming Mode

Large files (>100MB) processed in streaming mode:

- Reduces memory usage
- Enables real-time progress tracking
- Supports cancellation mid-stream

### Spatial Index Caching

Frequently accessed atoms cached in memory:

- LRU eviction policy
- 10GB default cache size
- 67% hit rate (typical)

---

## SDK Examples

### C# SDK

```csharp
using Hartonomous.Client;

var client = new HartonomousClient("https://api.hartonomous.ai", "YOUR_TOKEN");

// Upload file
var result = await client.Ingestion.UploadFileAsync(
    new FileUploadRequest
    {
        FilePath = "document.pdf",
        TenantId = 1
    });

Console.WriteLine($"Job ID: {result.JobId}");
Console.WriteLine($"Atoms created: {result.Atoms.Total}");
Console.WriteLine($"Deduplication: {result.Atoms.DeduplicationRate}%");

// Poll job status
var status = await client.Ingestion.GetJobStatusAsync(result.JobId);
while (status.Status == "processing")
{
    await Task.Delay(1000);
    status = await client.Ingestion.GetJobStatusAsync(result.JobId);
}

Console.WriteLine($"Final status: {status.Status}");
```

### Python SDK

```python
from hartonomous import HartonomousClient

client = HartonomousClient(
    base_url="https://api.hartonomous.ai",
    token="YOUR_TOKEN"
)

# Upload file
result = client.ingestion.upload_file(
    file_path="document.pdf",
    tenant_id=1
)

print(f"Job ID: {result['jobId']}")
print(f"Atoms created: {result['atoms']['total']}")
print(f"Deduplication: {result['atoms']['deduplicationRate']}%")

# Poll job status
import time
status = client.ingestion.get_job_status(result['jobId'])
while status['status'] == 'processing':
    time.sleep(1)
    status = client.ingestion.get_job_status(result['jobId'])

print(f"Final status: {status['status']}")
```

### JavaScript/TypeScript SDK

```typescript
import { HartonomousClient } from '@hartonomous/client';

const client = new HartonomousClient({
  baseUrl: 'https://api.hartonomous.ai',
  token: 'YOUR_TOKEN'
});

// Upload file
const result = await client.ingestion.uploadFile({
  file: new File([fileBuffer], 'document.pdf'),
  tenantId: 1
});

console.log(`Job ID: ${result.jobId}`);
console.log(`Atoms created: ${result.atoms.total}`);
console.log(`Deduplication: ${result.atoms.deduplicationRate}%`);

// Poll job status
let status = await client.ingestion.getJobStatus(result.jobId);
while (status.status === 'processing') {
  await new Promise(resolve => setTimeout(resolve, 1000));
  status = await client.ingestion.getJobStatus(result.jobId);
}

console.log(`Final status: ${status.status}`);
```

---

## Troubleshooting

### Common Issues

**1. "Unsupported file format" error**

- Verify file extension matches content
- Check magic number detection
- Convert to supported format (e.g., PyTorch → SafeTensors)

**2. "Atomization failed" error**

- Verify file integrity (checksum)
- Check file size (max 1GB)
- Inspect logs for specific parser errors

**3. Slow ingestion performance**

- Enable streaming mode for large files
- Increase worker thread count
- Check network bandwidth for URL ingestion

**4. High deduplication rate (>90%)**

- Expected for repeated content
- Validates content-addressable storage
- Not an error condition

### Debug Mode

Enable verbose logging:

```bash
curl -X POST "https://api.hartonomous.ai/api/ingestion/file" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "X-Debug-Mode: true" \
  -F "file=@document.pdf"
```

Response includes detailed trace:

```json
{
  "jobId": "...",
  "debug": {
    "fileTypeDetection": "PDF via magic number 0x255044462D",
    "atomizerSelected": "PdfAtomizer (priority: 100)",
    "parsingTime": 1247,
    "hashingTime": 823,
    "deduplicationTime": 412,
    "spatialIndexingTime": 1100
  }
}
```

---

## Related Documentation

- [Query API](query.md) - Semantic search and spatial queries
- [Reasoning API](reasoning.md) - Chain-of-Thought and Tree-of-Thought
- [Provenance API](provenance.md) - Atom lineage and Merkle DAG
- [Streaming API](streaming.md) - Real-time telemetry and video streams
