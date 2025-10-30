# Real-World Embedding Dimensions (2024-2025)

Research results from actual production models:

## Common Dimensions (Most Frequent)

| Dimension | Models | Usage |
|-----------|--------|-------|
| **384** | Sentence-Transformers (all-MiniLM-L6-v2, msmarco-MiniLM-L12-v3) | Fast semantic search, lightweight embeddings |
| **768** | BERT-Base, GPT-1, GPT-2 Small, all-mpnet-base-v2 | **MOST COMMON** - Standard transformer size |
| **1024** | BERT-Large, GPT-2 Medium, stsb-roberta-large | High-quality sentence embeddings |
| **1536** | OpenAI text-embedding-ada-002 | OpenAI embedding standard |

## Large Language Models

| Dimension | Model | Parameters |
|-----------|-------|------------|
| **4096** | LLaMA 2-7B | 7 billion |
| **5120** | LLaMA 2-13B | 13 billion |
| **8192** | LLaMA 2-70B | 70 billion |
| **12288** | GPT-3 | 175 billion |

## Small/Efficient Models

| Dimension | Model | Use Case |
|-----------|-------|----------|
| **128** | BERT-Tiny | Edge devices, mobile |
| **256** | text-embedding-3-large (shortened) | Optimized retrieval |
| **384** | MiniLM variants | Production semantic search |

## SQL Server 2025 Constraints

- **Max float32**: 1,998 dimensions
- **Max float16**: 3,996 dimensions

## Recommended Bucket Strategy

Based on real-world usage:

### Tier 1: Essential (covers 90% of models)
```sql
CREATE TABLE Weights_384  -- MiniLM, lightweight
CREATE TABLE Weights_768  -- BERT-Base, GPT-2, MOST COMMON
CREATE TABLE Weights_1024 -- BERT-Large
CREATE TABLE Weights_1536 -- OpenAI ada-002
```

### Tier 2: Large Models (LLMs)
```sql
-- These exceed SQL Server 2025 limits!
-- LLaMA: 4096, 5120, 8192
-- GPT-3: 12288
-- Solution: Chunking or external storage
```

## Key Insights

1. **768 is the king** - BERT-Base, GPT-2, most sentence transformers
2. **384 for efficiency** - Popular for production semantic search
3. **1536 for OpenAI** - If you're using OpenAI embeddings
4. **LLMs exceed SQL limits** - LLaMA (4096+) and GPT-3 (12288) need chunking

## Strategy for Large Models (>1998 dimensions)

For LLaMA/GPT-3 sized models exceeding SQL Server's 1998 float32 limit:

### Option 1: Use float16 (up to 3996)
```sql
CREATE TABLE Weights_3996 (
    weight_vector VECTOR(3996, float16)
)
-- Covers: Nothing in practice (models jump from 1536 to 4096)
```

### Option 2: Chunking Strategy
```sql
-- Split 4096-dim into chunks of 1998
CREATE TABLE WeightChunks (
    weight_id BIGINT,
    chunk_index INT,  -- 0, 1, 2 for 4096-dim split into 3 chunks
    chunk_vector VECTOR(1998)
)
```

### Option 3: Hybrid Approach
```sql
-- Store full-precision in VARBINARY for LLMs
CREATE TABLE LargeModelWeights (
    weight_id BIGINT,
    dimension INT,  -- 4096, 8192, 12288
    weight_data VARBINARY(MAX),
    -- For search: store UMAP 3D projection
    spatial_projection GEOMETRY
)
```

## Conclusion

**For 90% of models**: Support 384, 768, 1024, 1536

**For LLMs**: Need special handling - they exceed SQL Server limits anyway

The original bucket strategy (768, 1536, 1998, 3996) was close but missed:
- **384** (very common for MiniLM)
- **1024** (BERT-Large standard)

Recommend adding these two buckets for real-world coverage.
