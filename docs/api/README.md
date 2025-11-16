# API Reference

Complete API documentation for Hartonomous.

---

## API Layers

Hartonomous provides **two API layers**:

1. **T-SQL Stored Procedures** (primary interface - direct database access)
2. **REST API** (thin HTTP wrapper - optional convenience layer)

**Recommended**: Use T-SQL stored procedures directly for maximum performance and flexibility.

---

## T-SQL Stored Procedure Reference

### Generation & Inference

**sp_GenerateText** - Generate text using spatial navigation
**sp_SpatialNextToken** - Next token prediction via R-Tree
**sp_GenerateWithAttention** - Full attention-based generation
**sp_CrossModalQuery** - Query across text/image/audio/video
**sp_GenerateImage** - Image synthesis with geometric guidance
**sp_GenerateAudio** - Audio synthesis (harmonic generation)
**sp_GenerateVideo** - Video frame generation

### Reasoning Frameworks

**sp_ChainOfThoughtReasoning** - Linear step-by-step reasoning
**sp_MultiPathReasoning** - Tree of Thought exploration
**sp_SelfConsistencyReasoning** - Reflexion and consensus finding

### Agent Tools

**sp_SelectAgentTool** - Semantic tool selection
**sp_ExecuteAgentTool** - Dynamic tool execution
**sp_AgentExecuteTask** - Complete agent decision loop

### OODA Loop

**sp_Analyze** - System observation and metrics
**sp_Hypothesize** - Generate improvement hypotheses
**sp_Act** - Execute safe improvements
**sp_Learn** - Measure results and update weights

### Atomization

**sp_AtomizeText_Governed** - Atomize text content
**sp_AtomizeImage_Governed** - Atomize image pixels
**sp_AtomizeAudio_Governed** - Atomize audio samples
**sp_AtomizeModel_Governed** - Atomize model weights

### Utilities

**fn_ProjectTo3D** - Project embedding to 3D GEOMETRY
**fn_ComputeHilbertValue** - Compute Hilbert curve value
**fn_GenerateEmbedding** - Generate high-dimensional embedding

**Complete T-SQL API reference coming soon.**

---

## REST API Reference

**Base URL**: `https://localhost:5001/api` (development)

### Endpoints

**POST /api/inference/generate**
- Generate text/image/audio using spatial inference
- Request body: `{ "prompt": "...", "modality": "text", "maxTokens": 100 }`
- Response: `{ "result": "...", "inferenceId": "..." }`

**POST /api/reasoning/chain-of-thought**
- Execute Chain of Thought reasoning
- Request body: `{ "prompt": "...", "maxSteps": 10 }`
- Response: `{ "chainId": "...", "steps": [...], "finalOutput": "..." }`

**POST /api/agent/execute-task**
- Execute task with agent tool selection
- Request body: `{ "taskDescription": "...", "sessionId": "..." }`
- Response: `{ "toolUsed": "...", "result": "..." }`

**GET /api/provenance/{inferenceId}**
- Get full provenance trace for inference
- Response: `{ "inference": {...}, "inputs": [...], "reasoning": {...} }`

**Complete REST API reference coming soon.**

---

## Usage Examples

### T-SQL: Generate Text

```sql
DECLARE @SessionId UNIQUEIDENTIFIER = NEWID();

EXEC dbo.sp_GenerateText
    @Prompt = 'Explain quantum computing in simple terms',
    @MaxTokens = 200,
    @Temperature = 0.7,
    @SessionId = @SessionId;
```

### T-SQL: Chain of Thought

```sql
EXEC dbo.sp_ChainOfThoughtReasoning
    @Prompt = 'Solve: If x + 5 = 12, what is x?',
    @MaxSteps = 5,
    @SessionId = @SessionId;
```

### T-SQL: Cross-Modal Query

```sql
-- Find images related to "sunset"
DECLARE @QueryText NVARCHAR(MAX) = 'beautiful sunset over ocean';

EXEC dbo.sp_CrossModalQuery
    @QueryText = @QueryText,
    @Modalities = 'image,audio',  -- Return images and audio
    @TopK = 20,
    @SessionId = @SessionId;
```

### REST: Generate Text (curl)

```bash
curl -X POST https://localhost:5001/api/inference/generate \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Explain quantum computing in simple terms",
    "modality": "text",
    "maxTokens": 200,
    "temperature": 0.7
  }'
```

### REST: Chain of Thought (curl)

```bash
curl -X POST https://localhost:5001/api/reasoning/chain-of-thought \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Solve: If x + 5 = 12, what is x?",
    "maxSteps": 5
  }'
```

---

## Authentication

**T-SQL**: Use SQL Server authentication (integrated or SQL authentication)

**REST API**:
- Development: No authentication required
- Production: Bearer token (JWT) or API key required
- See [Security Guide](../setup/security.md) for configuration

---

## Rate Limiting

**T-SQL**: No rate limiting (controlled by SQL Server resource governor)

**REST API**:
- Development: 100 requests/minute per IP
- Production: Configurable per tenant/user
- Headers: `X-RateLimit-Remaining`, `X-RateLimit-Reset`

---

## Error Handling

### T-SQL Errors

```sql
BEGIN TRY
    EXEC dbo.sp_GenerateText @Prompt = 'test', @SessionId = @SessionId;
END TRY
BEGIN CATCH
    SELECT
        ERROR_NUMBER() AS ErrorNumber,
        ERROR_MESSAGE() AS ErrorMessage,
        ERROR_PROCEDURE() AS ErrorProcedure,
        ERROR_LINE() AS ErrorLine;
END CATCH
```

### REST API Errors

**Status Codes**:
- `200 OK` - Success
- `400 Bad Request` - Invalid input
- `401 Unauthorized` - Authentication required
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Server error

**Error Response**:
```json
{
  "error": {
    "code": "INVALID_PROMPT",
    "message": "Prompt cannot be empty",
    "details": {...}
  }
}
```

---

## Performance Considerations

**T-SQL Direct Access**:
- ✅ No HTTP overhead
- ✅ Connection pooling
- ✅ Transaction control
- ✅ Direct spatial index access
- **Recommended for high-performance scenarios**

**REST API**:
- ⚠️ HTTP overhead (~5-10ms per request)
- ⚠️ JSON serialization overhead
- ✅ Language-agnostic
- ✅ Easier integration
- **Recommended for external integrations**

---

## Complete API Documentation

**Coming soon**:
- Complete T-SQL stored procedure reference with parameters and examples
- REST API OpenAPI/Swagger specification
- Client libraries (Python, C#, JavaScript)
- Authentication and authorization guide
- Advanced usage patterns

For now, see:
- **[Rewrite Guide](../rewrite-guide/)** for technical implementation details
- **[QUICKSTART.md](../../QUICKSTART.md)** for basic usage examples
- **[Architecture](../../ARCHITECTURE.md)** for system overview
