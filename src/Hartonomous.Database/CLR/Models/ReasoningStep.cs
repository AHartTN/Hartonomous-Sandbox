namespace Hartonomous.Clr.Models
{
    /// <summary>
    /// Single step in a reasoning chain (CoT, ToT, Reflexion).
    /// </summary>
    public struct ReasoningStep
    {
        /// <summary>
        /// Step number in reasoning sequence
        /// </summary>
        public int StepNumber { get; set; }

        /// <summary>
        /// Reasoning text for this step
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Embedding vector for spatial reasoning
        /// </summary>
        public float[] Embedding { get; set; }

        /// <summary>
        /// Confidence score [0, 1]
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Optional: Parent step index (for tree-based reasoning)
        /// </summary>
        public int? ParentStep { get; set; }

        /// <summary>
        /// Optional: Branch index (for parallel path exploration)
        /// </summary>
        public int? BranchIndex { get; set; }

        public ReasoningStep(int stepNumber, string text, float[] embedding, float confidence)
        {
            StepNumber = stepNumber;
            Text = text;
            Embedding = embedding;
            Confidence = confidence;
            ParentStep = null;
            BranchIndex = null;
        }
    }
}
