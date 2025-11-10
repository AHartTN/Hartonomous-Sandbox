using Hartonomous.Api.Common;
using Hartonomous.Api.DTOs.Tokenizer;
using Hartonomous.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Hartonomous.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class TokenizerController : ControllerBase
    {
        private readonly TokenizerService _tokenizerService;
        private readonly ILogger<TokenizerController> _logger;

        public TokenizerController(TokenizerService tokenizerService, ILogger<TokenizerController> logger)
        {
            _tokenizerService = tokenizerService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<TokenizeResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> Tokenize([FromBody] TokenizeRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Request body with text is required."));
            }

            try
            {
                var result = await _tokenizerService.TokenizeAsync(request, cancellationToken);
                return Ok(ApiResponse<TokenizeResponse>.Ok(result));
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "An error occurred during tokenization.");
                return StatusCode(500, ApiResponse<object>.Fail("TOKENIZATION_ERROR", "An unexpected error occurred."));
            }
        }
    }
}
