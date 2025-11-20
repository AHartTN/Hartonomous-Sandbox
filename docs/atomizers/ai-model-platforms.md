# AI Model Platform Atomizers

## Overview

Hartonomous supports ingesting AI models from popular platforms without needing to manually download files. These atomizers fetch model metadata, configurations, and structure information directly from model registries.

## Supported Platforms

### Ollama

The `OllamaModelAtomizer` ingests models from a local Ollama server or Ollama Library.

**Input Format**: `model:tag` or just `model`

**Examples**:
- `llama3.2` - Latest Llama 3.2 model
- `mistral:7b-instruct` - Mistral 7B instruction-tuned
- `codellama:13b` - CodeLlama 13B

**Extracted Information**:
- Model metadata (modelfile, parameters, template)
- Model size and digest (version identifier)
- Architecture details (parameter count, quantization level, family)

**Requirements**:
- Ollama running locally (default: `http://localhost:11434`)
- Model must be pulled/available in Ollama

**Configuration**:
```json
{
  "ollamaEndpoint": "http://localhost:11434"
}
```

**API Endpoints Used**:
- `POST /api/show` - Fetch model information
- `GET /api/tags` - List available models with details

### Hugging Face

The `HuggingFaceModelAtomizer` ingests models from Hugging Face Hub.

**Input Format**: `organization/model-name` or `username/model-name`

**Examples**:
- `meta-llama/Llama-3.2-1B` - Llama 3.2 1B parameters
- `mistralai/Mistral-7B-v0.1` - Mistral 7B base
- `openai/whisper-large-v3` - Whisper audio model
- `gpt2` - GPT-2 (no organization prefix for official models)

**Extracted Information**:
- Model metadata (languages, license, tags)
- Pipeline tag (task type: text-generation, audio-transcription, etc.)
- Download statistics
- Model files list (safetensors, config.json, tokenizer files)
- Configuration files content

**Requirements**:
- Internet connection to Hugging Face Hub
- Optional: HF token for private/gated models

**Configuration**:
```json
{
  "hfToken": "hf_..." // Optional, for private repos
}
```

**API Endpoints Used**:
- `GET /api/models/{model_id}` - Model metadata
- `GET /api/models/{model_id}/tree/main` - File listing
- `GET /{model_id}/resolve/main/{file}` - Download files

## Atom Structure

Both atomizers create hierarchical atom compositions:

```
Model Atom (root)
├── Metadata Atom (model info JSON)
├── Configuration Atoms
│   ├── Modelfile/Model Card
│   ├── Parameters
│   ├── Template/Prompt Format
│   └── License/Tags
├── Architecture Atoms
│   ├── Parameter Size
│   ├── Quantization Level
│   └── Model Family
└── File Reference Atoms
    ├── Config Files (config.json, tokenizer_config.json)
    └── Weight Files (safetensors, pytorch_model.bin)
```

## Usage via API

### Ollama Example

```bash
POST /api/ingest/ollama
Content-Type: application/json

{
  "modelIdentifier": "llama3.2",
  "source": {
    "name": "Llama 3.2 from Ollama",
    "metadata": "{\"ollamaEndpoint\":\"http://localhost:11434\"}"
  }
}
```

### Hugging Face Example

```bash
POST /api/ingest/huggingface
Content-Type: application/json

{
  "modelIdentifier": "meta-llama/Llama-3.2-1B",
  "source": {
    "name": "Llama 3.2 1B from Hugging Face",
    "metadata": "{\"hfToken\":\"hf_...\"}"  // Optional
  }
}
```

## Future Enhancements

### Planned Features

1. **Full Model Download**: Currently, model weight files are referenced but not downloaded. Future versions will support downloading and atomizing actual model weights.

2. **Export Functionality**: Reconstitute models from atoms and push back to platforms:
   - Generate Ollama Modelfiles from atoms
   - Create Hugging Face model repositories
   - Convert between model formats

3. **Caching**: Implement local caching to avoid re-fetching model metadata.

4. **Batch Operations**: Support ingesting multiple models in a single operation.

5. **Model Comparison**: Compare architectures, quantization levels, and parameters across models.

## Integration with Other Atomizers

Models downloaded from these platforms can be further atomized by format-specific atomizers:

- **ModelFileAtomizer**: Parses GGUF, ONNX, SafeTensors, PyTorch files
- **DocumentAtomizer**: Processes README.md, model cards (Markdown/PDF)
- **CodeFileAtomizer**: Analyzes implementation code in model repos

The platform atomizers create references that can trigger recursive atomization of downloaded files.

## Troubleshooting

### Ollama Connection Failed

**Error**: `Failed to connect to Ollama`

**Solutions**:
- Verify Ollama is running: `ollama list`
- Check endpoint configuration in metadata
- Ensure model is pulled: `ollama pull llama3.2`

### Hugging Face 401 Unauthorized

**Error**: `Failed to fetch from Hugging Face: 401`

**Solutions**:
- Model may be gated (requires access request)
- Provide HF token in metadata for private models
- Verify token has correct permissions

### Model Not Found

**Error**: `Failed to fetch model info: 404`

**Solutions**:
- Check model identifier spelling
- Verify model exists: https://huggingface.co/{model_id}
- For Ollama, ensure model is pulled locally

## Performance Considerations

- **Ollama**: Fetches metadata only (< 1 MB), fast operation
- **Hugging Face**: Fetches metadata + config files (< 10 MB typically)
- Large model weights are **not** downloaded by default (requires enhancement)
- Both atomizers perform HTTP requests, expect network latency

## Security Notes

- **HF Tokens**: Store tokens securely, never commit to source control
- **Private Models**: Token permissions should be read-only
- **Local Ollama**: No authentication required for localhost
- **Network Traffic**: All HTTPS connections use TLS encryption
