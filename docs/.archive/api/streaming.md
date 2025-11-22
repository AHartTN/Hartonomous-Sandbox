# Streaming API Reference

**Endpoint Prefix**: `/api/streaming`  
**Authentication**: Required (Bearer token)  
**Rate Limit**: 1000 requests/minute per tenant  
**Response Time**: Variable (depends on stream duration)  

---

## Overview

The Streaming API provides **real-time data ingestion** and **long-running operation monitoring** through **Server-Sent Events (SSE)**, **WebSockets**, and **polling**. It supports streaming telemetry data, video frames, audio buffers, and job progress tracking.

### Core Features

**Server-Sent Events (SSE)**: One-way streaming from server to client  
**WebSocket Support**: Bidirectional real-time communication  
**Job Status Polling**: HTTP-based progress tracking  
**Session Management**: Organize streams into logical sessions  
**Batch Processing**: Efficient handling of high-volume data  

---

## Endpoints

### 1. Start Streaming Session

Initialize a streaming session for telemetry, video, or audio data.

**Endpoint**: `POST /api/streaming/sessions/start`

#### Request

**Content-Type**: `application/json`

**Body**:

```json
{
  "sessionType": "telemetry",
  "sessionName": "IoT Sensor Stream 2025-01-20",
  "metadata": {
    "deviceId": "sensor-12345",
    "location": "Manufacturing Floor A",
    "samplingRateHz": 100
  },
  "batchSize": 1000,
  "maxBatchDelayMs": 5000,
  "tenantId": 1
}
```

**Request Fields**:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `sessionType` | String | Yes | - | Stream type (`telemetry`, `video`, `audio`) |
| `sessionName` | String | No | Auto-generated | Human-readable session name |
| `metadata` | Object | No | {} | Session metadata |
| `batchSize` | Integer | No | 1000 | Records per batch |
| `maxBatchDelayMs` | Integer | No | 5000 | Maximum batch delay (milliseconds) |
| `tenantId` | Integer | No | 0 | Tenant isolation ID |

**Session Types**:

- `telemetry`: Time-series sensor data
- `video`: Video frame sequences
- `audio`: Audio buffer streams

#### Response

**Success (200 OK)**:

```json
{
  "sessionId": "stream-session-abc123",
  "sessionType": "telemetry",
  "sessionName": "IoT Sensor Stream 2025-01-20",
  "status": "active",
  "createdAt": "2025-01-20T10:30:00Z",
  "uploadUrl": "https://api.hartonomous.ai/api/streaming/sessions/stream-session-abc123/upload",
  "statusUrl": "https://api.hartonomous.ai/api/streaming/sessions/stream-session-abc123/status",
  "sseUrl": "https://api.hartonomous.ai/api/streaming/sessions/stream-session-abc123/events",
  "websocketUrl": "wss://api.hartonomous.ai/api/streaming/sessions/stream-session-abc123/ws"
}
```

#### Example cURL Request

```bash
curl -X POST "https://api.hartonomous.ai/api/streaming/sessions/start" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "sessionType": "telemetry",
    "sessionName": "IoT Sensor Stream 2025-01-20",
    "batchSize": 1000
  }'
```

---

### 2. Upload Stream Data

Upload data to an active streaming session.

**Endpoint**: `POST /api/streaming/sessions/{sessionId}/upload`

#### Request (Telemetry)

**Content-Type**: `application/json`

**Body**:

```json
{
  "records": [
    {
      "timestamp": "2025-01-20T10:30:00.000Z",
      "values": {
        "temperature": 72.5,
        "humidity": 45.2,
        "pressure": 1013.25
      }
    },
    {
      "timestamp": "2025-01-20T10:30:00.010Z",
      "values": {
        "temperature": 72.6,
        "humidity": 45.1,
        "pressure": 1013.26
      }
    }
  ]
}
```

#### Request (Video Frames)

**Content-Type**: `application/json`

**Body**:

```json
{
  "frames": [
    {
      "frameNumber": 1,
      "timestamp": "2025-01-20T10:30:00.000Z",
      "imageData": "base64-encoded-jpeg-data...",
      "format": "jpeg",
      "width": 1920,
      "height": 1080
    },
    {
      "frameNumber": 2,
      "timestamp": "2025-01-20T10:30:00.033Z",
      "imageData": "base64-encoded-jpeg-data...",
      "format": "jpeg",
      "width": 1920,
      "height": 1080
    }
  ]
}
```

#### Request (Audio Buffers)

