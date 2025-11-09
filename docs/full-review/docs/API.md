# docs/API.md

## Purpose and Context

- Public-facing API reference covering authentication, embeddings, vector search, graph queries, autonomous system endpoints, analytics, rate limiting, webhooks, and SDK examples.
- Serves as canonical specification for Hartonomous REST services exposed at `https://api.hartonomous.com`.

## Key Sections / Highlights

- Detailed request/response schemas for embedding generation, batch operations, vector and hybrid search, graph lineage, autonomy controls, and analytics queries.
- Consistent error payload contract and enumerated error codes; outlines per-tier rate limits and response headers.
- Webhook registration and sample payloads for event-driven integrations.
- SDK usage snippets provided for C#, Python, and TypeScript clients.

## Potential Risks / Follow-ups

- Endpoints reference advanced autonomous features (hypothesis approval, graph provenance) that may not exist in current codebase; validate alignment with implementation.
- Static examples include timestamps, IDs, and metrics that could drift from actual system capabilities; schedule periodic refresh.
- Authentication section assumes username/password token issuance; confirm compatibility with production identity provider.
