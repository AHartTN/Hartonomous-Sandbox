# Web Search Rate Limiting Issue

## Problem
The `mcp_mcp_docker_search` tool (DuckDuckGo web search) frequently returns:
```
No results were found for your search query. This could be due to DuckDuckGo's bot detection or the query returned no matches.
```

## Root Cause
DuckDuckGo implements aggressive rate limiting and bot detection. Repeated searches in quick succession trigger blocking.

## Impact
- Cannot use web search for implementation specs during active development
- Must rely on existing knowledge, documentation in repo, or Microsoft Learn docs
- Video scene detection, histogram algorithms, and other computer vision specs need alternative sources

## Workarounds
1. **Use Microsoft Learn docs first** via `mcp_microsoftdocs_microsoft_docs_search` and `mcp_microsoftdocs_microsoft_code_sample_search`
2. **Space out searches** - wait several minutes between web searches
3. **Use specific queries** - be precise to avoid multiple failed attempts
4. **Implement from first principles** - use mathematical definitions from existing code/comments
5. **Defer to libraries** - recommend ImageSharp, OpenCV, FFmpeg instead of implementing from scratch

## Session Log
**Date**: 2025-11-12
**Context**: Implementing video scene detection with histogram comparison
**Failed Queries**:
- "video scene detection histogram comparison chi-square distance algorithm implementation"
- "histogram intersection distance opencv video shot boundary detection"

**Resolution**: Implement from first principles using histogram math definitions, recommend FFmpeg.NET or OpenCV for production use.

## Future Sessions
Check this file first before attempting web searches. If rate limited, use alternative approaches listed above.
