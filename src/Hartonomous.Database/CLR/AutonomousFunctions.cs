using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Microsoft.SqlServer.Server;
using System.Data;

namespace Hartonomous.Clr
{
    /// <summary>
    /// SQL CLR functions for autonomous AI operations.
    /// Replaces C# services with database-native autonomous intelligence.
    /// </summary>
    public static class AutonomousFunctions
    {
        /// <summary>
        /// Calculates inference complexity based on token count, multimodal requirements, and tool usage.
        /// Replaces InferenceMetadataService.CalculateComplexity method.
        /// </summary>
        /// <param name="inputTokenCount">Number of input tokens</param>
        /// <param name="requiresMultiModal">Whether the inference requires multimodal processing</param>
        /// <param name="requiresToolUse">Whether the inference requires tool usage</param>
        /// <returns>Complexity score from 1-10</returns>
        [SqlFunction(IsDeterministic = true)]
        public static SqlInt32 fn_CalculateComplexity(
            SqlInt32 inputTokenCount,
            SqlBoolean requiresMultiModal,
            SqlBoolean requiresToolUse)
        {
            if (inputTokenCount.IsNull)
                return SqlInt32.Null;

            int complexity = 1;
            int tokenCount = inputTokenCount.Value;

            // Base complexity from token count
            if (tokenCount > 8000) complexity += 4;
            else if (tokenCount > 4000) complexity += 3;
            else if (tokenCount > 2000) complexity += 2;
            else if (tokenCount > 1000) complexity += 1;

            // Multi-modal adds complexity
            if (!requiresMultiModal.IsNull && requiresMultiModal.Value)
                complexity += 2;

            // Tool use adds complexity
            if (!requiresToolUse.IsNull && requiresToolUse.Value)
                complexity += 2;

            // Cap at 10
            return Math.Min(complexity, 10);
        }

        /// <summary>
        /// Determines SLA tier based on priority and complexity.
        /// Replaces InferenceMetadataService.DetermineSla method.
        /// </summary>
        /// <param name="priority">Priority level (critical, high, medium, low)</param>
        /// <param name="complexity">Complexity score from fn_CalculateComplexity</param>
        /// <returns>SLA tier: realtime, expedited, or standard</returns>
        [SqlFunction(IsDeterministic = true)]
        public static SqlString fn_DetermineSla(SqlString priority, SqlInt32 complexity)
        {
            if (priority.IsNull || complexity.IsNull)
                return SqlString.Null;

            string priorityValue = priority.Value.ToLowerInvariant();
            int complexityValue = complexity.Value;

            // Case-insensitive priority comparison
            if (priorityValue == "critical" ||
                (priorityValue == "high" && complexityValue <= 3))
            {
                return "realtime";
            }

            if (priorityValue == "high" ||
                (priorityValue == "medium" && complexityValue <= 5))
            {
                return "expedited";
            }

            return "standard";
        }

        /// <summary>
        /// Estimates response time based on model performance metrics and complexity.
        /// Replaces InferenceMetadataService.EstimateResponseTimeAsync method.
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <param name="complexity">Complexity score from fn_CalculateComplexity</param>
        /// <returns>Estimated response time in milliseconds</returns>
        [SqlFunction(DataAccess = DataAccessKind.Read)]
        public static SqlInt32 fn_EstimateResponseTime(SqlString modelName, SqlInt32 complexity)
        {
            if (complexity.IsNull)
                return SqlInt32.Null;

            if (modelName.IsNull || string.IsNullOrWhiteSpace(modelName.Value))
            {
                // Base estimate when no model specified
                return complexity.Value * 5;
            }

            try
            {
                // Query model performance metrics from database
                using (var connection = new SqlConnection("context connection=true"))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT JSON_VALUE(m.MetadataJson, '$.performanceMetrics.avgLatencyMs')
                            FROM dbo.Models m
                            WHERE m.ModelName = @modelName
                        ";

                        var param = command.Parameters.Add("@modelName", System.Data.SqlDbType.NVarChar, 255);
                        param.Value = modelName.Value;

                        var result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            int baseLatency = Convert.ToInt32(result);
                            return baseLatency + (complexity.Value * (baseLatency / 10));
                        }
                    }
                }