**Content-Type**: `application/json`

**Body**:

```json
{
  "buffers": [
    {
      "bufferNumber": 1,
      "timestamp": "2025-01-20T10:30:00.000Z",
      "audioData": "base64-encoded-wav-data...",
      "format": "wav",
      "sampleRate": 44100,
      "channels": 2,
      "bitDepth": 16
    }
  ]
}
```

#### Response

```json
{
  "sessionId": "stream-session-abc123",
  "recordsReceived": 2,
  "recordsProcessed": 2,
  "recordsQueued": 0,
  "batchId": "batch-001",
  "processingTimeMs": 45,
  "status": "active"
}
```

---

### 3. Server-Sent Events (SSE)

Stream real-time progress updates via Server-Sent Events.

**Endpoint**: `GET /api/streaming/sessions/{sessionId}/events`

#### Request

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `sessionId` | String | Streaming session ID |

#### SSE Stream

**Connection**:

```bash
curl -N -H "Authorization: Bearer YOUR_TOKEN" \
  "https://api.hartonomous.ai/api/streaming/sessions/stream-session-abc123/events"
```

**Event Stream**:

```
event: session-started
data: {"sessionId":"stream-session-abc123","timestamp":"2025-01-20T10:30:00Z"}

event: batch-received
data: {"batchId":"batch-001","recordCount":1000,"timestamp":"2025-01-20T10:30:05Z"}

event: batch-processed
data: {"batchId":"batch-001","atomsCreated":1000,"processingTimeMs":120,"timestamp":"2025-01-20T10:30:07Z"}

event: progress
data: {"totalRecordsReceived":5000,"totalAtomsCreated":5000,"elapsedMs":25000,"timestamp":"2025-01-20T10:30:25Z"}

event: session-completed
data: {"sessionId":"stream-session-abc123","totalRecords":10000,"totalAtoms":10000,"durationMs":50000,"timestamp":"2025-01-20T10:31:00Z"}
```

**Event Types**:

| Event | Description | Data Fields |
|-------|-------------|-------------|
| `session-started` | Session initialized | `sessionId`, `timestamp` |
| `batch-received` | Batch uploaded | `batchId`, `recordCount` |
| `batch-processed` | Batch atomized | `batchId`, `atomsCreated`, `processingTimeMs` |
| `progress` | Progress update | `totalRecordsReceived`, `totalAtomsCreated`, `elapsedMs` |
| `error` | Processing error | `errorCode`, `message`, `batchId` |
| `session-completed` | Session ended | `totalRecords`, `totalAtoms`, `durationMs` |

---

### 4. WebSocket Connection

Establish bidirectional real-time connection.

**Endpoint**: `WSS /api/streaming/sessions/{sessionId}/ws`

#### WebSocket Protocol

**Connection**:

```javascript
const ws = new WebSocket('wss://api.hartonomous.ai/api/streaming/sessions/stream-session-abc123/ws', {
  headers: {
    'Authorization': 'Bearer YOUR_TOKEN'
  }
});

ws.onopen = () => {
  console.log('WebSocket connected');
};

ws.onmessage = (event) => {
  const message = JSON.parse(event.data);
  console.log('Received:', message);
};

// Send telemetry data
ws.send(JSON.stringify({
  type: 'telemetry',
  records: [
    {
      timestamp: new Date().toISOString(),
      values: { temperature: 72.5 }
    }
  ]
}));
```

**Message Types**:

**Client → Server**:

```json
{
  "type": "telemetry",
  "records": [...]
}
```

**Server → Client**:

```json
{
  "type": "batch-processed",
  "batchId": "batch-001",
  "atomsCreated": 1000,
  "processingTimeMs": 120
}
```

---

### 5. Job Status Polling

Poll job status for long-running operations.

**Endpoint**: `GET /api/streaming/jobs/{jobId}/status`

#### Request

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `jobId` | String | Job ID from ingestion request |

#### Response

**In Progress (200 OK)**:

```json
{
  "jobId": "job-xyz789",
  "status": "in-progress",
  "progress": {
    "currentStep": "atomization",
    "totalSteps": 5,
    "percentComplete": 60,
    "itemsProcessed": 6000,
    "totalItems": 10000,
    "estimatedTimeRemainingMs": 8000
  },
  "startedAt": "2025-01-20T10:30:00Z",
  "lastUpdatedAt": "2025-01-20T10:30:12Z",
  "statusUrl": "https://api.hartonomous.ai/api/streaming/jobs/job-xyz789/status"
}
```

