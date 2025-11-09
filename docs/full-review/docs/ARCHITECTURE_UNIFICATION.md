# docs/ARCHITECTURE_UNIFICATION.md

## Purpose and Context

- Declares completion of the "Architecture Unification" initiative as of 2025-01-24, asserting all obsolete code paths removed and canonical substrate-based generation in place.
- Documents removal of ONNX runtime dependencies, replacement with SQL CLR+VECTOR distance pipelines, and alignment across ingestion, embedding, and generation flows.

## Core Assertions

- Eliminates specific files/methods (OnnxInferenceService, ContentGenerationSuite, GenerateViaSpatialQueryAsync) deemed incompatible with database-centric architecture.
- Describes canonical flow where stored procedures call CLR functions (`fn_GenerateWithAttention`) operating on `AtomEmbeddings` via `VECTOR_DISTANCE` and attention sampling.
- Details completed feature extraction implementations for images and audio (histograms, GLCM, Hu moments, FFT, MFCC), each producing 768-dimensional vectors.
- Outlines verified database schema representations for `TensorAtoms`, `TensorAtomCoefficients`, `AtomEmbeddings`, and `Atoms` supporting the unified approach.

## Validation Evidence

- Claims successful compilation across Infrastructure, SqlClr, API, and Admin projects; notes legacy Azure App Configuration build errors remain unresolved.
- Provides grep verification ensuring obsolete ONNX-related classes are absent.
- Summarizes end-to-end ingestion → embedding → projection → generation → provenance workflow and architecture principles enforced (no external runtimes/APIs).

## Potential Risks / Follow-ups

- Assertions may conflict with current repository state if files were reintroduced; confirm accuracy before planning work.
- Heavy reliance on SQL CLR and attention logic in database necessitates careful deployment/testing in SQL Server environments.
- Feature extraction and VECTOR dimensions should be cross-checked against actual code to avoid drift from documented expectations.
