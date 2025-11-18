using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

public class Program
{
    private static readonly HttpClient httpClient = new HttpClient
    {
        BaseAddress = new Uri("http://localhost:5000") // Assuming the API runs locally
    };

    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Hartonomous CLI: A command-line interface for the Hartonomous AGI system.");

        // ========== Ingest Command ==========
        var ingestCommand = new Command("ingest", "Ingest data into the Hartonomous system.");
        
        // --- Ingest File Subcommand ---
        var ingestFileCommand = new Command("file", "Ingest a local file.");
        var filePathArgument = new Argument<FileInfo>("file-path", "The path to the file to ingest.");
        var modalityOption = new Option<string>("--modality", "The modality of the content (e.g., 'text', 'image', 'code').") { IsRequired = true };
        var subtypeOption = new Option<string>("--subtype", "An optional subtype for the modality.");
        var sourceUriOption = new Option<string>("--source-uri", "An optional source URI to associate with the content.");

        ingestFileCommand.AddArgument(filePathArgument);
        ingestFileCommand.AddOption(modalityOption);
        ingestFileCommand.AddOption(subtypeOption);
        ingestFileCommand.AddOption(sourceUriOption);

        ingestFileCommand.SetHandler(async (context) =>
        {
            var fileInfo = context.ParseResult.GetValueForArgument(filePathArgument);
            var modality = context.ParseResult.GetValueForOption(modalityOption);
            var subtype = context.ParseResult.GetValueForOption(subtypeOption);
            var sourceUri = context.ParseResult.GetValueForOption(sourceUriOption);

            await IngestFileAsync(fileInfo, modality, subtype, sourceUri);
        });

        ingestCommand.AddCommand(ingestFileCommand);
        rootCommand.AddCommand(ingestCommand);

        // ========== Inference Command ==========
        var inferCommand = new Command("infer", "Run inference using a model in the Hartonomous system.");

        // --- Infer Run Subcommand ---
        var inferRunCommand = new Command("run", "Run a synchronous inference task and get the result immediately.");
        var modelIdOption = new Option<int>("--model-id", "The ID of the model to use for inference.") { IsRequired = true };
        var promptOption = new Option<string>("--prompt", "The input prompt to the model.") { IsRequired = true };

        inferRunCommand.AddOption(modelIdOption);
        inferRunCommand.AddOption(promptOption);

        inferRunCommand.SetHandler(async (context) =>
        {
            var modelId = context.ParseResult.GetValueForOption(modelIdOption);
            var prompt = context.ParseResult.GetValueForOption(promptOption);

            await RunInferenceAsync(modelId, prompt);
        });

        inferCommand.AddCommand(inferRunCommand);
        rootCommand.AddCommand(inferCommand);

        // ========== Search Command ==========
        var searchCommand = new Command("search", "Search for content in the Hartonomous system.");
        var searchQueryArgument = new Argument<string>("query", "The search query text.");
        var topKOption = new Option<int>("--top-k", () => 5, "The number of results to return.");

        searchCommand.AddArgument(searchQueryArgument);
        searchCommand.AddOption(topKOption);

        searchCommand.SetHandler(async (context) =>
        {
            var query = context.ParseResult.GetValueForArgument(searchQueryArgument);
            var topK = context.ParseResult.GetValueForOption(topKOption);

            await SearchAsync(query, topK);
        });

        rootCommand.AddCommand(searchCommand);