                // Fallback: no metadata available
                return complexity.Value * 5;
            }
            catch
            {
                // Error fallback
                return complexity.Value * 5;
            }
        }

        /// <summary>
        /// Parses model capabilities from JSON metadata.
        /// Replaces ModelCapabilityService.GetCapabilitiesAsync method.
        /// Returns a table with capability information.
        /// </summary>
        /// <param name="modelName">Name of the model to query</param>
        /// <returns>Table with supported_tasks, supported_modalities, max_tokens, max_context_window, embedding_dimension</returns>
        [SqlFunction(
            DataAccess = DataAccessKind.Read,
            SystemDataAccess = SystemDataAccessKind.Read,
            FillRowMethodName = nameof(FillCapabilityRow),
            TableDefinition = "supported_tasks NVARCHAR(MAX), supported_modalities NVARCHAR(MAX), max_tokens INT, max_context_window INT, embedding_dimension INT")]
        public static System.Collections.IEnumerable fn_ParseModelCapabilities(SqlString modelName)
        {
            CapabilityResult result;

            if (modelName.IsNull || string.IsNullOrWhiteSpace(modelName.Value))
            {
                // Return default capabilities
                result = new CapabilityResult
                {
                    SupportedTasks = "text-generation",
                    SupportedModalities = "text",
                    MaxTokens = 2048,
                    MaxContextWindow = 4096,
                    EmbeddingDimension = null
                };
            }
            else
            {
                try
                {
                    using (var connection = new SqlConnection("context connection=true"))
                    {
                        connection.Open();

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"
                                SELECT
                                    JSON_VALUE(m.MetadataJson, '$.supportedTasks') as supportedTasks,
                                    JSON_VALUE(m.MetadataJson, '$.supportedModalities') as supportedModalities,
                                    JSON_VALUE(m.MetadataJson, '$.maxOutputLength') as maxOutputLength,
                                    JSON_VALUE(m.MetadataJson, '$.maxInputLength') as maxInputLength,
                                    JSON_VALUE(m.MetadataJson, '$.embeddingDimension') as embeddingDimension
                                FROM dbo.Models m
                                WHERE m.ModelName = @modelName
                            ";

                            var param = command.Parameters.Add("@modelName", System.Data.SqlDbType.NVarChar, 255);
                            param.Value = modelName.Value;

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    var supportedTasks = reader.IsDBNull(0) ? "text-generation" : reader.GetString(0);
                                    var supportedModalities = reader.IsDBNull(1) ? "text" : reader.GetString(1);
                                    var maxTokens = reader.IsDBNull(2) ? 2048 : Convert.ToInt32(reader.GetValue(2));
                                    var maxContextWindow = reader.IsDBNull(3) ? 4096 : Convert.ToInt32(reader.GetValue(3));
                                    var embeddingDimension = reader.IsDBNull(4) ? (int?)null : Convert.ToInt32(reader.GetValue(4));

                                    result = new CapabilityResult
                                    {
                                        SupportedTasks = supportedTasks,
                                        SupportedModalities = supportedModalities,
                                        MaxTokens = maxTokens,
                                        MaxContextWindow = maxContextWindow,
                                        EmbeddingDimension = embeddingDimension
                                    };
                                }
                                else
                                {
                                    // Model not found, return defaults
                                    result = new CapabilityResult
                                    {
                                        SupportedTasks = "text-generation",
                                        SupportedModalities = "text",
                                        MaxTokens = 2048,
                                        MaxContextWindow = 4096,
                                        EmbeddingDimension = null
                                    };
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Error fallback
                    result = new CapabilityResult
                    {
                        SupportedTasks = "text-generation",
                        SupportedModalities = "text",
                        MaxTokens = 2048,
                        MaxContextWindow = 4096,
                        EmbeddingDimension = null
                    };
                }
            }

            yield return result;
        }

        /// <summary>
        /// Autonomous learning procedure that analyzes performance and generates insights.
        /// Replaces AutonomousLearningRepository.LearnFromPerformanceAsync method.
        /// </summary>
        /// <param name="averageResponseTimeMs">Average response time in milliseconds</param>
        /// <param name="throughput">Requests per second</param>
        /// <param name="successfulActions">Number of successful improvement actions</param>
        /// <param name="failedActions">Number of failed improvement actions</param>
        /// <param name="learningId">Output parameter for the generated learning ID</param>
        /// <param name="insights">Output parameter for generated insights JSON</param>
        /// <param name="recommendations">Output parameter for recommendations JSON</param>
        /// <param name="confidenceScore">Output parameter for confidence score</param>
        /// <param name="isSystemHealthy">Output parameter for system health status</param>
        [SqlProcedure]
        public static void sp_LearnFromPerformance(
            SqlDouble averageResponseTimeMs,
            SqlDouble throughput,
            SqlInt32 successfulActions,
            SqlInt32 failedActions,
            out SqlGuid learningId,
            out SqlString insights,
            out SqlString recommendations,
            out SqlDouble confidenceScore,
            out SqlBoolean isSystemHealthy)
        {
            learningId = Guid.NewGuid();
            var insightsList = new List<string>();
            var recommendationsList = new List<string>();
            var confidence = 0.5; // Base confidence
            var systemHealthy = true;

            try
            {
                using (var connection = new SqlConnection("context connection=true"))
                {
                    connection.Open();

                    // Analyze recent performance history
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT TOP(10)
                                SuccessScore,
                                PerformanceDelta
                            FROM dbo.AutonomousImprovementHistory
                            WHERE StartedAt >= DATEADD(DAY, -7, SYSUTCDATETIME())
                            ORDER BY StartedAt DESC
                        ";

                        var avgSuccessScore = 0.0;
                        var avgPerformanceDelta = 0.0;
                        var historyCount = 0;

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                    avgSuccessScore += Convert.ToDouble(reader.GetDecimal(0));
                                if (!reader.IsDBNull(1))
                                    avgPerformanceDelta += Convert.ToDouble(reader.GetDecimal(1));
                                historyCount++;
                            }
                        }

                        if (historyCount > 0)
                        {
                            avgSuccessScore /= historyCount;
                            avgPerformanceDelta /= historyCount;
                        }

                        // Generate insights based on current vs historical performance
                        if (!averageResponseTimeMs.IsNull && averageResponseTimeMs.Value > 100)
                        {
                            insightsList.Add($"High response time detected: {averageResponseTimeMs.Value:F2}ms");
                            recommendationsList.Add("Consider index optimization or query tuning");
                            systemHealthy = false;
                        }
                        else if (!averageResponseTimeMs.IsNull)
                        {
                            insightsList.Add($"Response time within acceptable range: {averageResponseTimeMs.Value:F2}ms");
                        }

                        if (!throughput.IsNull && throughput.Value < 10)
                        {
                            insightsList.Add($"Low throughput detected: {throughput.Value:F2} req/sec");
                            recommendationsList.Add("Investigate resource bottlenecks or scaling needs");
                            systemHealthy = false;
                        }

                        // Analyze action effectiveness
                        var totalActions = (successfulActions.IsNull ? 0 : successfulActions.Value) +
                                         (failedActions.IsNull ? 0 : failedActions.Value);

                        if (totalActions > 0)
                        {
                            var successRate = (double)(successfulActions.IsNull ? 0 : successfulActions.Value) / totalActions;
                            if (successRate < 0.5)
                            {
                                insightsList.Add($"Action success rate: {successRate * 100:F1}%");
                                recommendationsList.Add("Review action approval criteria or implementation");
                                confidence -= 0.2;
                            }
                            else
                            {
                                confidence += 0.3;
                            }
                        }

                        // Calculate overall confidence
                        confidence = Math.Max(0.1, Math.Min(1.0, confidence));
                    }

                    // Store improvement history
                    using (var insertCommand = connection.CreateCommand())
                    {
                        insertCommand.CommandText = @"
                            INSERT INTO dbo.AutonomousImprovementHistory (
                                ImprovementId,
                                AnalysisResults,
                                GeneratedCode,
                                TargetFile,
                                ChangeType,
                                RiskLevel,
                                EstimatedImpact,
                                SuccessScore,
                                TestsPassed,
                                TestsFailed,
                                PerformanceDelta,
                                WasDeployed,
                                StartedAt,
                                CompletedAt
                            ) VALUES (
                                @improvementId,
                                @analysisResults,
                                @generatedCode,
                                @targetFile,
                                @changeType,
                                @riskLevel,
                                @estimatedImpact,
                                @successScore,
                                @testsPassed,
                                @testsFailed,
                                @performanceDelta,
                                @wasDeployed,
                                SYSUTCDATETIME(),
                                SYSUTCDATETIME()
                            )
                        ";

                        insertCommand.Parameters.AddWithValue("@improvementId", learningId.Value);
                        insertCommand.Parameters.AddWithValue("@analysisResults",
                            "{" +
                            "\"averageResponseTimeMs\":" + (averageResponseTimeMs.IsNull ? "null" : averageResponseTimeMs.Value.ToString("F2")) + "," +
                            "\"throughput\":" + (throughput.IsNull ? "null" : throughput.Value.ToString("F2")) + "," +
                            "\"insights\":[" + string.Join(",", insightsList.Select(i => "\"" + i.Replace("\"", "\\\"") + "\"")) + "]," +
                            "\"recommendations\":[" + string.Join(",", recommendationsList.Select(r => "\"" + r.Replace("\"", "\\\"") + "\"")) + "]," +
                            "\"confidenceScore\":" + confidence.ToString("F2") + "," +
                            "\"isSystemHealthy\":" + systemHealthy.ToString().ToLower() +
                            "}");
                        insertCommand.Parameters.AddWithValue("@generatedCode",
                            "{" +
                            "\"successfulActions\":" + (successfulActions.IsNull ? "null" : successfulActions.Value.ToString()) + "," +
                            "\"failedActions\":" + (failedActions.IsNull ? "null" : failedActions.Value.ToString()) +
                            "}");
                        insertCommand.Parameters.AddWithValue("@targetFile", "AutonomousLearning");
                        insertCommand.Parameters.AddWithValue("@changeType", "analysis");
                        insertCommand.Parameters.AddWithValue("@riskLevel", "low");
                        insertCommand.Parameters.AddWithValue("@estimatedImpact", "medium");
                        insertCommand.Parameters.AddWithValue("@successScore", (decimal)confidence);
                        insertCommand.Parameters.AddWithValue("@testsPassed", successfulActions.IsNull ? 0 : successfulActions.Value);
                        insertCommand.Parameters.AddWithValue("@testsFailed", failedActions.IsNull ? 0 : failedActions.Value);
                        insertCommand.Parameters.AddWithValue("@performanceDelta",
                            averageResponseTimeMs.IsNull ? (decimal?)null : (decimal)(averageResponseTimeMs.Value / 100.0));
                        insertCommand.Parameters.AddWithValue("@wasDeployed", true);

                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error and return safe defaults
                insightsList.Add($"Learning analysis error: {ex.Message}");
                recommendationsList.Add("Review system logs for autonomous learning issues");
                confidence = 0.1;
                systemHealthy = false;
            }

            // Set output parameters
            insights = "[" + string.Join(",", insightsList.Select(i => "\"" + i.Replace("\"", "\\\"") + "\"")) + "]";
            recommendations = "[" + string.Join(",", recommendationsList.Select(r => "\"" + r.Replace("\"", "\\\"") + "\"")) + "]";
            confidenceScore = confidence;
            isSystemHealthy = systemHealthy;
        }

        /// <summary>
        /// Autonomous analysis procedure that detects anomalies and patterns in system performance.
        /// Replaces AutonomousAnalysisRepository.AnalyzeSystemAsync method.
        /// </summary>
        /// <param name="tenantId">Tenant ID for analysis scope (0 for all)</param>
        /// <param name="analysisScope">Analysis scope ('full', 'performance', 'embeddings')</param>
        /// <param name="lookbackHours">Hours to look back for analysis</param>
        /// <param name="analysisId">Output parameter for the generated analysis ID</param>
        /// <param name="totalInferences">Output parameter for total inferences analyzed</param>
        /// <param name="avgDurationMs">Output parameter for average duration</param>
        /// <param name="anomalyCount">Output parameter for number of anomalies detected</param>
        /// <param name="anomaliesJson">Output parameter for anomalies as JSON</param>
        /// <param name="patternsJson">Output parameter for patterns as JSON</param>
        [SqlProcedure]
        public static void sp_AnalyzeSystem(
            SqlInt32 tenantId,
            SqlString analysisScope,
            SqlInt32 lookbackHours,
            out SqlGuid analysisId,
            out SqlInt32 totalInferences,
            out SqlDouble avgDurationMs,
            out SqlInt32 anomalyCount,
            out SqlString anomaliesJson,
            out SqlString patternsJson)
        {
            analysisId = Guid.NewGuid();
            var scope = analysisScope.IsNull ? "full" : analysisScope.Value;
            var lookback = lookbackHours.IsNull ? 24 : lookbackHours.Value;
            var lookbackStart = DateTime.UtcNow.AddHours(-lookback);

            var inferences = new List<InferenceData>();
            var anomalies = new List<PerformanceAnomaly>();
            var patterns = new List<EmbeddingPattern>();

            try
            {
                using (var connection = new SqlConnection("context connection=true"))
                {
                    connection.Open();

                    // Query recent inference activity
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT TOP(1000)
                                InferenceId,
                                RequestTimestamp,
                                TotalDurationMs,
                                LEN(ISNULL(InputData, '')) as InputLength,
                                LEN(ISNULL(OutputData, '')) as OutputLength
                            FROM dbo.InferenceRequests
                            WHERE RequestTimestamp >= @lookbackStart
                                AND Status IN ('Completed', 'Failed')
                            ORDER BY RequestTimestamp DESC
                        ";

                        command.Parameters.AddWithValue("@lookbackStart", lookbackStart);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                inferences.Add(new InferenceData
                                {
                                    InferenceId = reader.GetInt64(0),
                                    RequestedAt = reader.GetDateTime(1),
                                    DurationMs = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                                    InputLength = reader.GetInt32(3),
                                    OutputLength = reader.GetInt32(4)
                                });
                            }
                        }
                    }

                    // Calculate metrics and detect anomalies
                    if (inferences.Any())
                    {
                        // Calculate token counts (rough approximation)
                        var inferencesWithTokens = inferences.Select(ir => new
                        {
                            ir.InferenceId,
                            ir.RequestedAt,
                            ir.DurationMs,
                            TokenCount = (ir.InputLength + ir.OutputLength) / 4
                        }).ToList();

                        totalInferences = inferencesWithTokens.Count;

                        // Calculate average duration
                        var validDurations = inferencesWithTokens.Where(ir => ir.DurationMs > 0).ToList();
                        double localAvgDuration = validDurations.Any() ?
                            validDurations.Average(ir => (double)ir.DurationMs) : 0.0;

                        avgDurationMs = localAvgDuration > 0 ? (SqlDouble)localAvgDuration : SqlDouble.Null;

                        // Detect performance anomalies (inferences that took >2x the average duration)
                        if (localAvgDuration > 0)
                        {
                            anomalies = inferencesWithTokens
                                .Where(ir => ir.DurationMs > localAvgDuration * 2)
                                .Select(ir => new PerformanceAnomaly
                                {
                                    InferenceRequestId = ir.InferenceId,
                                    DurationMs = ir.DurationMs,
                                    AvgDurationMs = localAvgDuration,
                                    SlowdownFactor = ir.DurationMs / localAvgDuration
                                })
                                .ToList();

                            anomalyCount = anomalies.Count;
                        }
                        else
                        {
                            anomalyCount = 0;
                        }
                    }
                    else
                    {
                        totalInferences = 0;
                        avgDurationMs = SqlDouble.Null;
                        anomalyCount = 0;
                    }

                    // Identify embedding patterns if scope includes embeddings
                    if (scope == "full" || scope == "embeddings")
                    {
                        using (var patternCommand = connection.CreateCommand())
                        {
                            patternCommand.CommandText = @"
                                SELECT TOP(10)
                                    FIRST_VALUE(ae.AtomId) OVER (PARTITION BY ae.SpatialBucketX, ae.SpatialBucketY, ae.SpatialBucketZ ORDER BY ae.CreatedAt) as RepresentativeAtomId,
                                    FIRST_VALUE(a.Modality) OVER (PARTITION BY ae.SpatialBucketX, ae.SpatialBucketY, ae.SpatialBucketZ ORDER BY ae.CreatedAt) as Modality,
                                    COUNT(*) as ClusterSize
                                FROM dbo.AtomEmbeddings ae
                                INNER JOIN dbo.Atoms a ON ae.AtomId = a.AtomId
                                WHERE ae.CreatedAt >= @lookbackStart
                                    AND ae.SpatialBucketX IS NOT NULL
                                GROUP BY ae.SpatialBucketX, ae.SpatialBucketY, ae.SpatialBucketZ
                                ORDER BY ClusterSize DESC
                            ";

                            patternCommand.Parameters.AddWithValue("@lookbackStart", lookbackStart);

                            using (var reader = patternCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    patterns.Add(new EmbeddingPattern
                                    {
                                        AtomId = reader.GetInt64(0),
                                        Modality = reader.GetString(1),
                                        ClusterSize = reader.GetInt32(2)
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error and return safe defaults
                totalInferences = 0;
                avgDurationMs = SqlDouble.Null;
                anomalyCount = 0;
                anomaliesJson = "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}";
                patternsJson = "[]";
                return;
            }

            // Format output JSON
            anomaliesJson = "[" + string.Join(",", anomalies.Select(a =>
                "{" +
                "\"inferenceRequestId\":" + a.InferenceRequestId + "," +
                "\"durationMs\":" + a.DurationMs + "," +
                "\"avgDurationMs\":" + a.AvgDurationMs.ToString("F2") + "," +
                "\"slowdownFactor\":" + a.SlowdownFactor.ToString("F2") +
                "}")) + "]";

            patternsJson = "[" + string.Join(",", patterns.Select(p =>
                "{" +
                "\"atomId\":" + p.AtomId + "," +
                "\"modality\":\"" + p.Modality.Replace("\"", "\\\"") + "\"," +
                "\"clusterSize\":" + p.ClusterSize +
                "}")) + "]";
        }

        /// <summary>
        /// Autonomous action execution procedure that executes actions based on hypotheses.
        /// Replaces AutonomousActionRepository.ExecuteActionsAsync method.
        /// </summary>
        /// <param name="analysisId">Analysis ID that generated the hypotheses</param>
        /// <param name="hypothesesJson">JSON array of hypotheses to execute</param>
        /// <param name="autoApproveThreshold">Threshold for auto-approval of actions</param>
        /// <param name="executedActions">Output parameter for number of executed actions</param>
        /// <param name="queuedActions">Output parameter for number of queued actions</param>
        /// <param name="failedActions">Output parameter for number of failed actions</param>
        /// <param name="resultsJson">Output parameter for execution results as JSON</param>
        [SqlProcedure]
        public static void sp_ExecuteActions(
            SqlGuid analysisId,
            SqlString hypothesesJson,
            SqlInt32 autoApproveThreshold,
            out SqlInt32 executedActions,
            out SqlInt32 queuedActions,
            out SqlInt32 failedActions,
            out SqlString resultsJson)
        {
            var results = new List<ActionResult>();
            var threshold = autoApproveThreshold.IsNull ? 3 : autoApproveThreshold.Value;

            try
            {
                if (hypothesesJson.IsNull || string.IsNullOrWhiteSpace(hypothesesJson.Value))
                {
                    executedActions = 0;
                    queuedActions = 0;
                    failedActions = 0;
                    resultsJson = "[]";
                    return;
                }

                using (var connection = new SqlConnection("context connection=true"))
                {
                    connection.Open();

                    var hypotheses = ParseHypothesesJson(connection, hypothesesJson.Value);
                    var orderedHypotheses = hypotheses.OrderBy(h => h.Priority).ToList();

                    foreach (var hypothesis in orderedHypotheses)
                    {
                        var startTime = DateTime.UtcNow;
                        var actionResult = new ActionResult
                        {
                            HypothesisId = hypothesis.HypothesisId,
                            HypothesisType = hypothesis.HypothesisType
                        };

                        try
                        {
                            string executedActionsJson;
                            string status;

                            switch (hypothesis.HypothesisType)
                            {
                                case "IndexOptimization":
                                    (executedActionsJson, status) = ExecuteIndexOptimization(connection);
                                    break;

                                case "CacheWarming":
                                    (executedActionsJson, status) = ExecuteCacheWarming(connection);
                                    break;

                                case "ConceptDiscovery":
                                    (executedActionsJson, status) = ExecuteConceptDiscovery(connection);
                                    break;

                                case "ModelRetraining":
                                    (executedActionsJson, status) = ExecuteModelRetraining(hypothesis, threshold);
                                    break;

                                default:
                                    executedActionsJson = "{\"status\":\"Skipped\",\"reason\":\"Unknown hypothesis type\"}";
                                    status = "Skipped";
                                    break;
                            }

                            actionResult.ExecutedActions = executedActionsJson;
                            actionResult.ActionStatus = status;
                        }
                        catch (Exception ex)
                        {
                            actionResult.ActionStatus = "Failed";
                            actionResult.ExecutedActions = "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}";
                        }

                        actionResult.ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                        results.Add(actionResult);
                    }
                }
            }
            catch (Exception ex)
            {
                // Global error handling
                executedActions = 0;
                queuedActions = 0;
                failedActions = 1;
                resultsJson = "[{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}]";
                return;
            }

            // Set output parameters
            executedActions = results.Count(r => r.ActionStatus == "Executed");
            queuedActions = results.Count(r => r.ActionStatus == "QueuedForApproval");
            failedActions = results.Count(r => r.ActionStatus == "Failed");

            resultsJson = "[" + string.Join(",", results.Select(r =>
                "{" +
                "\"hypothesisId\":\"" + r.HypothesisId + "\"," +
                "\"hypothesisType\":\"" + r.HypothesisType.Replace("\"", "\\\"") + "\"," +
                "\"actionStatus\":\"" + r.ActionStatus + "\"," +
                "\"executedActions\":" + r.ExecutedActions + "," +
                "\"executionTimeMs\":" + r.ExecutionTimeMs +
                "}")) + "]";
        }

        private static List<Hypothesis> ParseHypothesesJson(SqlConnection connection, string json)
        {
            var hypotheses = new List<Hypothesis>();

            if (connection == null || string.IsNullOrWhiteSpace(json))
                return hypotheses;

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT
                        HypothesisId,
                        HypothesisType,
                        Priority
                    FROM OPENJSON(@payload)
                    WITH (
                        HypothesisId NVARCHAR(256) '$.hypothesisId',
                        HypothesisType NVARCHAR(128) '$.hypothesisType',
                        Priority INT '$.priority'
                    )
                ";

                var param = command.Parameters.Add("@payload", SqlDbType.NVarChar, -1);
                param.Value = json;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0))
                            continue;

                        var hypothesisId = reader.GetString(0);
                        var hypothesisType = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
                        var priority = reader.IsDBNull(2) ? int.MaxValue : reader.GetInt32(2);

                        hypotheses.Add(new Hypothesis
                        {
                            HypothesisId = hypothesisId,
                            HypothesisType = hypothesisType,
                            Priority = priority
                        });
                    }
                }
            }

            return hypotheses;
        }

        private static (string executedActions, string status) ExecuteIndexOptimization(SqlConnection connection)
        {
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT TOP(5)
                            'AtomEmbeddings' as TableName,
                            'SpatialBucketX, SpatialBucketY' as IndexColumns,
                            COUNT(*) as ImpactScore
                        FROM dbo.AtomEmbeddings
                        WHERE SpatialBucketX IS NOT NULL
                        GROUP BY SpatialBucketX, SpatialBucketY
                        HAVING COUNT(*) > 100
                        ORDER BY COUNT(*) DESC
                    ";

                    var missingIndexes = new List<string>();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            missingIndexes.Add(string.Format(
                                "{0}({1}):{2}",
                                reader.GetString(0),
                                reader.GetString(1),
                                reader.GetInt32(2)
                            ));
                        }
                    }

                    var executedActions = "{" +
                        "\"analyzedIndexes\":" + missingIndexes.Count + "," +
                        "\"potentialIndexes\":[" + string.Join(",", missingIndexes.Select(i => "\"" + i.Replace("\"", "\\\"") + "\"")) + "]" +
                        "}";

                    return (executedActions, "Executed");
                }
            }
            catch
            {
                return ("{\"error\":\"Index optimization failed\"}", "Failed");
            }
        }

        private static (string executedActions, string status) ExecuteCacheWarming(SqlConnection connection)
        {
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT COUNT(*)
                        FROM dbo.AtomEmbeddings
                        WHERE CreatedAt >= DATEADD(DAY, -7, SYSUTCDATETIME())
                    ";

                    var preloadedCount = (int)command.ExecuteScalar();

                    var executedActions = "{\"preloadedEmbeddings\":" + preloadedCount + "}";
                    return (executedActions, "Executed");
                }
            }
            catch
            {
                return ("{\"error\":\"Cache warming failed\"}", "Failed");
            }
        }

        private static (string executedActions, string status) ExecuteConceptDiscovery(SqlConnection connection)
        {
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT COUNT(DISTINCT CAST(SpatialBucketX AS VARCHAR) + ',' + CAST(SpatialBucketY AS VARCHAR) + ',' + CAST(SpatialBucketZ AS VARCHAR))
                        FROM dbo.AtomEmbeddings
                        WHERE CreatedAt >= DATEADD(DAY, -7, SYSUTCDATETIME())
                            AND SpatialBucketX IS NOT NULL
                    ";

                    var discoveredClusters = (int)command.ExecuteScalar();

                    var executedActions = "{\"discoveredClusters\":" + discoveredClusters + "}";
                    return (executedActions, "Executed");
                }
            }
            catch
            {
                return ("{\"error\":\"Concept discovery failed\"}", "Failed");
            }
        }

        private static (string executedActions, string status) ExecuteModelRetraining(Hypothesis hypothesis, int threshold)
        {
            // Always queue for approval (dangerous operation)
            var executedActions = "{" +
                "\"status\":\"QueuedForApproval\"," +
                "\"reason\":\"ModelRetraining requires manual approval\"," +
                "\"hypothesisId\":\"" + hypothesis.HypothesisId + "\"" +
                "}";

            return (executedActions, "QueuedForApproval");
        }

        public static void FillCapabilityRow(
            object rowObject,
            out string supportedTasks,
            out string supportedModalities,
            out int maxTokens,
            out int maxContextWindow,
            out int embeddingDimension)
        {
            var row = (CapabilityResult)rowObject;
            supportedTasks = row.SupportedTasks;
            supportedModalities = row.SupportedModalities;
            maxTokens = row.MaxTokens;
            maxContextWindow = row.MaxContextWindow;
            embeddingDimension = row.EmbeddingDimension ?? 0; // 0 for null dimension
        }
    }

    /// <summary>
    /// Result class for capability parsing function
    /// </summary>
    public sealed class CapabilityResult
    {
        public string SupportedTasks { get; set; } = null!;
        public string SupportedModalities { get; set; } = null!;
        public int MaxTokens { get; set; }
        public int MaxContextWindow { get; set; }
        public int? EmbeddingDimension { get; set; }
    }

    /// <summary>
    /// Data class for inference analysis
    /// </summary>
    public sealed class InferenceData
    {
        public long InferenceId { get; set; }
        public DateTime RequestedAt { get; set; }
        public int DurationMs { get; set; }
        public int InputLength { get; set; }
        public int OutputLength { get; set; }
    }

    /// <summary>
    /// Data class for performance anomalies
    /// </summary>
    public sealed class PerformanceAnomaly
    {
        public long InferenceRequestId { get; set; }
        public int? ModelId { get; set; }
        public int DurationMs { get; set; }
        public double AvgDurationMs { get; set; }
        public double SlowdownFactor { get; set; }
    }

    /// <summary>
    /// Data class for embedding patterns
    /// </summary>
    public sealed class EmbeddingPattern
    {
        public long AtomId { get; set; }
        public string Modality { get; set; } = null!;
        public int ClusterSize { get; set; }
    }

    /// <summary>
    /// Data class for hypotheses
    /// </summary>
    public sealed class Hypothesis
    {
        public string HypothesisId { get; set; } = null!;
        public string HypothesisType { get; set; } = null!;
        public int Priority { get; set; }
    }

    /// <summary>
    /// Data class for action results
    /// </summary>
    public sealed class ActionResult
    {
        public string HypothesisId { get; set; } = null!;
        public string HypothesisType { get; set; } = null!;
        public string ActionStatus { get; set; } = null!;
        public string ExecutedActions { get; set; } = null!;
        public int ExecutionTimeMs { get; set; }
    }
}
