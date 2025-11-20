# Catalog Manager

**Status**: Design Phase  
**Last Updated**: November 18, 2025  
**Owner**: CLR Refactoring Team

## Overview

The Catalog Manager coordinates multi-file AI models where a single model consists of multiple files (config, tokenizer, weights, etc.). This is essential for HuggingFace repositories, Ollama models, and Stable Diffusion pipelines.

### Key Principles

1. **Multi-File Coordination**: Handle models split across multiple files
2. **Config Parsing**: Extract metadata from config.json, tokenizer_config.json
3. **Component Mapping**: Track relationships between model components
4. **Version Management**: Handle model versions and revisions
5. **Catalog Integrity**: Validate all required files are present

## Catalog Structure

```
Model Catalog
├── config.json (architecture, hyperparameters)
├── tokenizer.json (vocabulary, special tokens)
├── tokenizer_config.json (tokenizer settings)
├── model.safetensors (weights - single file)
OR
├── model-00001-of-00003.safetensors (sharded weights)
├── model-00002-of-00003.safetensors
└── model-00003-of-00003.safetensors
```

## Core Interfaces

```csharp
namespace Hartonomous.Clr.Catalog
{
    public interface IModelCatalog
    {
        string ModelId { get; }
        ModelConfig Config { get; }
        TokenizerConfig Tokenizer { get; }
        WeightFile[] Weights { get; }
        Dictionary<string, string> AdditionalFiles { get; }
        
        bool IsComplete();
        string[] GetMissingFiles();
    }

    public class ModelConfig
    {
        public string Architecture { get; set; }
        public string ModelType { get; set; }
        public Dictionary<string, object> Hyperparameters { get; set; }
    }

    public class TokenizerConfig
    {
        public int VocabSize { get; set; }
        public string TokenizerClass { get; set; }
        public Dictionary<string, int> SpecialTokens { get; set; }
    }

    public class WeightFile
    {
        public string FileName { get; set; }
        public string Format { get; set; }
        public long SizeBytes { get; set; }
        public int ShardIndex { get; set; }
        public int TotalShards { get; set; }
    }
}
```

## HuggingFace Catalog Handler

```csharp
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hartonomous.Clr.Catalog
{
    public class HuggingFaceCatalog : IModelCatalog
    {
        public string ModelId { get; set; }
        public ModelConfig Config { get; set; }
        public TokenizerConfig Tokenizer { get; set; }
        public WeightFile[] Weights { get; set; }
        public Dictionary<string, string> AdditionalFiles { get; set; }

        public static HuggingFaceCatalog FromFiles(Dictionary<string, byte[]> files)
        {
            var catalog = new HuggingFaceCatalog
            {
                AdditionalFiles = new Dictionary<string, string>()
            };

            // Parse config.json
            if (files.ContainsKey("config.json"))
            {
                var configJson = System.Text.Encoding.UTF8.GetString(files["config.json"]);
                var config = JObject.Parse(configJson);
                
                catalog.Config = new ModelConfig
                {
                    Architecture = config["architectures"]?[0]?.ToString(),
                    ModelType = config["model_type"]?.ToString(),
                    Hyperparameters = new Dictionary<string, object>()
                };

                foreach (var prop in config.Properties())
                {
                    catalog.Config.Hyperparameters[prop.Name] = prop.Value.ToString();
                }
            }

            // Parse tokenizer_config.json
            if (files.ContainsKey("tokenizer_config.json"))
            {
                var tokenizerJson = System.Text.Encoding.UTF8.GetString(files["tokenizer_config.json"]);
                var tokConfig = JObject.Parse(tokenizerJson);
                
                catalog.Tokenizer = new TokenizerConfig
                {
                    VocabSize = tokConfig["vocab_size"]?.Value<int>() ?? 0,
                    TokenizerClass = tokConfig["tokenizer_class"]?.ToString(),
                    SpecialTokens = new Dictionary<string, int>()
                };

                // Extract special tokens
                if (tokConfig["added_tokens_decoder"] != null)
                {
                    foreach (var token in (JObject)tokConfig["added_tokens_decoder"])
                    {
                        var tokenId = int.Parse(token.Key);
                        var tokenValue = token.Value["content"]?.ToString();
                        if (tokenValue != null)
                        {
                            catalog.Tokenizer.SpecialTokens[tokenValue] = tokenId;
                        }
                    }
                }
            }

            // Enumerate weight files
            var weightFiles = new List<WeightFile>();
            var shardPattern = @"model-(\d+)-of-(\d+)\.(safetensors|bin)";
            
            foreach (var file in files.Keys)
            {
                if (file.EndsWith(".safetensors") || file.EndsWith(".bin"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(file, shardPattern);
                    
                    var weightFile = new WeightFile
                    {
                        FileName = file,
                        Format = file.EndsWith(".safetensors") ? "SafeTensors" : "PyTorch",
                        SizeBytes = files[file].Length
                    };

                    if (match.Success)
                    {
                        weightFile.ShardIndex = int.Parse(match.Groups[1].Value);
                        weightFile.TotalShards = int.Parse(match.Groups[2].Value);
                    }
                    else
                    {
                        weightFile.ShardIndex = 1;
                        weightFile.TotalShards = 1;
                    }

                    weightFiles.Add(weightFile);
                }
            }

            catalog.Weights = weightFiles.ToArray();

            return catalog;
        }

        public bool IsComplete()
        {
            // Check required files
            if (Config == null) return false;
            if (Weights == null || Weights.Length == 0) return false;

            // Check shard continuity
            if (Weights.Any(w => w.TotalShards > 1))
            {
                var totalShards = Weights.First().TotalShards;
                var presentShards = Weights.Select(w => w.ShardIndex).Distinct().Count();
                return presentShards == totalShards;
            }

            return true;
        }

        public string[] GetMissingFiles()
        {
            var missing = new List<string>();

            if (Config == null)
                missing.Add("config.json");

            if (Weights == null || Weights.Length == 0)
                missing.Add("model weights");

            if (Weights != null && Weights.Any(w => w.TotalShards > 1))
            {
                var totalShards = Weights.First().TotalShards;
                var presentShards = new HashSet<int>(Weights.Select(w => w.ShardIndex));
                
                for (int i = 1; i <= totalShards; i++)
                {
                    if (!presentShards.Contains(i))
                        missing.Add($"model-{i:D5}-of-{totalShards:D5}.safetensors");
                }
            }

            return missing.ToArray();
        }
    }
}
```

