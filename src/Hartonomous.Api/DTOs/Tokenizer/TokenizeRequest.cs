using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Tokenizer
{
    public class TokenizeRequest
    {
        [Required]
        [MinLength(1)]
        public required string Text { get; set; }
    }
}
