using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Server;

namespace SqlClrFunctions
{
    /// <summary>
    /// Text analytics helpers to accelerate semantic feature extraction inside SQL Server.
    /// </summary>
    public static class SemanticAnalysis
    {
        private static readonly KeywordTopic[] Topics =
        {
            new KeywordTopic("topicTechnical", new []
            {
                Weighted("database", 1.0), Weighted("sql", 1.0), Weighted("server", 0.8),
                Weighted("query", 0.9), Weighted("index", 0.9), Weighted("algorithm", 1.0),
                Weighted("programming", 1.0), Weighted("code", 0.9), Weighted("function", 0.8),
                Weighted("api", 0.9), Weighted("system", 0.7), Weighted("software", 0.9),
                Weighted("data", 0.8), Weighted("computer", 0.8), Weighted("network", 0.9)
            }),
            new KeywordTopic("topicBusiness", new []
            {
                Weighted("revenue", 1.0), Weighted("profit", 1.0), Weighted("customer", 0.9),
                Weighted("market", 0.9), Weighted("sales", 0.9), Weighted("strategy", 0.8),
                Weighted("management", 0.8), Weighted("finance", 1.0), Weighted("investment", 0.9),
                Weighted("business", 1.0), Weighted("enterprise", 0.8), Weighted("commerce", 0.9)
            }),
            new KeywordTopic("topicScientific", new []
            {
                Weighted("research", 1.0), Weighted("theory", 0.9), Weighted("experiment", 1.0),
                Weighted("hypothesis", 1.0), Weighted("scientific", 1.0), Weighted("analysis", 0.8),
                Weighted("study", 0.8), Weighted("method", 0.7), Weighted("evidence", 0.9),
                Weighted("quantum", 1.0), Weighted("physics", 1.0), Weighted("biology", 1.0),
                Weighted("chemistry", 1.0)
            }),
            new KeywordTopic("topicCreative", new []
            {
                Weighted("design", 1.0), Weighted("art", 1.0), Weighted("creative", 1.0),
                Weighted("aesthetic", 0.9), Weighted("visual", 0.8), Weighted("style", 0.7),
                Weighted("artistic", 1.0), Weighted("beautiful", 0.8), Weighted("imagine", 0.8),
                Weighted("inspire", 0.9)
            })
        };

        private static readonly KeywordTopic SentimentPositive = new KeywordTopic("positive", new[]
        {
            Weighted("good", 0.3), Weighted("great", 0.4), Weighted("excellent", 0.5),
            Weighted("improve", 0.2), Weighted("better", 0.2), Weighted("optimize", 0.2)
        });

        private static readonly KeywordTopic SentimentNegative = new KeywordTopic("negative", new[]
        {
            Weighted("bad", -0.3), Weighted("poor", -0.3), Weighted("terrible", -0.4),
            Weighted("worse", -0.2), Weighted("fail", -0.25)
        });

        private static readonly Regex WordRegex = new Regex("[A-Za-z0-9']+", RegexOptions.Compiled);

        /// <summary>
        /// Compute semantic feature scores as JSON for downstream ingestion.
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlString ComputeSemanticFeatures(SqlString input)
        {
            if (input.IsNull)
            {
                return SqlString.Null;
            }

            string text = input.Value;
            string lower = text.ToLowerInvariant();

            var words = WordRegex.Matches(lower).Cast<Match>().Select(m => m.Value).ToArray();
            int wordCount = words.Length;
            int textLength = text.Length;
            double avgWordLength = wordCount == 0 ? 0 : words.Average(w => w.Length);
            double uniqueWordRatio = wordCount == 0 ? 0 : words.Distinct().Count() / (double)wordCount;

            var featureMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (var topic in Topics)
            {
                featureMap[topic.Name] = ScoreTopic(lower, topic);
            }

            double sentiment = ScoreTopic(lower, SentimentPositive) + ScoreTopic(lower, SentimentNegative);
            sentiment = Math.Max(-1.0, Math.Min(1.0, sentiment));

            double complexity;
            if (avgWordLength > 7)
            {
                complexity = 0.8;
            }
            else if (avgWordLength > 5)
            {
                complexity = 0.5;
            }
            else
            {
                complexity = 0.3;
            }

            double formality;
            if (featureMap["topicTechnical"] > 0.5 || featureMap["topicScientific"] > 0.5)
            {
                formality = 0.8;
            }
            else if (featureMap["topicBusiness"] > 0.3)
            {
                formality = 0.6;
            }
            else
            {
                formality = 0.3;
            }

            const double temporalRelevance = 1.0;

            var builder = new StringBuilder(256);
            builder.Append('{');
            AppendJsonProperty(builder, "topicTechnical", featureMap["topicTechnical"]);
            builder.Append(',');
            AppendJsonProperty(builder, "topicBusiness", featureMap["topicBusiness"]);
            builder.Append(',');
            AppendJsonProperty(builder, "topicScientific", featureMap["topicScientific"]);
            builder.Append(',');
            AppendJsonProperty(builder, "topicCreative", featureMap["topicCreative"]);
            builder.Append(',');
            AppendJsonProperty(builder, "sentimentScore", sentiment);
            builder.Append(',');
            AppendJsonProperty(builder, "formalityScore", formality);
            builder.Append(',');
            AppendJsonProperty(builder, "complexityScore", complexity);
            builder.Append(',');
            AppendJsonProperty(builder, "temporalRelevance", temporalRelevance);
            builder.Append(',');
            AppendJsonProperty(builder, "textLength", textLength);
            builder.Append(',');
            AppendJsonProperty(builder, "wordCount", wordCount);
            builder.Append(',');
            AppendJsonProperty(builder, "avgWordLength", avgWordLength);
            builder.Append(',');
            AppendJsonProperty(builder, "uniqueWordRatio", uniqueWordRatio);
            builder.Append('}');

            return new SqlString(builder.ToString());
        }

        private static Keyword Weighted(string keyword, double weight) => new Keyword(keyword, weight);

        private static double ScoreTopic(string text, KeywordTopic topic)
        {
            double totalWeight = 0;
            double totalMatched = 0;

            foreach (var keyword in topic.Keywords)
            {
                totalWeight += Math.Abs(keyword.Weight);
                if (text.IndexOf(keyword.Word, StringComparison.Ordinal) >= 0)
                {
                    totalMatched += keyword.Weight;
                }
            }

            if (totalWeight == 0)
            {
                return 0;
            }

            double score = totalMatched / totalWeight;
            return Math.Max(0, Math.Min(1, score));
        }

        private static void AppendJsonProperty(StringBuilder builder, string name, double value)
        {
            builder.Append('"');
            builder.Append(name);
            builder.Append('"');
            builder.Append(':');
            builder.Append(value.ToString("0.################", CultureInfo.InvariantCulture));
        }

        private readonly struct KeywordTopic
        {
            public KeywordTopic(string name, IReadOnlyList<Keyword> keywords)
            {
                Name = name;
                Keywords = keywords;
            }

            public string Name { get; }
            public IReadOnlyList<Keyword> Keywords { get; }
        }

        private readonly struct Keyword
        {
            public Keyword(string word, double weight)
            {
                Word = word;
                Weight = weight;
            }

            public string Word { get; }
            public double Weight { get; }
        }
    }
}
