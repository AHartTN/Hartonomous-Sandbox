using System;
using System.Collections.Generic;
using SqlClrFunctions.Contracts;

namespace SqlClrFunctions.JsonProcessing
{
    /// <summary>
    /// Hypothesis data structure for autonomous improvement.
    /// Must match T-SQL definition.
    /// </summary>
    public class Hypothesis
    {
        public int HypothesisId { get; set; }
        public string HypothesisType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public string SupportingEvidence { get; set; } = string.Empty;
        public string ProposedAction { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Parser for hypothesis JSON using System.Text.Json.
    /// REPLACES: AutonomousFunctions.cs:751 ParseHypothesesJson (which always returned empty list)
    /// </summary>
    public class HypothesisParser
    {
        private readonly IJsonSerializer _serializer;

        public HypothesisParser(IJsonSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// Parse hypothesis JSON into strongly-typed list.
        /// NO MORE "simplified parsing" that returns empty list!
        /// </summary>
        public List<Hypothesis> Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<Hypothesis>();

            try
            {
                return _serializer.DeserializeArray<Hypothesis>(json);
            }
            catch (Exception ex)
            {
                // Log error but return empty list to maintain SQL CLR compatibility
                // In production, would write to SQL error log
                System.Diagnostics.Debug.WriteLine($"Hypothesis parsing failed: {ex.Message}");
                return new List<Hypothesis>();
            }
        }

        /// <summary>
        /// Serialize hypotheses back to JSON.
        /// </summary>
        public string Serialize(List<Hypothesis> hypotheses)
        {
            if (hypotheses == null || hypotheses.Count == 0)
                return "[]";

            return _serializer.Serialize(hypotheses);
        }
    }
}