**Completed (200 OK)**:

```json
{
  "jobId": "job-xyz789",
  "status": "completed",
  "result": {
    "atomsCreated": 10000,
    "totalDurationMs": 20000,
    "finalHash": "a3f5b8c9d2e1f4a7b6c5d8e9f2a1b4c7d6e5f8a9b2c1d4e7f6a5b8c9d2e1f4a7"
  },
  "startedAt": "2025-01-20T10:30:00Z",
  "completedAt": "2025-01-20T10:30:20Z"
}
```

**Failed (200 OK)**:

```json
{
  "jobId": "job-xyz789",
  "status": "failed",
  "error": {
    "code": "ATOMIZATION_ERROR",
    "message": "Failed to parse video frame 1234",
    "details": "Invalid JPEG format",
    "failedAt": "2025-01-20T10:30:15Z"
  },
  "partialResult": {
    "atomsCreated": 1233,
    "lastSuccessfulFrame": 1233
  },
  "startedAt": "2025-01-20T10:30:00Z",
  "failedAt": "2025-01-20T10:30:15Z"
}
```

#### Example cURL Request

```bash
curl -X GET "https://api.hartonomous.ai/api/streaming/jobs/job-xyz789/status" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

### 6. Cancel Job

Cancel a running job.

**Endpoint**: `POST /api/streaming/jobs/{jobId}/cancel`

#### Request

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `jobId` | String | Job ID to cancel |

#### Response

```json
{
  "jobId": "job-xyz789",
  "status": "cancelled",
  "cancelledAt": "2025-01-20T10:30:18Z",
  "partialResult": {
    "atomsCreated": 7500,
    "itemsProcessed": 7500
  }
}
```

---

### 7. Session Status

Get current status of a streaming session.

**Endpoint**: `GET /api/streaming/sessions/{sessionId}/status`

#### Request

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `sessionId` | String | Streaming session ID |

#### Response

```json
{
  "sessionId": "stream-session-abc123",
  "sessionType": "telemetry",
  "sessionName": "IoT Sensor Stream 2025-01-20",
  "status": "active",
  "statistics": {
    "totalRecordsReceived": 15000,
    "totalBatchesReceived": 15,
    "totalAtomsCreated": 15000,
    "averageBatchProcessingTimeMs": 120,
    "dataReceivedBytes": 2500000,
    "sessionDurationMs": 75000
  },
  "createdAt": "2025-01-20T10:30:00Z",
  "lastActivityAt": "2025-01-20T10:31:15Z"
}
```

---

### 8. End Streaming Session

Close a streaming session and finalize processing.

**Endpoint**: `POST /api/streaming/sessions/{sessionId}/end`

#### Request

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `sessionId` | String | Streaming session ID |

**Body** (optional):

```json
{
  "waitForCompletion": true,
  "timeoutMs": 30000
}
```

#### Response

```json
{
  "sessionId": "stream-session-abc123",
  "status": "completed",
  "finalStatistics": {
    "totalRecordsReceived": 20000,
    "totalBatchesReceived": 20,
    "totalAtomsCreated": 20000,
    "totalDurationMs": 100000,
    "averageThroughputRecordsPerSecond": 200
  },
  "completedAt": "2025-01-20T10:31:40Z"
}
```

---

## Advanced Features

### Batch Processing

Optimize throughput with batch processing:

**Configuration**:

```json
{
  "batchSize": 1000,
  "maxBatchDelayMs": 5000,
  "maxConcurrentBatches": 5
}
```

**Behavior**:

- **Batch Size**: Buffer up to N records before processing
- **Max Delay**: Process batch after N milliseconds even if not full
- **Concurrency**: Process multiple batches in parallel

**Performance**:

- **Single Record**: 10ms latency, 100 records/sec
- **Batch (1000)**: 120ms latency, 8,333 records/sec
- **Speedup**: 83× throughput improvement

---

### Progress Tracking

Track detailed progress for long-running operations:

**Progress Phases**:

1. **Upload**: Data transfer to server
2. **Validation**: Format and schema validation
3. **Atomization**: Content parsing and atom creation
4. **Indexing**: Spatial and vector indexing
5. **Completion**: Finalization and cleanup

**Progress Updates**:

```json
{
  "currentPhase": "atomization",
  "phaseProgress": {
    "upload": { "status": "completed", "durationMs": 2000 },
    "validation": { "status": "completed", "durationMs": 500 },
    "atomization": { "status": "in-progress", "percentComplete": 60, "estimatedRemainingMs": 8000 },
    "indexing": { "status": "pending" },
    "completion": { "status": "pending" }
  }
}
```

---

### Retry Logic

Automatic retry for failed batches:

**Retry Configuration**:

```json
{
  "maxRetries": 3,
  "retryDelayMs": 1000,
  "retryBackoffMultiplier": 2.0
}
```

**Retry Schedule**:

- **Attempt 1**: Immediate
- **Attempt 2**: 1 second delay
- **Attempt 3**: 2 second delay
- **Attempt 4**: 4 second delay

**Dead Letter Queue**: Failed batches after max retries moved to DLQ for manual review.

---

### Session Lifecycle

**Session States**:

1. **Initializing**: Session created, not yet active
2. **Active**: Accepting data uploads
3. **Paused**: Temporarily stopped (resumable)
4. **Completing**: Finalizing processing
5. **Completed**: All data processed successfully
6. **Failed**: Unrecoverable error occurred
7. **Cancelled**: User-initiated cancellation

**State Transitions**:

```
Initializing → Active → Completing → Completed
             ↓         ↓            ↓
           Paused    Failed      Cancelled
