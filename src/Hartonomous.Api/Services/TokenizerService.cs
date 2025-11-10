using Dapper;
using Hartonomous.Api.DTOs.Tokenizer;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Hartonomous.Api.Services
{
    public class TokenizerService
    {
        private readonly string _connectionString;
        private readonly ILogger<TokenizerService> _logger;

        public TokenizerService(IConfiguration configuration, ILogger<TokenizerService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public async Task<TokenizeResponse> TokenizeAsync(TokenizeRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Tokenizing text using dbo.sp_TokenizeText, starting with: '{StartText}...'", request.Text.Substring(0, Math.Min(request.Text.Length, 50)));

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                var parameters = new DynamicParameters();
                parameters.Add("@text", request.Text, DbType.String, ParameterDirection.Input);
                parameters.Add("@tokenIdsJson", dbType: DbType.String, direction: ParameterDirection.Output, size: -1); // -1 for MAX

                await connection.ExecuteAsync("dbo.sp_TokenizeText", parameters, commandType: CommandType.StoredProcedure);

                string tokenIdsJson = parameters.Get<string>("@tokenIdsJson");

                if (string.IsNullOrEmpty(tokenIdsJson))
                {
                    return new TokenizeResponse { TokenIds = new List<int>() };
                }

                var tokenIds = JsonConvert.DeserializeObject<List<int>>(tokenIdsJson);

                return new TokenizeResponse { TokenIds = tokenIds ?? new List<int>() };
            }
        }
    }
}