        // Invoke the command line parser
        return await rootCommand.InvokeAsync(args);
    }

    private static async Task IngestFileAsync(FileInfo fileInfo, string modality, string subtype, string sourceUri)
    {
        if (!fileInfo.Exists)
        {
            System.Console.WriteLine($"Error: File not found at '{fileInfo.FullName}'");
            return;
        }

        using var content = new MultipartFormDataContent();
        using var fileStream = fileInfo.OpenRead();
        
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", fileInfo.Name);

        content.Add(new StringContent(modality), "modality");

        if (!string.IsNullOrEmpty(subtype))
        {
            content.Add(new StringContent(subtype), "subtype");
        }
        if (!string.IsNullOrEmpty(sourceUri))
        {
            content.Add(new StringContent(sourceUri), "sourceUri");
        }

        System.Console.WriteLine($"Uploading '{fileInfo.Name}'...");

        try
        {
            var response = await httpClient.PostAsync("/api/ingest/file", content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine("Ingestion successful:");
                System.Console.WriteLine(responseString);
            }
            else
            {
                var errorString = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"Error: {response.StatusCode}");
                System.Console.WriteLine(errorString);
            }
        }
                catch (HttpRequestException ex)
                {
                    System.Console.WriteLine($"API connection error: {ex.Message}");
                    System.Console.WriteLine("Please ensure the Hartonomous.Api server is running at http://localhost:5000.");
                }
            }
        
                private static async Task RunInferenceAsync(int modelId, string prompt)
                {
                    System.Console.WriteLine($"Tokenizing prompt...");
            
                    // 1. Tokenize the prompt by calling the new API endpoint
                    int[] tokenIds;
                    try
                    {
                        var tokenizeRequestPayload = new { text = prompt };
                        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(tokenizeRequestPayload);
                        var httpContent = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            
                        var tokenizeResponse = await httpClient.PostAsync("/api/v1/Tokenizer", httpContent);
            
                        if (!tokenizeResponse.IsSuccessStatusCode)
                        {
                            var errorString = await tokenizeResponse.Content.ReadAsStringAsync();
                            System.Console.WriteLine($"Error during tokenization: {tokenizeResponse.StatusCode}");
                            System.Console.WriteLine(errorString);
                            return;
                        }
            
                        var responseString = await tokenizeResponse.Content.ReadAsStringAsync();
                        // Assuming the response is in the format: { "data": { "tokenIds": [1, 2, 3] } }
                        using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseString);
                        tokenIds = jsonDoc.RootElement.GetProperty("data").GetProperty("tokenIds").EnumerateArray().Select(t => t.GetInt32()).ToArray();
                        
                        System.Console.WriteLine($"Tokenization successful. Found {tokenIds.Length} tokens.");
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"An error occurred during tokenization: {ex.Message}");
                        return;
                    }
            
                    // 2. Run inference with the retrieved token IDs
                    System.Console.WriteLine($"Running inference with Model ID: {modelId}...");
                    var inferenceRequestPayload = new
                    {
                        modelId = modelId,
                        tokenIds = tokenIds
                    };
            
                    var inferenceJsonPayload = System.Text.Json.JsonSerializer.Serialize(inferenceRequestPayload);
                    var inferenceHttpContent = new StringContent(inferenceJsonPayload, System.Text.Encoding.UTF8, "application/json");
            
                    try
                    {
                        var response = await httpClient.PostAsync("/api/inference/run", inferenceHttpContent);
            
                        if (response.IsSuccessStatusCode)
                        {
                            var responseString = await response.Content.ReadAsStringAsync();
                            System.Console.WriteLine("Inference successful:");
                            System.Console.WriteLine(responseString);
                        }
                        else
                        {
                            var errorString = await response.Content.ReadAsStringAsync();
                            System.Console.WriteLine($"Error: {response.StatusCode}");
                            System.Console.WriteLine(errorString);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                                    System.Console.WriteLine($"API connection error: {ex.Message}");
                                    System.Console.WriteLine("Please ensure the Hartonomous.Api server is running at http://localhost:5000.");
                                }
                            }
                        
                            private static async Task SearchAsync(string query, int topK)
                            {
                                System.Console.WriteLine($"Searching for: '{query}'...");
                        
                                var requestPayload = new
                                {
                                    queryText = query,
                                    targetModalities = new[] { "text", "code" }, // Default to searching for text-based content
                                    topK = topK
                                };
                        
                                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(requestPayload);
                                var httpContent = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                        
                                try
                                {
                                    var response = await httpClient.PostAsync("/api/search/cross-modal", httpContent);
                        
                                    if (response.IsSuccessStatusCode)
                                    {
                                        var responseString = await response.Content.ReadAsStringAsync();
                                        
                                        // Parse and format the output
                                        using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseString);
                                        var results = jsonDoc.RootElement.GetProperty("data").GetProperty("results");
                        
                                        System.Console.WriteLine("Search results:");
                                        System.Console.WriteLine("--------------------------------------------------");
                                        foreach (var result in results.EnumerateArray())
                                        {
                                            var similarity = result.GetProperty("similarity").GetDouble();
                                            var modality = result.GetProperty("modality").GetString();
                                            var sourceUri = result.GetProperty("sourceUri").GetString();
                        
                                            System.Console.WriteLine($"Similarity: {similarity:P2}");
                                            System.Console.WriteLine($"  Modality: {modality}");
                                            System.Console.WriteLine($"  Source:   {sourceUri}");
                                            System.Console.WriteLine("--------------------------------------------------");
                                        }
                                    }
                                    else
                                    {
                                        var errorString = await response.Content.ReadAsStringAsync();
                                        System.Console.WriteLine($"Error: {response.StatusCode}");
                                        System.Console.WriteLine(errorString);
                                    }
                                }
                                catch (HttpRequestException ex)
                                {
                                    System.Console.WriteLine($"API connection error: {ex.Message}");
                                    System.Console.WriteLine("Please ensure the Hartonomous.Api server is running at http://localhost:5000.");
                                }
                            }
                        }
                        
        