```

---

### Streaming Telemetry Schema

Define custom schemas for telemetry data:

**Schema Definition**:

```json
{
  "schemaName": "iot-sensors-v1",
  "schemaVersion": "1.0.0",
  "fields": [
    {
      "name": "timestamp",
      "type": "datetime",
      "required": true
    },
    {
      "name": "temperature",
      "type": "float",
      "required": true,
      "validation": {
        "min": -50.0,
        "max": 150.0
      }
    },
    {
      "name": "humidity",
      "type": "float",
      "required": true,
      "validation": {
        "min": 0.0,
        "max": 100.0
      }
    }
  ]
}
```

**Validation**: Incoming records validated against schema, invalid records rejected.

---

### Video Frame Extraction

Automatically extract frames from video streams:

**Configuration**:

```json
{
  "frameExtractionMode": "keyframes",
  "frameRateFps": 30,
  "resolution": {
    "width": 1920,
    "height": 1080
  },
  "compressionQuality": 85
}
```

**Extraction Modes**:

- `all-frames`: Extract every frame
- `keyframes`: Extract only keyframes (I-frames)
- `interval`: Extract every Nth frame
- `scene-changes`: Extract frames at scene boundaries

---

### Audio Segmentation

Segment audio streams into meaningful chunks:

**Configuration**:

```json
{
  "segmentationMode": "silence",
  "silenceThresholdDb": -40,
  "minSegmentDurationMs": 1000,
  "maxSegmentDurationMs": 30000
}
```

**Segmentation Modes**:

- `fixed-duration`: Fixed-length segments
- `silence`: Split on silence detection
- `energy`: Split on energy level changes
- `speech-detection`: Split on speech activity

---

## Error Handling

### Session Not Found

```json
{
  "error": "Session not found",
  "sessionId": "stream-session-invalid",
  "message": "No active session found with this ID",
  "suggestions": [
    "Verify session ID is correct",
    "Check if session expired (max 24 hours)",
    "Start a new session"
  ]
}
```

### Batch Size Exceeded

```json
{
  "error": "Batch size exceeded",
  "maxBatchSize": 10000,
  "receivedBatchSize": 15000,
  "message": "Batch exceeds maximum allowed size",
  "suggestion": "Split data into smaller batches"
}
```

### Rate Limit Exceeded

```json
{
  "error": "Rate limit exceeded",
  "limit": 1000,
  "window": "1 minute",
  "retryAfter": 45,
  "message": "Too many requests to streaming API"
}
```

### Invalid Data Format

```json
{
  "error": "Invalid data format",
  "batchId": "batch-001",
  "invalidRecords": [
    {
      "recordIndex": 42,
      "field": "temperature",
      "value": "invalid",
      "expectedType": "float",
      "message": "Cannot parse 'invalid' as float"
    }
  ],
  "suggestion": "Fix invalid records and resubmit batch"
}
```

---

## SDK Examples

### C# SDK

```csharp
using Hartonomous.Client;
using System.Net.Http.Headers;

var client = new HartonomousClient("https://api.hartonomous.ai", "YOUR_TOKEN");

// Start streaming session
var session = await client.Streaming.StartSessionAsync(new StartSessionRequest
{
    SessionType = SessionType.Telemetry,
    SessionName = "IoT Sensor Stream",
    BatchSize = 1000
});

