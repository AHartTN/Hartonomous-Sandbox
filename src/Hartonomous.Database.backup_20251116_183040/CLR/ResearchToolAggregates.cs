using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr
{
    /// <summary>
    /// AUTONOMOUS RESEARCH & TOOL EXECUTION AGGREGATES
    /// Track multi-step research workflows, tool chains, knowledge graph construction
    /// </summary>

    /// <summary>
    /// RESEARCH WORKFLOW AGGREGATE
    /// Track multi-step research process with branching, tool execution, and knowledge accumulation
    /// 
    /// SELECT research_session_id,
    ///        dbo.ResearchWorkflow(step_number, step_type, query_vector, result_vector, 
    ///                             confidence, source_citations, parent_step)
    /// FROM research_steps
    /// GROUP BY research_session_id
    /// 
    /// Returns: Complete research trajectory with quality metrics
    /// USE CASE: "Find me everything about X" - track the entire discovery process
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct ResearchWorkflow : IBinarySerialize
    {
        private enum StepType { Query, Search, Analysis, Synthesis, Verification, ToolExecution }

        private class ResearchStep
        {
            public int StepNumber;
            public StepType Type;
            public float[] QueryVector;
            public float[] ResultVector;
            public double Confidence;
            public List<string> Citations;
            public int ParentStep;
            public List<int> ChildSteps;
            public double NoveltyScore; // How different from previous findings
            public double RelevanceScore; // How relevant to original query

            public ResearchStep()
            {
                Citations = new List<string>();
                ChildSteps = new List<int>();
            }
        }

        private Dictionary<int, ResearchStep> steps;
        private int dimension;
        private float[] originalQueryVector;

        public void Init()
        {
            steps = new Dictionary<int, ResearchStep>();
            dimension = 0;
            originalQueryVector = null;
        }

        public void Accumulate(SqlInt32 stepNumber, SqlString stepType, SqlString queryVectorJson, 
            SqlString resultVectorJson, SqlDouble confidence, SqlString citations, SqlInt32 parentStep)
        {
            if (stepNumber.IsNull || stepType.IsNull) return;

            var queryVec = !queryVectorJson.IsNull ? VectorUtilities.ParseVectorJson(queryVectorJson.Value) : null;
            var resultVec = !resultVectorJson.IsNull ? VectorUtilities.ParseVectorJson(resultVectorJson.Value) : null;

            if (queryVec != null)
            {
                if (dimension == 0)
                {
                    dimension = queryVec.Length;
                    originalQueryVector = queryVec;
                }
                else if (queryVec.Length != dimension)
                    return;
            }

            if (resultVec != null && dimension > 0 && resultVec.Length != dimension)
                return;

            var step = new ResearchStep
            {
                StepNumber = stepNumber.Value,
                Type = ParseStepType(stepType.Value),
                QueryVector = queryVec,
                ResultVector = resultVec,
                Confidence = confidence.IsNull ? 0.5 : confidence.Value,
                ParentStep = parentStep.IsNull ? -1 : parentStep.Value
            };

            if (!citations.IsNull && !string.IsNullOrWhiteSpace(citations.Value))
            {
                step.Citations.AddRange(citations.Value.Split(';').Select(c => c.Trim()));
            }

            steps[step.StepNumber] = step;

            // Link to parent
            if (step.ParentStep >= 0 && steps.ContainsKey(step.ParentStep))
            {
                steps[step.ParentStep].ChildSteps.Add(step.StepNumber);
            }
        }

        public void Merge(ResearchWorkflow other)
        {
            if (other.steps != null)
            {
                foreach (var kvp in other.steps)
                {
                    if (!steps.ContainsKey(kvp.Key))
                        steps[kvp.Key] = kvp.Value;
                }
            }

            if (originalQueryVector == null && other.originalQueryVector != null)
                originalQueryVector = other.originalQueryVector;
        }

        public SqlString Terminate()
        {
            if (steps.Count == 0 || dimension == 0) return SqlString.Null;

            // Compute novelty and relevance scores
            var processedVectors = new List<float[]>();

            foreach (var step in steps.Values.OrderBy(s => s.StepNumber))
            {
                if (step.ResultVector != null)
                {
                    // Novelty: how different from what we've already found
                    if (processedVectors.Count > 0)
                    {
                        double maxSimilarity = processedVectors.Max(v => 
                            VectorMath.CosineSimilarity(step.ResultVector, v));
                        step.NoveltyScore = 1.0 - maxSimilarity;
                    }
                    else
                    {
                        step.NoveltyScore = 1.0;
                    }

                    // Relevance: similarity to original query
                    if (originalQueryVector != null)
                    {
                        step.RelevanceScore = VectorMath.CosineSimilarity(
                            step.ResultVector, originalQueryVector);
                    }

                    processedVectors.Add(step.ResultVector);
                }
            }

            // Compute research quality metrics
            double avgConfidence = steps.Values.Average(s => s.Confidence);
            double avgNovelty = steps.Values.Where(s => s.ResultVector != null).Average(s => s.NoveltyScore);
            double avgRelevance = steps.Values.Where(s => s.ResultVector != null).Average(s => s.RelevanceScore);
            int totalCitations = steps.Values.Sum(s => s.Citations.Count);
            int branchingPoints = steps.Values.Count(s => s.ChildSteps.Count > 1);

            // Find key insights (high novelty + high relevance + high confidence)
            var keyInsights = steps.Values
                .Where(s => s.ResultVector != null && 
                       s.NoveltyScore > 0.5 && 
                       s.RelevanceScore > 0.7 && 
                       s.Confidence > 0.7)
                .OrderByDescending(s => s.NoveltyScore * s.RelevanceScore * s.Confidence)
                .Take(5)
                .ToList();

            // Build JSON
            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"total_steps\":{0},", steps.Count);
            sb.AppendFormat("\"avg_confidence\":{0:G6},", avgConfidence);
            sb.AppendFormat("\"avg_novelty\":{0:G6},", avgNovelty);
            sb.AppendFormat("\"avg_relevance\":{0:G6},", avgRelevance);
            sb.AppendFormat("\"total_citations\":{0},", totalCitations);
            sb.AppendFormat("\"branching_points\":{0},", branchingPoints);
            sb.AppendFormat("\"research_depth\":{0},", ComputeMaxDepth());
            sb.Append("\"key_insights\":[");
            sb.Append(string.Join(",", keyInsights.Select(s =>
                $"{{\"step\":{s.StepNumber}," +
                $"\"type\":\"{s.Type}\"," +
                $"\"novelty\":{s.NoveltyScore:G6}," +
                $"\"relevance\":{s.RelevanceScore:G6}," +
                $"\"confidence\":{s.Confidence:G6}," +
                $"\"citations\":{s.Citations.Count}}}"
            )));
            sb.Append("]}");

            return new SqlString(sb.ToString());
        }

        private int ComputeMaxDepth()
        {
            int maxDepth = 0;
            var roots = steps.Values.Where(s => s.ParentStep < 0).ToList();

            foreach (var root in roots)
            {
                maxDepth = Math.Max(maxDepth, ComputeDepth(root, 1));
            }

            return maxDepth;
        }

        private int ComputeDepth(ResearchStep step, int currentDepth)
        {
            if (step.ChildSteps.Count == 0) return currentDepth;

            int maxChildDepth = currentDepth;
            foreach (int childStep in step.ChildSteps)
            {
                if (steps.ContainsKey(childStep))
                {
                    maxChildDepth = Math.Max(maxChildDepth, ComputeDepth(steps[childStep], currentDepth + 1));
                }
            }

            return maxChildDepth;
        }

        private static StepType ParseStepType(string typeStr)
        {
            return typeStr?.ToLowerInvariant() switch
            {
                "query" => StepType.Query,
                "search" => StepType.Search,
                "analysis" => StepType.Analysis,
                "synthesis" => StepType.Synthesis,
                "verification" => StepType.Verification,
                "tool" or "tool_execution" => StepType.ToolExecution,
                _ => StepType.Query
            };
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            
            bool hasOriginalQuery = r.ReadBoolean();
            if (hasOriginalQuery)
            {
                originalQueryVector = new float[dimension];
                for (int i = 0; i < dimension; i++)
                    originalQueryVector[i] = r.ReadSingle();
            }

            int stepCount = r.ReadInt32();
            steps = new Dictionary<int, ResearchStep>(stepCount);

            for (int i = 0; i < stepCount; i++)
            {
                var step = new ResearchStep
                {
                    StepNumber = r.ReadInt32(),
                    Type = (StepType)r.ReadInt32(),
                    Confidence = r.ReadDouble(),
                    ParentStep = r.ReadInt32(),
                    NoveltyScore = r.ReadDouble(),
                    RelevanceScore = r.ReadDouble()
                };

                bool hasQuery = r.ReadBoolean();
                if (hasQuery)
                {
                    step.QueryVector = new float[dimension];
                    for (int j = 0; j < dimension; j++)
                        step.QueryVector[j] = r.ReadSingle();
                }

                bool hasResult = r.ReadBoolean();
                if (hasResult)
                {
                    step.ResultVector = new float[dimension];
                    for (int j = 0; j < dimension; j++)
                        step.ResultVector[j] = r.ReadSingle();
                }

                int citationCount = r.ReadInt32();
                for (int j = 0; j < citationCount; j++)
                    step.Citations.Add(r.ReadString());

                int childCount = r.ReadInt32();
                for (int j = 0; j < childCount; j++)
                    step.ChildSteps.Add(r.ReadInt32());

                steps[step.StepNumber] = step;
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            
            w.Write(originalQueryVector != null);
            if (originalQueryVector != null)
            {
                for (int i = 0; i < dimension; i++)
                    w.Write(originalQueryVector[i]);
            }

            w.Write(steps.Count);

            foreach (var step in steps.Values)
            {
                w.Write(step.StepNumber);
                w.Write((int)step.Type);
                w.Write(step.Confidence);
                w.Write(step.ParentStep);
                w.Write(step.NoveltyScore);
                w.Write(step.RelevanceScore);

                w.Write(step.QueryVector != null);
                if (step.QueryVector != null)
                {
                    for (int j = 0; j < dimension; j++)
                        w.Write(step.QueryVector[j]);
                }

                w.Write(step.ResultVector != null);
                if (step.ResultVector != null)
                {
                    for (int j = 0; j < dimension; j++)
                        w.Write(step.ResultVector[j]);
                }

                w.Write(step.Citations.Count);
                foreach (var citation in step.Citations)
                    w.Write(citation);

                w.Write(step.ChildSteps.Count);
                foreach (var child in step.ChildSteps)
                    w.Write(child);
            }
        }
    }

    /// <summary>
    /// TOOL EXECUTION CHAIN AGGREGATE
    /// Track sequences of tool calls and their semantic results
    /// 
    /// SELECT session_id,
    ///        dbo.ToolExecutionChain(execution_order, tool_name, input_vector, 
    ///                               output_vector, success, execution_time_ms)
    /// FROM tool_executions
    /// GROUP BY session_id
    /// 
    /// Returns: Tool chain analysis with success rates, bottlenecks, semantic flow
    /// USE CASE: Understand how autonomous agents use tools, optimize workflows
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct ToolExecutionChain : IBinarySerialize
    {
        private class ToolExecution
        {
            public int Order;
            public string ToolName;
            public float[] InputVector;
            public float[] OutputVector;
            public bool Success;
            public long ExecutionTimeMs;
            public double SemanticTransformation; // How much did the vector change
        }

        private List<ToolExecution> executions;
        private int dimension;

        public void Init()
        {
            executions = new List<ToolExecution>();
            dimension = 0;
        }

        public void Accumulate(SqlInt32 executionOrder, SqlString toolName, SqlString inputVectorJson,
            SqlString outputVectorJson, SqlBoolean success, SqlInt64 executionTimeMs)
        {
            if (executionOrder.IsNull || toolName.IsNull) return;

            var inputVec = !inputVectorJson.IsNull ? VectorUtilities.ParseVectorJson(inputVectorJson.Value) : null;
            var outputVec = !outputVectorJson.IsNull ? VectorUtilities.ParseVectorJson(outputVectorJson.Value) : null;

            if (inputVec != null)
            {
                if (dimension == 0)
                    dimension = inputVec.Length;
                else if (inputVec.Length != dimension)
                    return;
            }

            if (outputVec != null && dimension > 0 && outputVec.Length != dimension)
                return;

            executions.Add(new ToolExecution
            {
                Order = executionOrder.Value,
                ToolName = toolName.Value,
                InputVector = inputVec,
                OutputVector = outputVec,
                Success = success.IsNull ? false : success.Value,
                ExecutionTimeMs = executionTimeMs.IsNull ? 0 : executionTimeMs.Value
            });
        }

        public void Merge(ToolExecutionChain other)
        {
            if (other.executions != null)
                executions.AddRange(other.executions);
        }

        public SqlString Terminate()
        {
            if (executions.Count == 0) return SqlString.Null;

            // Sort by order
            executions.Sort((a, b) => a.Order.CompareTo(b.Order));

            // Compute semantic transformations
            for (int i = 0; i < executions.Count; i++)
            {
                var exec = executions[i];
                if (exec.InputVector != null && exec.OutputVector != null)
                {
                    exec.SemanticTransformation = VectorMath.EuclideanDistance(
                        exec.InputVector, exec.OutputVector);
                }
            }

            // Analyze tool usage patterns
            var toolStats = executions
                .GroupBy(e => e.ToolName)
                .Select(g => new
                {
                    Tool = g.Key,
                    Count = g.Count(),
                    SuccessRate = g.Average(e => e.Success ? 1.0 : 0.0),
                    AvgTime = g.Average(e => (double)e.ExecutionTimeMs),
                    AvgTransformation = g.Where(e => e.SemanticTransformation > 0).Average(e => e.SemanticTransformation)
                })
                .OrderByDescending(t => t.Count)
                .ToList();

            // Find bottlenecks (slow executions)
            var avgExecTime = executions.Average(e => (double)e.ExecutionTimeMs);
            var bottlenecks = executions
                .Where(e => e.ExecutionTimeMs > avgExecTime * 2)
                .OrderByDescending(e => e.ExecutionTimeMs)
                .Take(5)
                .ToList();

            // Compute overall metrics
            double successRate = executions.Average(e => e.Success ? 1.0 : 0.0);
            long totalTime = executions.Sum(e => e.ExecutionTimeMs);
            int uniqueTools = toolStats.Count;

            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"total_executions\":{0},", executions.Count);
            sb.AppendFormat("\"unique_tools\":{0},", uniqueTools);
            sb.AppendFormat("\"success_rate\":{0:G6},", successRate);
            sb.AppendFormat("\"total_time_ms\":{0},", totalTime);
            sb.AppendFormat("\"avg_time_ms\":{0:G3},", avgExecTime);
            
            sb.Append("\"tool_usage\":[");
            sb.Append(string.Join(",", toolStats.Select(t =>
                $"{{\"tool\":\"{t.Tool.Replace("\"", "\\\"")}\"," +
                $"\"count\":{t.Count}," +
                $"\"success_rate\":{t.SuccessRate:G6}," +
                $"\"avg_time_ms\":{t.AvgTime:G3}," +
                $"\"avg_semantic_change\":{t.AvgTransformation:G6}}}"
            )));
            sb.Append("],");

            sb.Append("\"bottlenecks\":[");
            sb.Append(string.Join(",", bottlenecks.Select(b =>
                $"{{\"tool\":\"{b.ToolName.Replace("\"", "\\\"")}\"," +
                $"\"order\":{b.Order}," +
                $"\"time_ms\":{b.ExecutionTimeMs}}}"
            )));
            sb.Append("]}");

            return new SqlString(sb.ToString());
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            executions = new List<ToolExecution>(count);

            for (int i = 0; i < count; i++)
            {
                var exec = new ToolExecution
                {
                    Order = r.ReadInt32(),
                    ToolName = r.ReadString(),
                    Success = r.ReadBoolean(),
                    ExecutionTimeMs = r.ReadInt64(),
                    SemanticTransformation = r.ReadDouble()
                };

                bool hasInput = r.ReadBoolean();
                if (hasInput)
                {
                    exec.InputVector = new float[dimension];
                    for (int j = 0; j < dimension; j++)
                        exec.InputVector[j] = r.ReadSingle();
                }

                bool hasOutput = r.ReadBoolean();
                if (hasOutput)
                {
                    exec.OutputVector = new float[dimension];
                    for (int j = 0; j < dimension; j++)
                        exec.OutputVector[j] = r.ReadSingle();
                }

                executions.Add(exec);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(executions.Count);

            foreach (var exec in executions)
            {
                w.Write(exec.Order);
                w.Write(exec.ToolName);
                w.Write(exec.Success);
                w.Write(exec.ExecutionTimeMs);
                w.Write(exec.SemanticTransformation);

                w.Write(exec.InputVector != null);
                if (exec.InputVector != null)
                {
                    for (int j = 0; j < dimension; j++)
                        w.Write(exec.InputVector[j]);
                }

                w.Write(exec.OutputVector != null);
                if (exec.OutputVector != null)
                {
                    for (int j = 0; j < dimension; j++)
                        w.Write(exec.OutputVector[j]);
                }
            }
        }
    }
}
