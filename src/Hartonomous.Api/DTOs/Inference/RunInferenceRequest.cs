using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Inference
{
    /// <summary>
    /// Represents the request payload for the synchronous inference endpoint.
    /// </summary>
    public class RunInferenceRequest
    {
        /// <summary>
        /// The ID of the model to use for inference.
        /// </summary>
        [Required]
        public int ModelId { get; set; }

        /// <summary>
        /// An array of integer token IDs representing the input prompt.
        /// </summary>
        [Required]
        [MinLength(1)]
        public required int[] TokenIds { get; set; }
    }
}