## Ollama Catalog Handler

```csharp
namespace Hartonomous.Clr.Catalog
{
    public class OllamaCatalog : IModelCatalog
    {
        public string ModelId { get; set; }
        public ModelConfig Config { get; set; }
        public TokenizerConfig Tokenizer { get; set; }
        public WeightFile[] Weights { get; set; }
        public Dictionary<string, string> AdditionalFiles { get; set; }
        public string Modelfile { get; set; }

        public static OllamaCatalog FromFiles(Dictionary<string, byte[]> files)
        {
            var catalog = new OllamaCatalog
            {
                AdditionalFiles = new Dictionary<string, string>()
            };

            // Parse Modelfile
            if (files.ContainsKey("Modelfile"))
            {
                catalog.Modelfile = System.Text.Encoding.UTF8.GetString(files["Modelfile"]);
                catalog.Config = ParseModelfile(catalog.Modelfile);
            }

            // Find GGUF weight file
            var ggufFile = files.Keys.FirstOrDefault(k => k.EndsWith(".gguf"));
            if (ggufFile != null)
            {
                catalog.Weights = new[]
                {
                    new WeightFile
                    {
                        FileName = ggufFile,
                        Format = "GGUF",
                        SizeBytes = files[ggufFile].Length,
                        ShardIndex = 1,
                        TotalShards = 1
                    }
                };
            }

            return catalog;
        }

        private static ModelConfig ParseModelfile(string modelfile)
        {
            var config = new ModelConfig
            {
                ModelType = "GGUF",
                Hyperparameters = new Dictionary<string, object>()
            };

            foreach (var line in modelfile.Split('\n'))
            {
                var trimmed = line.Trim();
                
                if (trimmed.StartsWith("FROM "))
                {
                    config.Hyperparameters["BaseModel"] = trimmed.Substring(5).Trim();
                }
                else if (trimmed.StartsWith("PARAMETER "))
                {
                    var paramLine = trimmed.Substring(10);
                    var parts = paramLine.Split(new[] { ' ' }, 2);
                    if (parts.Length == 2)
                    {
                        config.Hyperparameters[parts[0]] = parts[1];
                    }
                }
                else if (trimmed.StartsWith("TEMPLATE "))
                {
                    config.Hyperparameters["PromptTemplate"] = trimmed.Substring(9).Trim();
                }
            }

            return config;
        }

        public bool IsComplete()
        {
            return Modelfile != null && Weights != null && Weights.Length > 0;
        }

        public string[] GetMissingFiles()
        {
            var missing = new List<string>();

            if (string.IsNullOrEmpty(Modelfile))
                missing.Add("Modelfile");

            if (Weights == null || Weights.Length == 0)
                missing.Add("*.gguf");

            return missing.ToArray();
        }
    }
}
```

## Stable Diffusion Catalog Handler

