using Hartonomous.Api.DTOs.Reasoning;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Advanced reasoning capabilities powered by database-layer AI reasoning frameworks.
/// Implements Chain of Thought, Tree of Thought, Self-Consistency, and more.
/// All intelligence lives in SQL Server stored procedures with CLR aggregates.
/// </summary>
[ApiController]
[Route("api/v1/reasoning")]
public class ReasoningController : ApiControllerBase
{
    private readonly string _connectionString;
    private readonly ILogger<ReasoningController> _logger;

    public ReasoningController(
        IConfiguration configuration,
        ILogger<ReasoningController> logger)
    {
        _connectionString = configuration.GetConnectionString("HartonomousDb")
            ?? throw new InvalidOperationException("Connection string 'HartonomousDb' not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute Chain of Thought reasoning for step-by-step problem solving.
    /// Uses CLR aggregates to analyze coherence across reasoning steps.
    /// </summary>
    [HttpPost("chain-of-thought")]
    [ProducesResponseType(typeof(ApiResponse<ChainOfThoughtResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChainOfThoughtResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ChainOfThoughtResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChainOfThoughtAsync(
        [FromBody] ChainOfThoughtRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(Failure<ChainOfThoughtResponse>(new[] 
            { 
                ErrorDetailFactory.InvalidFieldValue("Prompt", "Prompt cannot be empty") 
            }));
        }

        try
        {
            var steps = new List<ReasoningStep>();
            string? coherenceAnalysis = null;

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("dbo.sp_ChainOfThoughtReasoning", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 120 // Complex reasoning may take time
            };

            command.Parameters.AddWithValue("@ProblemId", request.ProblemId ?? Guid.NewGuid());
            command.Parameters.AddWithValue("@InitialPrompt", request.Prompt);
            command.Parameters.AddWithValue("@MaxSteps", request.MaxSteps ?? 5);
            command.Parameters.AddWithValue("@Temperature", request.Temperature ?? 0.7);
            command.Parameters.AddWithValue("@Debug", request.Debug ?? false);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                steps.Add(new ReasoningStep
                {
                    StepNumber = reader.GetInt32(reader.GetOrdinal("StepNumber")),
                    Prompt = reader.GetString(reader.GetOrdinal("Prompt")),
                    Response = reader.GetString(reader.GetOrdinal("Response")),
                    Confidence = reader.GetDouble(reader.GetOrdinal("Confidence")),
                    Timestamp = reader.GetDateTime(reader.GetOrdinal("StepTime"))
                });

                // Coherence analysis is same for all rows
                if (coherenceAnalysis == null)
                {
                    var ordinal = reader.GetOrdinal("CoherenceAnalysis");
                    coherenceAnalysis = reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
                }
            }

            var response = new ChainOfThoughtResponse
            {
                ProblemId = request.ProblemId ?? Guid.NewGuid(),
                Steps = steps,
                CoherenceAnalysis = coherenceAnalysis,
                TotalSteps = steps.Count
            };

            _logger.LogInformation(
                "Chain of Thought reasoning completed: {Steps} steps, coherence: {Coherence}",
                steps.Count,
                coherenceAnalysis);

            return Ok(Success(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error during Chain of Thought reasoning");
            var error = ErrorDetailFactory.InternalServerError("REASONING_DB_ERROR", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Failure<ChainOfThoughtResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Chain of Thought reasoning");
            var error = ErrorDetailFactory.InternalServerError("REASONING_FAILED", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Failure<ChainOfThoughtResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Execute Tree of Thought reasoning exploring multiple paths in parallel.
    /// Evaluates multiple reasoning branches and selects the optimal path.
    /// </summary>
    [HttpPost("tree-of-thought")]
    [ProducesResponseType(typeof(ApiResponse<TreeOfThoughtResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TreeOfThoughtResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TreeOfThoughtResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TreeOfThoughtAsync(
        [FromBody] TreeOfThoughtRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BasePrompt))
        {
            return BadRequest(Failure<TreeOfThoughtResponse>(new[] 
            { 
                ErrorDetailFactory.InvalidFieldValue("BasePrompt", "Base prompt cannot be empty") 
            }));
        }

        try
        {
            var tree = new List<ReasoningNode>();
            int bestPathId = 0;

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("dbo.sp_MultiPathReasoning", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 180 // Multiple paths require more time
            };

            command.Parameters.AddWithValue("@ProblemId", request.ProblemId ?? Guid.NewGuid());
            command.Parameters.AddWithValue("@BasePrompt", request.BasePrompt);
            command.Parameters.AddWithValue("@NumPaths", request.NumPaths ?? 3);
            command.Parameters.AddWithValue("@MaxDepth", request.MaxDepth ?? 3);
            command.Parameters.AddWithValue("@BranchingFactor", request.BranchingFactor ?? 2);
            command.Parameters.AddWithValue("@Debug", request.Debug ?? false);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                tree.Add(new ReasoningNode
                {
                    PathId = reader.GetInt32(reader.GetOrdinal("PathId")),
                    StepNumber = reader.GetInt32(reader.GetOrdinal("StepNumber")),
                    BranchId = reader.GetInt32(reader.GetOrdinal("BranchId")),
                    Prompt = reader.GetString(reader.GetOrdinal("Prompt")),
                    Response = reader.GetString(reader.GetOrdinal("Response")),
                    Score = reader.GetDouble(reader.GetOrdinal("Score")),
                    Timestamp = reader.GetDateTime(reader.GetOrdinal("StepTime"))
                });

                // Best path ID is same for all rows
                if (bestPathId == 0)
                {
                    bestPathId = reader.GetInt32(reader.GetOrdinal("BestPathId"));
                }
            }

            var response = new TreeOfThoughtResponse
            {
                ProblemId = request.ProblemId ?? Guid.NewGuid(),
                Tree = tree,
                BestPathId = bestPathId,
                TotalPaths = tree.Select(n => n.PathId).Distinct().Count(),
                TotalNodes = tree.Count
            };

            _logger.LogInformation(
                "Tree of Thought reasoning completed: {Paths} paths, {Nodes} nodes, best path: {BestPath}",
                response.TotalPaths,
                response.TotalNodes,
                bestPathId);

            return Ok(Success(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error during Tree of Thought reasoning");
            var error = ErrorDetailFactory.InternalServerError("REASONING_DB_ERROR", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Failure<TreeOfThoughtResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Tree of Thought reasoning");
            var error = ErrorDetailFactory.InternalServerError("REASONING_FAILED", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Failure<TreeOfThoughtResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Execute Self-Consistency reasoning generating multiple samples and finding consensus.
    /// Uses CLR aggregates to analyze agreement patterns across independent reasoning paths.
    /// </summary>
    [HttpPost("self-consistency")]
    [ProducesResponseType(typeof(ApiResponse<SelfConsistencyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SelfConsistencyResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SelfConsistencyResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SelfConsistencyAsync(
        [FromBody] SelfConsistencyRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(Failure<SelfConsistencyResponse>(new[] 
            { 
                ErrorDetailFactory.InvalidFieldValue("Prompt", "Prompt cannot be empty") 
            }));
        }

        try
        {
            var samples = new List<ReasoningSample>();
            string? consensusAnswer = null;
            double agreementRatio = 0;
            int numSupportingSamples = 0;
            double avgConfidence = 0;
            string? consensusMetrics = null;

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("dbo.sp_SelfConsistencyReasoning", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 180 // Multiple samples require time
            };

            command.Parameters.AddWithValue("@ProblemId", request.ProblemId ?? Guid.NewGuid());
            command.Parameters.AddWithValue("@Prompt", request.Prompt);
            command.Parameters.AddWithValue("@NumSamples", request.NumSamples ?? 5);
            command.Parameters.AddWithValue("@Temperature", request.Temperature ?? 0.8);
            command.Parameters.AddWithValue("@Debug", request.Debug ?? false);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                samples.Add(new ReasoningSample
                {
                    SampleId = reader.GetInt32(reader.GetOrdinal("SampleId")),
                    Response = reader.GetString(reader.GetOrdinal("Response")),
                    Confidence = reader.GetDouble(reader.GetOrdinal("Confidence")),
                    Timestamp = reader.GetDateTime(reader.GetOrdinal("SampleTime"))
                });

                // Consensus data is same for all rows
                if (consensusAnswer == null)
                {
                    var answerOrdinal = reader.GetOrdinal("ConsensusAnswer");
                    consensusAnswer = reader.IsDBNull(answerOrdinal) ? null : reader.GetString(answerOrdinal);
                    
                    agreementRatio = reader.GetDouble(reader.GetOrdinal("AgreementRatio"));
                    numSupportingSamples = reader.GetInt32(reader.GetOrdinal("NumSupportingSamples"));
                    avgConfidence = reader.GetDouble(reader.GetOrdinal("AvgConfidence"));
                    
                    var metricsOrdinal = reader.GetOrdinal("ConsensusMetrics");
                    consensusMetrics = reader.IsDBNull(metricsOrdinal) ? null : reader.GetString(metricsOrdinal);
                }
            }

            var response = new SelfConsistencyResponse
            {
                ProblemId = request.ProblemId ?? Guid.NewGuid(),
                ConsensusAnswer = consensusAnswer,
                AgreementRatio = agreementRatio,
                NumSupportingSamples = numSupportingSamples,
                AvgConfidence = avgConfidence,
                Samples = samples,
                ConsensusMetrics = consensusMetrics
            };

            _logger.LogInformation(
                "Self-Consistency reasoning completed: {Samples} samples, agreement: {Agreement:P2}, confidence: {Confidence:F2}",
                samples.Count,
                agreementRatio,
                avgConfidence);

            return Ok(Success(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error during Self-Consistency reasoning");
            var error = ErrorDetailFactory.InternalServerError("REASONING_DB_ERROR", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Failure<SelfConsistencyResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Self-Consistency reasoning");
            var error = ErrorDetailFactory.InternalServerError("REASONING_FAILED", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                Failure<SelfConsistencyResponse>(new[] { error }));
        }
    }
}
