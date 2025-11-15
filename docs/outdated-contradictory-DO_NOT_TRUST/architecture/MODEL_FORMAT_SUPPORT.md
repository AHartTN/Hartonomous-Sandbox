# Model Format Support

**Last Updated**: 2025-11-14
**Philosophy**: Parse ALL open model formats, extract weights → TensorAtoms, run inference via spatial tensor queries + CLR SIMD

## Supported Formats (4)

### ✅ GGUF (.gguf)
**Reader**: `GGUFModelReader`
**Library**: Custom parser (GGML spec)
**Status**: **COMPLETE** - All 15 quantization formats supported

**Quantization Formats**:
- F32 (0) - Full 32-bit floats
- F16 (1) - 16-bit floats
- Q4_0 (2) - 4-bit quantization, block size 32
- Q4_1 (3) - 4-bit quantization with min, block size 32
- Q5_0 (6) - 5-bit quantization, block size 32
- Q5_1 (7) - 5-bit quantization with min, block size 32
- Q8_0 (8) - 8-bit quantization, block size 32
- Q2_K (10) - 2-bit super-block quantization
- Q3_K (11) - 3-bit super-block quantization
- Q4_K (12) - 4-bit super-block quantization
- Q5_K (13) - 5-bit super-block quantization
- Q6_K (14) - 6-bit super-block quantization
- BF16 (30) - Brain Float 16

**Use Case**: Quantized LLMs (Llama, Mistral, Yi, etc.)

---

### ✅ ONNX (.onnx)
**Reader**: `OnnxModelReader`
**Library**: `Microsoft.ML.OnnxRuntime` (format parsing only, NOT for inference)
**Status**: **COMPLETE** - All tensor types supported

**Tensor Types**:
- float32, float64, float16, bfloat16
- int8, int16, int32, int64
- uint8, uint16, uint32, uint64
- bool, string
- complex64, complex128

**Use Case**: Cross-framework models (PyTorch/TensorFlow → ONNX export)

---

### ✅ PyTorch (.pth, .pt)
**Reader**: `PyTorchModelReader`
**Library**: `TorchSharp` (format parsing only)
**Status**: **COMPLETE** - State dict extraction working

**Features**:
- Extracts state_dict tensors
- Handles nested module hierarchies
- Converts to float32 for storage

**Use Case**: Native PyTorch checkpoints

---

### ✅ Safetensors (.safetensors)
**Reader**: `SafetensorsModelReader`
**Library**: Custom parser
**Status**: **COMPLETE** - Binary format with JSON header

**Features**:
- Fast zero-copy tensor loading
- JSON metadata extraction
- Supports all dtypes (F16, F32, BF16, I8, I16, I32, I64, U8, BOOL)

**Use Case**: HuggingFace model hub standard format

---

## Missing Formats (7)

### ❌ TensorFlow SavedModel (.pb)
**Proposed Library**: `TensorFlow.NET` or custom protobuf parser
**Priority**: HIGH - Major framework
**Complexity**: Medium - Protobuf parsing + graph extraction

**Requirements**:
- Parse GraphDef protobuf
- Extract Variable tensors
- Handle frozen graphs vs checkpoints

---

### ❌ Keras H5 (.h5)
**Proposed Library**: `HDF.PInvoke` + custom parser
**Priority**: HIGH - Widely used
**Complexity**: Medium - HDF5 format + Keras layer structure

**Requirements**:
- Parse HDF5 hierarchical structure
- Extract layer weights
- Handle model config JSON

---

### ❌ TensorFlow Lite (.tflite)
**Proposed Library**: FlatBuffers parser
**Priority**: MEDIUM - Mobile/edge deployments
**Complexity**: Low - FlatBuffers schema available

**Requirements**:
- Parse FlatBuffers schema
- Extract quantized operator weights
- Handle uint8 quantization

---

### ❌ CoreML (.mlmodel)
**Proposed Library**: Protobuf parser (CoreML spec)
**Priority**: MEDIUM - Apple ecosystem
**Complexity**: Medium - Protobuf + multiple model types

**Requirements**:
- Parse CoreML protobuf spec
- Support NeuralNetwork, Pipeline, GLM types
- Extract quantized weights

---

### ❌ JAX Checkpoints
**Proposed Library**: Pickle + msgpack parser
**Priority**: LOW - Research-focused
**Complexity**: High - Multiple serialization formats

**Requirements**:
- Parse Flax checkpoints (msgpack)
- Parse Orbax checkpoints (TensorStore)
- Handle pytree structures

---

### ❌ PaddlePaddle (.pdparams, .pdiparams)
**Proposed Library**: Custom parser
**Priority**: LOW - Regional (China)
**Complexity**: Medium - Pickle-based format

---

### ❌ MXNet (.params)
**Proposed Library**: Custom parser
**Priority**: LOW - Declining usage
**Complexity**: Medium - NDArray serialization

---

## Architecture

### Ingestion Flow
```
Model File (.gguf/.onnx/.pth/.safetensors)
    ↓
IModelFormatReader.ReadAsync()
    ↓
Extract weights as float[] arrays
    ↓
ModelLayer.WeightsGeometry = CreateGeometryFromWeights()
    ↓
TensorAtoms with SpatialSignature (GEOMETRY)
    ↓
R-tree spatial index
```

### Inference Flow (NO external inference libraries)
```
Input features → GEOMETRY point
    ↓
SELECT TOP 100 FROM TensorAtoms
WHERE ModelType = 'speech_encoder'
ORDER BY SpatialSignature.STDistance(@inputGeometry) ASC
    ↓
CLR SIMD processes input using retrieved tensor components
    ↓
Output atoms
```

### Key Principle
**Format Parser = GOOD** (libraries that READ file formats)
**External Inference Service = BAD** (Azure OpenAI, ONNX Runtime inference, etc.)

The intelligence happens via:
- Spatial tensor queries (R-tree KNN)
- CLR SIMD operations
- Database-first architecture

---

## Adding New Formats

### Required Files
1. `Services/ModelFormats/Readers/{Format}ModelReader.cs` - Implements `IModelFormatReader<T>`
2. `Services/ModelFormats/{Format}Metadata.cs` - Format-specific metadata
3. Unit tests

### Implementation Checklist
- [ ] Parse file structure
- [ ] Extract tensor data as float[] arrays
- [ ] Create ModelLayer entities with WeightsGeometry
- [ ] Handle quantization/compression formats
- [ ] Extract metadata (architecture, version, etc.)
- [ ] Register in `ModelDiscoveryService`

### Example Registration
```csharp
// In ModelDiscoveryService.cs
_formatReaders.Add(new TensorFlowModelReader(_logger, _layerRepository, _tfLoader));
```

---

## Proprietary Formats

**Policy**: Closed/proprietary formats without published specs are NOT supported.

**Rationale**: Cannot parse formats without specifications. If vendors want inclusion in "changing the world," publish a spec.

**Examples**:
- Apple proprietary formats (beyond CoreML spec)
- Vendor-locked formats with no documentation
- Formats requiring licensing for parsing

---

## Performance Targets

- Model ingestion: < 30 seconds for 7B parameter model
- Tensor extraction: < 5 seconds per billion parameters
- Spatial signature creation: < 2ms per layer
- Zero VRAM usage during ingestion

---

## Future Enhancements

1. **Streaming ingestion** - Process models > RAM size
2. **Incremental updates** - Add fine-tuned layers without full re-ingestion
3. **Format conversion** - Cross-format weight mapping
4. **Automatic quantization** - Convert F32 → Q4_K during ingestion
