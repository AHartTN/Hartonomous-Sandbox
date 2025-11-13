using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;

namespace SqlClrFunctions.NaturalLanguage
{
    /// <summary>
    /// Proper Byte Pair Encoding (BPE) tokenizer implementation.
    /// REPLACES: EmbeddingFunctions.cs character-level stub tokenization
    /// 
    /// Implements BPE algorithm for subword tokenization used by transformer models.
    /// Vocabulary and merges should be loaded from TensorAtoms GEOMETRY/binary storage.
    /// </summary>
    public class BpeTokenizer
    {
        private readonly Dictionary<string, int> _vocabulary;
        private readonly List<(string, string)> _merges;
        private readonly int _unknownTokenId;
        private readonly int _maxTokenLength;

        /// <summary>
        /// Initialize BPE tokenizer with vocabulary and merge rules.
        /// </summary>
        /// <param name="vocabulary">Token to ID mapping</param>
        /// <param name="merges">Ordered list of merge rules (token1, token2)</param>
        /// <param name="unknownTokenId">ID for unknown tokens (typically 0 or vocab size)</param>
        /// <param name="maxTokenLength">Maximum sequence length</param>
        public BpeTokenizer(
            Dictionary<string, int> vocabulary,
            List<(string, string)> merges,
            int unknownTokenId = 0,
            int maxTokenLength = 512)
        {
            _vocabulary = vocabulary ?? throw new ArgumentNullException(nameof(vocabulary));
            _merges = merges ?? throw new ArgumentNullException(nameof(merges));
            _unknownTokenId = unknownTokenId;
            _maxTokenLength = maxTokenLength;
        }

        /// <summary>
        /// Tokenize text using BPE algorithm.
        /// </summary>
        public int[] Encode(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<int>();

            // Pre-tokenize: split on whitespace and punctuation
            var words = PreTokenize(text);
            var tokens = new List<int>();

            foreach (var word in words)
            {
                var wordTokens = TokenizeWord(word);
                tokens.AddRange(wordTokens);

                if (tokens.Count >= _maxTokenLength)
                    break;
            }

            // Truncate to max length
            if (tokens.Count > _maxTokenLength)
                tokens = tokens.Take(_maxTokenLength).ToList();

            return tokens.ToArray();
        }

        /// <summary>
        /// Tokenize a single word using BPE merges.
        /// </summary>
        private List<int> TokenizeWord(string word)
        {
            if (string.IsNullOrEmpty(word))
                return new List<int>();

            // Start with character-level tokens
            var symbols = word.Select(c => c.ToString()).ToList();

            // Apply BPE merges
            while (symbols.Count > 1)
            {
                // Find the highest priority merge
                int bestMergeIndex = -1;
                int bestMergePriority = int.MaxValue;

                for (int i = 0; i < symbols.Count - 1; i++)
                {
                    var pair = (symbols[i], symbols[i + 1]);
                    int priority = FindMergePriority(pair);

                    if (priority >= 0 && priority < bestMergePriority)
                    {
                        bestMergePriority = priority;
                        bestMergeIndex = i;
                    }
                }

                // No more merges available
                if (bestMergeIndex == -1)
                    break;

                // Apply the merge
                symbols[bestMergeIndex] = symbols[bestMergeIndex] + symbols[bestMergeIndex + 1];
                symbols.RemoveAt(bestMergeIndex + 1);
            }

            // Convert tokens to IDs
            var tokenIds = new List<int>();
            foreach (var symbol in symbols)
            {
                if (_vocabulary.TryGetValue(symbol, out int id))
                    tokenIds.Add(id);
                else
                    tokenIds.Add(_unknownTokenId);
            }

            return tokenIds;
        }

        /// <summary>
        /// Find priority of a merge pair in the merge list.
        /// Returns -1 if not found, otherwise returns index (lower = higher priority).
        /// </summary>
        private int FindMergePriority((string, string) pair)
        {
            for (int i = 0; i < _merges.Count; i++)
            {
                if (_merges[i] == pair)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Pre-tokenize text into words (split on whitespace/punctuation).
        /// </summary>
        private List<string> PreTokenize(string text)
        {
            var words = new List<string>();
            var currentWord = new System.Text.StringBuilder();

            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (currentWord.Length > 0)
                    {
                        words.Add(currentWord.ToString());
                        currentWord.Clear();
                    }
                }
                else if (char.IsPunctuation(c))
                {
                    if (currentWord.Length > 0)
                    {
                        words.Add(currentWord.ToString());
                        currentWord.Clear();
                    }
                    words.Add(c.ToString());
                }
                else
                {
                    currentWord.Append(c);
                }
            }

            if (currentWord.Length > 0)
                words.Add(currentWord.ToString());

            return words;
        }

        /// <summary>
        /// Load BPE vocabulary from SQL Server TensorAtoms storage.
        /// Should query GEOMETRY or binary tensor data containing vocab JSON.
        /// NOTE: This method should be called from SQL CLR context where SqlConnection is available.
        /// Bridge library provides the algorithm; SQL CLR provides the data access.
        /// </summary>
        public static Dictionary<string, int> LoadVocabularyFromJson(SqlConnection connection, string jsonData)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var vocabulary = new Dictionary<string, int>(StringComparer.Ordinal);

            if (!string.IsNullOrWhiteSpace(jsonData))
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT [key], TokenId
                        FROM OPENJSON(@payload)
                        WITH (TokenId INT '$')
                        WHERE TokenId IS NOT NULL;
                    ";

                    var param = command.Parameters.Add("@payload", SqlDbType.NVarChar, -1);
                    param.Value = jsonData;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.IsDBNull(0) || reader.IsDBNull(1))
                                continue;

                            vocabulary[reader.GetString(0)] = reader.GetInt32(1);
                        }
                    }
                }
            }

            // Fallback: if no vocabulary found, create basic ASCII vocabulary
            if (vocabulary.Count == 0)
            {
                for (int i = 0; i < 256; i++)
                    vocabulary[((char)i).ToString()] = i;
            }

            return vocabulary;
        }

        /// <summary>
        /// Load BPE merges from merge rules text.
        /// NOTE: This method should be called from SQL CLR context.
        /// Bridge library provides the algorithm; SQL CLR provides the data access.
        /// </summary>
        public static List<(string, string)> LoadMergesFromText(string mergesText)
        {
            var merges = new List<(string, string)>();

            if (!string.IsNullOrEmpty(mergesText))
            {
                var lines = mergesText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var parts = line.Split(' ');
                    if (parts.Length == 2)
                        merges.Add((parts[0], parts[1]));
                }
            }

            return merges;
        }

        /// <summary>
        /// Decode token IDs back to text.
        /// </summary>
        public string Decode(int[] tokenIds)
        {
            if (tokenIds == null || tokenIds.Length == 0)
                return string.Empty;

            var reverseVocab = _vocabulary.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            var tokens = new List<string>();

            foreach (var id in tokenIds)
            {
                if (reverseVocab.TryGetValue(id, out string token))
                    tokens.Add(token);
            }

            return string.Join("", tokens);
        }
    }
}