Console.WriteLine($"Session started: {session.SessionId}");

// Upload telemetry data
var records = new List<TelemetryRecord>
{
    new TelemetryRecord
    {
        Timestamp = DateTime.UtcNow,
        Values = new Dictionary<string, object>
        {
            { "temperature", 72.5 },
            { "humidity", 45.2 }
        }
    }
};

var uploadResult = await client.Streaming.UploadDataAsync(session.SessionId, new UploadRequest
{
    Records = records
});

Console.WriteLine($"Uploaded {uploadResult.RecordsProcessed} records");

// Subscribe to SSE events
await foreach (var evt in client.Streaming.SubscribeToEventsAsync(session.SessionId))
{
    Console.WriteLine($"Event: {evt.EventType} - {evt.Data}");
    
    if (evt.EventType == "session-completed")
        break;
}

// End session
var finalStats = await client.Streaming.EndSessionAsync(session.SessionId);
Console.WriteLine($"Total atoms created: {finalStats.FinalStatistics.TotalAtomsCreated}");
```

### Python SDK

```python
from hartonomous import HartonomousClient
import asyncio

client = HartonomousClient(
    base_url="https://api.hartonomous.ai",
    token="YOUR_TOKEN"
)

# Start streaming session
session = client.streaming.start_session(
    session_type="telemetry",
    session_name="IoT Sensor Stream",
    batch_size=1000
)

print(f"Session started: {session['sessionId']}")

# Upload telemetry data
records = [
    {
        "timestamp": "2025-01-20T10:30:00Z",
        "values": {
            "temperature": 72.5,
            "humidity": 45.2
        }
    }
]

upload_result = client.streaming.upload_data(
    session_id=session['sessionId'],
    records=records
)

print(f"Uploaded {upload_result['recordsProcessed']} records")

# Subscribe to SSE events
async for event in client.streaming.subscribe_to_events(session['sessionId']):
    print(f"Event: {event['eventType']} - {event['data']}")
    
    if event['eventType'] == 'session-completed':
        break

# End session
final_stats = client.streaming.end_session(session['sessionId'])
print(f"Total atoms created: {final_stats['finalStatistics']['totalAtomsCreated']}")
```

### JavaScript/TypeScript SDK

```typescript
import { HartonomousClient } from '@hartonomous/client';

const client = new HartonomousClient({
  baseUrl: 'https://api.hartonomous.ai',
  token: 'YOUR_TOKEN'
});

// Start streaming session
const session = await client.streaming.startSession({
  sessionType: 'telemetry',
  sessionName: 'IoT Sensor Stream',
  batchSize: 1000
});

console.log(`Session started: ${session.sessionId}`);

// Upload telemetry data
const records = [
  {
    timestamp: new Date().toISOString(),
    values: {
      temperature: 72.5,
      humidity: 45.2
    }
  }
];

const uploadResult = await client.streaming.uploadData(session.sessionId, {
  records
});

console.log(`Uploaded ${uploadResult.recordsProcessed} records`);

// Subscribe to SSE events
const eventStream = client.streaming.subscribeToEvents(session.sessionId);

for await (const event of eventStream) {
  console.log(`Event: ${event.eventType} - ${JSON.stringify(event.data)}`);
  
  if (event.eventType === 'session-completed')
    break;
}

// End session
const finalStats = await client.streaming.endSession(session.sessionId);
console.log(`Total atoms created: ${finalStats.finalStatistics.totalAtomsCreated}`);
```

---

## Performance Benchmarks

### Throughput

| Data Type | Batch Size | Throughput | Latency |
|-----------|------------|------------|---------|
| Telemetry | 100 | 1,000 rec/sec | 100ms |
| Telemetry | 1,000 | 8,333 rec/sec | 120ms |
| Telemetry | 10,000 | 50,000 rec/sec | 200ms |
| Video Frames | 10 | 300 frames/sec | 33ms |
| Video Frames | 100 | 2,000 frames/sec | 50ms |
| Audio Buffers | 10 | 500 buffers/sec | 20ms |

### Scaling

**Single Node**: 10,000 records/second  
**Cluster (5 nodes)**: 50,000 records/second  
**Auto-scaling**: Up to 200,000 records/second  

---

## Related Documentation

- [Ingestion API](ingestion.md) - Data ingestion and atomization
- [Query API](query.md) - Semantic search and spatial queries
- [Reasoning API](reasoning.md) - Chain-of-Thought and Tree-of-Thought
- [Provenance API](provenance.md) - Atom lineage and relationships
