using Hartonomous.Api.DTOs.Generation;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Multimodal content generation controller
/// Supports text, image, audio, and video generation from prompts
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("inference")] // Use inference rate limiter for expensive generation operations
public class GenerationController : ApiControllerBase
{
    private readonly string _connectionString;
    private readonly ILogger<GenerationController> _logger;

    public GenerationController(IConfiguration configuration, ILogger<GenerationController> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured");
        _logger = logger;
    }

    /// <summary>
    /// Generate text from a prompt using language models
    /// </summary>
    [HttpPost("text")]
    [ProducesResponseType(typeof(ApiResponse<GenerationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GenerationResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateTextAsync([FromBody] GenerateTextRequest request)
    {
        try
        {
            _logger.LogInformation("Generating text from prompt: {PromptPreview}", 
                request.Prompt.Length > 100 ? request.Prompt.Substring(0, 100) + "..." : request.Prompt);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand("dbo.sp_GenerateText", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 300 // 5 minutes for generation
            };

            command.Parameters.AddWithValue("@prompt", request.Prompt);
            command.Parameters.AddWithValue("@max_tokens", request.MaxTokens);
            command.Parameters.AddWithValue("@temperature", request.Temperature);
            command.Parameters.AddWithValue("@ModelIds", (object?)request.ModelIds ?? DBNull.Value);
            command.Parameters.AddWithValue("@top_k", request.TopK);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                    "No results returned from text generation");
                return BadRequest(Failure<GenerationResponse>(new[] { error }));
            }

            var response = new GenerationResponse
            {
                JobId = reader.GetInt64(reader.GetOrdinal("GenerationId")),
                Prompt = request.Prompt,
                ContentType = "text",
                Status = "completed",
                GeneratedContent = reader.GetString(reader.GetOrdinal("GeneratedText")),
                AtomId = reader.IsDBNull(reader.GetOrdinal("AtomId")) ? null : reader.GetInt64(reader.GetOrdinal("AtomId")),
                ModelIds = new List<int> { reader.GetInt32(reader.GetOrdinal("ModelId")) },
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Text generation complete: JobId={JobId}, Length={Length} chars", 
                response.JobId, response.GeneratedContent?.Length ?? 0);

            return Ok(Success(response));
        }
        catch (SqlException ex) when (ex.Number == 50090 || ex.Number == 50091 || ex.Number == 50092)
        {
            _logger.LogWarning(ex, "Text generation validation error");
            var error = ErrorDetailFactory.Validation(ex.Message);
            return BadRequest(Failure<GenerationResponse>(new[] { error }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error during text generation");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "Failed to generate text", ex.Message);
            return BadRequest(Failure<GenerationResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during text generation");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "An unexpected error occurred during text generation");
            return BadRequest(Failure<GenerationResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Generate an image from a text prompt using diffusion models
    /// </summary>
    [HttpPost("image")]
    [ProducesResponseType(typeof(ApiResponse<GenerationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GenerationResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateImageAsync([FromBody] GenerateImageRequest request)
    {
        try
        {
            _logger.LogInformation("Generating image: {PromptPreview}, {Width}x{Height}", 
                request.Prompt.Substring(0, Math.Min(50, request.Prompt.Length)), request.Width, request.Height);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand("dbo.sp_GenerateImage", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 600 // 10 minutes for image generation
            };

            command.Parameters.AddWithValue("@prompt", request.Prompt);
            command.Parameters.AddWithValue("@width", request.Width);
            command.Parameters.AddWithValue("@height", request.Height);
            command.Parameters.AddWithValue("@patch_size", request.PatchSize);
            command.Parameters.AddWithValue("@steps", request.Steps);
            command.Parameters.AddWithValue("@guidance_scale", request.GuidanceScale);
            command.Parameters.AddWithValue("@ModelIds", (object?)request.ModelIds ?? DBNull.Value);
            command.Parameters.AddWithValue("@top_k", request.TopK);
            command.Parameters.AddWithValue("@output_format", request.OutputFormat);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                    "No results returned from image generation");
                return BadRequest(Failure<GenerationResponse>(new[] { error }));
            }

            var response = new GenerationResponse
            {
                JobId = reader.GetInt64(reader.GetOrdinal("GenerationId")),
                Prompt = request.Prompt,
                ContentType = "image",
                Status = "completed",
                AtomId = reader.IsDBNull(reader.GetOrdinal("AtomId")) ? null : reader.GetInt64(reader.GetOrdinal("AtomId")),
                ModelIds = new List<int> { reader.GetInt32(reader.GetOrdinal("ModelId")) },
                Metadata = new Dictionary<string, object>
                {
                    ["width"] = request.Width,
                    ["height"] = request.Height,
                    ["patchSize"] = request.PatchSize,
                    ["steps"] = request.Steps,
                    ["outputFormat"] = request.OutputFormat
                },
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Image generation complete: JobId={JobId}, AtomId={AtomId}", 
                response.JobId, response.AtomId);

            return Ok(Success(response));
        }
        catch (SqlException ex) when (ex.Number >= 50100 && ex.Number <= 50109)
        {
            _logger.LogWarning(ex, "Image generation validation error");
            var error = ErrorDetailFactory.Validation(ex.Message);
            return BadRequest(Failure<GenerationResponse>(new[] { error }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error during image generation");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "Failed to generate image", ex.Message);
            return BadRequest(Failure<GenerationResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during image generation");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "An unexpected error occurred during image generation");
            return BadRequest(Failure<GenerationResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Generate audio from a text prompt
    /// </summary>
    [HttpPost("audio")]
    [ProducesResponseType(typeof(ApiResponse<GenerationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GenerationResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateAudioAsync([FromBody] GenerateAudioRequest request)
    {
        try
        {
            _logger.LogInformation("Generating audio: {PromptPreview}, duration={DurationMs}ms", 
                request.Prompt.Substring(0, Math.Min(50, request.Prompt.Length)), request.TargetDurationMs);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand("dbo.sp_GenerateAudio", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 600 // 10 minutes
            };

            command.Parameters.AddWithValue("@prompt", request.Prompt);
            command.Parameters.AddWithValue("@targetDurationMs", request.TargetDurationMs);
            command.Parameters.AddWithValue("@sampleRate", request.SampleRate);
            command.Parameters.AddWithValue("@ModelIds", (object?)request.ModelIds ?? DBNull.Value);
            command.Parameters.AddWithValue("@top_k", request.TopK);
            command.Parameters.AddWithValue("@temperature", request.Temperature);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                    "No results returned from audio generation");
                return BadRequest(Failure<GenerationResponse>(new[] { error }));
            }

            var response = new GenerationResponse
            {
                JobId = reader.GetInt64(reader.GetOrdinal("GenerationId")),
                Prompt = request.Prompt,
                ContentType = "audio",
                Status = "completed",
                AtomId = reader.IsDBNull(reader.GetOrdinal("AtomId")) ? null : reader.GetInt64(reader.GetOrdinal("AtomId")),
                ModelIds = new List<int> { reader.GetInt32(reader.GetOrdinal("ModelId")) },
                Metadata = new Dictionary<string, object>
                {
                    ["durationMs"] = request.TargetDurationMs,
                    ["sampleRate"] = request.SampleRate
                },
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Audio generation complete: JobId={JobId}, AtomId={AtomId}", 
                response.JobId, response.AtomId);

            return Ok(Success(response));
        }
        catch (SqlException ex) when (ex.Number >= 50110 && ex.Number <= 50119)
        {
            _logger.LogWarning(ex, "Audio generation validation error");
            var error = ErrorDetailFactory.Validation(ex.Message);
            return BadRequest(Failure<GenerationResponse>(new[] { error }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error during audio generation");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "Failed to generate audio", ex.Message);
            return BadRequest(Failure<GenerationResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during audio generation");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "An unexpected error occurred during audio generation");
            return BadRequest(Failure<GenerationResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Generate video from a text prompt
    /// </summary>
    [HttpPost("video")]
    [ProducesResponseType(typeof(ApiResponse<GenerationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GenerationResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateVideoAsync([FromBody] GenerateVideoRequest request)
    {
        try
        {
            _logger.LogInformation("Generating video: {PromptPreview}, duration={DurationMs}ms, fps={Fps}", 
                request.Prompt.Substring(0, Math.Min(50, request.Prompt.Length)), request.TargetDurationMs, request.TargetFps);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand("dbo.sp_GenerateVideo", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 900 // 15 minutes for video
            };

            command.Parameters.AddWithValue("@prompt", request.Prompt);
            command.Parameters.AddWithValue("@targetDurationMs", request.TargetDurationMs);
            command.Parameters.AddWithValue("@targetFps", request.TargetFps);
            command.Parameters.AddWithValue("@ModelIds", (object?)request.ModelIds ?? DBNull.Value);
            command.Parameters.AddWithValue("@top_k", request.TopK);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                    "No results returned from video generation");
                return BadRequest(Failure<GenerationResponse>(new[] { error }));
            }

            var response = new GenerationResponse
            {
                JobId = reader.GetInt64(reader.GetOrdinal("GenerationId")),
                Prompt = request.Prompt,
                ContentType = "video",
                Status = "completed",
                AtomId = reader.IsDBNull(reader.GetOrdinal("AtomId")) ? null : reader.GetInt64(reader.GetOrdinal("AtomId")),
                ModelIds = new List<int> { reader.GetInt32(reader.GetOrdinal("ModelId")) },
                Metadata = new Dictionary<string, object>
                {
                    ["durationMs"] = request.TargetDurationMs,
                    ["fps"] = request.TargetFps
                },
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Video generation complete: JobId={JobId}, AtomId={AtomId}", 
                response.JobId, response.AtomId);

            return Ok(Success(response));
        }
        catch (SqlException ex) when (ex.Number >= 50120 && ex.Number <= 50129)
        {
            _logger.LogWarning(ex, "Video generation validation error");
            var error = ErrorDetailFactory.Validation(ex.Message);
            return BadRequest(Failure<GenerationResponse>(new[] { error }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error during video generation");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "Failed to generate video", ex.Message);
            return BadRequest(Failure<GenerationResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during video generation");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "An unexpected error occurred during video generation");
            return BadRequest(Failure<GenerationResponse>(new[] { error }));
        }
    }
}