```csharp
namespace Hartonomous.Clr.Catalog
{
    public class StableDiffusionCatalog : IModelCatalog
    {
        public string ModelId { get; set; }
        public ModelConfig Config { get; set; }
        public TokenizerConfig Tokenizer { get; set; }
        public WeightFile[] Weights { get; set; }
        public Dictionary<string, string> AdditionalFiles { get; set; }

        // SD-specific components
        public WeightFile UNet { get; set; }
        public WeightFile VAE { get; set; }
        public WeightFile TextEncoder { get; set; }
        public WeightFile TextEncoder2 { get; set; } // SDXL

        public static StableDiffusionCatalog FromFiles(Dictionary<string, byte[]> files)
        {
            var catalog = new StableDiffusionCatalog
            {
                AdditionalFiles = new Dictionary<string, string>()
            };

            // Parse model_index.json
            if (files.ContainsKey("model_index.json"))
            {
                var indexJson = System.Text.Encoding.UTF8.GetString(files["model_index.json"]);
                var index = JObject.Parse(indexJson);
                
                catalog.Config = new ModelConfig
                {
                    Architecture = "StableDiffusion",
                    ModelType = index["_class_name"]?.ToString(),
                    Hyperparameters = new Dictionary<string, object>()
                };

                // Extract component paths
                foreach (var prop in index.Properties().Where(p => !p.Name.StartsWith("_")))
                {
                    catalog.Config.Hyperparameters[prop.Name] = prop.Value.ToString();
                }
            }

            var weights = new List<WeightFile>();

            // Map components
            foreach (var file in files.Keys)
            {
                if (file.Contains("unet/") && file.EndsWith(".safetensors"))
                {
                    catalog.UNet = new WeightFile
                    {
                        FileName = file,
                        Format = "SafeTensors",
                        SizeBytes = files[file].Length
                    };
                    weights.Add(catalog.UNet);
                }
                else if (file.Contains("vae/") && file.EndsWith(".safetensors"))
                {
                    catalog.VAE = new WeightFile
                    {
                        FileName = file,
                        Format = "SafeTensors",
                        SizeBytes = files[file].Length
                    };
                    weights.Add(catalog.VAE);
                }
                else if (file.Contains("text_encoder/") && file.EndsWith(".safetensors"))
                {
                    catalog.TextEncoder = new WeightFile
                    {
                        FileName = file,
                        Format = "SafeTensors",
                        SizeBytes = files[file].Length
                    };
                    weights.Add(catalog.TextEncoder);
                }
                else if (file.Contains("text_encoder_2/") && file.EndsWith(".safetensors"))
                {
                    catalog.TextEncoder2 = new WeightFile
                    {
                        FileName = file,
                        Format = "SafeTensors",
                        SizeBytes = files[file].Length
                    };
                    weights.Add(catalog.TextEncoder2);
                }
            }

            catalog.Weights = weights.ToArray();

            return catalog;
        }

        public bool IsComplete()
        {
            return Config != null && UNet != null && VAE != null && TextEncoder != null;
        }

        public string[] GetMissingFiles()
        {
            var missing = new List<string>();

            if (Config == null)
                missing.Add("model_index.json");
            if (UNet == null)
                missing.Add("unet/diffusion_pytorch_model.safetensors");
            if (VAE == null)
                missing.Add("vae/diffusion_pytorch_model.safetensors");
            if (TextEncoder == null)
                missing.Add("text_encoder/model.safetensors");

            return missing.ToArray();
        }
    }
}
```

## SQL Integration

```csharp
public static class SqlCatalogFunctions
{
    [SqlFunction(DataAccess = DataAccessKind.Read)]
    public static SqlString GetModelCatalog(SqlInt32 modelId)
    {
        using (SqlConnection conn = new SqlConnection("context connection=true"))
        {
            conn.Open();

            // Get all files for model
            var files = new Dictionary<string, byte[]>();
            
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT FileName, FileData
                FROM ModelFiles
                WHERE ModelId = @modelId", conn))
            {
                cmd.Parameters.AddWithValue("@modelId", modelId.Value);
                
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        files[reader.GetString(0)] = (byte[])reader[1];
                    }
                }
            }

            // Detect catalog type and parse
            IModelCatalog catalog;
            
            if (files.ContainsKey("config.json"))
                catalog = HuggingFaceCatalog.FromFiles(files);
            else if (files.ContainsKey("Modelfile"))
                catalog = OllamaCatalog.FromFiles(files);
            else if (files.ContainsKey("model_index.json"))
                catalog = StableDiffusionCatalog.FromFiles(files);
            else
                return SqlString.Null;

            return new SqlString(JsonConvert.SerializeObject(catalog));
        }
    }

    [SqlProcedure]
    public static void ValidateModelCatalog(SqlInt32 modelId, out SqlBoolean isComplete, out SqlString missingFiles)
    {
        var catalogJson = GetModelCatalog(modelId);
        
        if (catalogJson.IsNull)
        {
            isComplete = SqlBoolean.False;
            missingFiles = new SqlString("Unable to determine catalog type");
            return;
        }

        var catalog = JsonConvert.DeserializeObject<HuggingFaceCatalog>(catalogJson.Value);
        
        isComplete = new SqlBoolean(catalog.IsComplete());
        missingFiles = new SqlString(string.Join(", ", catalog.GetMissingFiles()));
    }
}
```

## Summary

✅ **HuggingFace**: config.json, tokenizer, sharded weights  
✅ **Ollama**: Modelfile, GGUF weights  
✅ **Stable Diffusion**: model_index.json, UNet, VAE, TextEncoder  
✅ **Validation**: Check completeness, identify missing files  
✅ **SQL Integration**: Catalog retrieval and validation functions
