using Hartonomous.Api.DTOs.Billing;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Text.Json;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// API controller for billing operations including usage tracking, invoicing, and quota management.
/// Supports multi-tenant billing with volume discounts and quota enforcement.
/// </summary>
[Route("api/billing")]
[Authorize]
public sealed class BillingController : ApiControllerBase
{
    private readonly string _connectionString;
    private readonly ILogger<BillingController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BillingController"/> class.
    /// </summary>
    /// <param name="configuration">Configuration containing connection strings.</param>
    /// <param name="logger">Logger for tracking billing operations and errors.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration or logger is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when connection string is not configured.</exception>
    public BillingController(
        IConfiguration configuration,
        ILogger<BillingController> logger)
    {
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a usage report for a tenant with real-time metrics and trend analysis.
    /// </summary>
    /// <param name="request">Report parameters including tenant ID, report type, and time range.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>OK (200) with usage summary if successful; BadRequest (400) for validation errors; InternalServerError (500) for database failures.</returns>
    [HttpPost("usage/report")]
    [ProducesResponseType(typeof(ApiResponse<UsageReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UsageReportResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UsageReportResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UsageReportResponse>>> GetUsageReportAsync(
        [FromBody] UsageReportRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<UsageReportResponse>(new[] { ValidationError("Request body is required.") }));
        }

        // TODO: Add authorization check - user can only query their own tenant's usage unless Admin
        // Get tenantId from claims: var tenantIdClaim = User.FindFirst("tenantId")?.Value;

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new SqlCommand("dbo.sp_GenerateUsageReport", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            command.Parameters.AddWithValue("@TenantId", request.TenantId ?? 1); // Default tenant if not specified
            command.Parameters.AddWithValue("@ReportType", request.ReportType);
            command.Parameters.AddWithValue("@TimeRange", request.TimeRange);

            // sp_GenerateUsageReport returns JSON via FOR JSON PATH
            var jsonResult = new StringBuilder();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            if (!reader.HasRows)
            {
                jsonResult.Append("[]");
            }
            else
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    jsonResult.Append(reader.GetValue(0).ToString());
                }
            }

            // Deserialize JSON to typed objects
            var usageSummaries = JsonSerializer.Deserialize<List<UsageTypeSummary>>(jsonResult.ToString())
                ?? new List<UsageTypeSummary>();

            var response = new UsageReportResponse
            {
                UsageSummaries = usageSummaries
            };

            var metadata = new Dictionary<string, object?>
            {
                ["reportType"] = request.ReportType,
                ["timeRange"] = request.TimeRange,
                ["summaryCount"] = usageSummaries.Count,
                ["totalCost"] = usageSummaries.Sum(s => s.TotalCost)
            };

            _logger.LogInformation("Generated {ReportType} usage report for tenant {TenantId}: {Count} usage types, total cost ${TotalCost:F2}",
                request.ReportType, request.TenantId, usageSummaries.Count, metadata["totalCost"]);

            return Ok(Success(response, metadata));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error generating usage report for tenant {TenantId}", request.TenantId);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to generate usage report", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<UsageReportResponse>(new[] { error }));
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error parsing usage report for tenant {TenantId}", request.TenantId);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to parse usage report data");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<UsageReportResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating usage report for tenant {TenantId}", request.TenantId);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred while generating usage report.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<UsageReportResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Calculates bill for a tenant with volume discounts and tax.
    /// Optionally generates an invoice record in the database.
    /// </summary>
    /// <param name="request">Billing parameters including tenant ID, billing period, and invoice generation flag.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>OK (200) with bill calculation if successful; BadRequest (400) for validation errors; InternalServerError (500) for database failures.</returns>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(ApiResponse<BillCalculationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BillCalculationResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BillCalculationResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BillCalculationResponse>>> CalculateBillAsync(
        [FromBody] CalculateBillRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<BillCalculationResponse>(new[] { ValidationError("Request body is required.") }));
        }

        if (request.BillingPeriodEnd.HasValue && request.BillingPeriodStart.HasValue &&
            request.BillingPeriodEnd.Value < request.BillingPeriodStart.Value)
        {
            return BadRequest(Failure<BillCalculationResponse>(new[] { ValidationError("BillingPeriodEnd must be after BillingPeriodStart.") }));
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new SqlCommand("dbo.sp_CalculateBill", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            command.Parameters.AddWithValue("@TenantId", request.TenantId);
            command.Parameters.AddWithValue("@BillingPeriodStart", request.BillingPeriodStart ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@BillingPeriodEnd", request.BillingPeriodEnd ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@GenerateInvoice", request.GenerateInvoice);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return NotFound(Failure<BillCalculationResponse>(new[] { ErrorDetailFactory.NotFound("billing data", request.TenantId.ToString()) }));
            }

            var response = new BillCalculationResponse
            {
                TenantId = reader.GetInt32(0),
                PeriodStart = reader.GetDateTime(1),
                PeriodEnd = reader.GetDateTime(2),
                Subtotal = reader.GetDecimal(3),
                DiscountPercent = reader.GetDecimal(4),
                Discount = reader.GetDecimal(5),
                Tax = reader.GetDecimal(6),
                Total = reader.GetDecimal(7),
                UsageBreakdown = null
            };

            // Parse UsageBreakdown JSON (column index 8)
            if (!reader.IsDBNull(8))
            {
                var usageBreakdownJson = reader.GetString(8);
                response.UsageBreakdown = JsonSerializer.Deserialize<List<UsageBreakdownItem>>(usageBreakdownJson);
            }

            var metadata = new Dictionary<string, object?>
            {
                ["billingPeriod"] = $"{response.PeriodStart:yyyy-MM-dd} to {response.PeriodEnd:yyyy-MM-dd}",
                ["invoiceGenerated"] = request.GenerateInvoice,
                ["discountApplied"] = response.DiscountPercent > 0
            };

            _logger.LogInformation("Calculated bill for tenant {TenantId}: ${Total:F2} (Subtotal: ${Subtotal:F2}, Discount: {DiscountPercent}%, Tax: ${Tax:F2})",
                request.TenantId, response.Total, response.Subtotal, response.DiscountPercent, response.Tax);

            return Ok(Success(response, metadata));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error calculating bill for tenant {TenantId}", request.TenantId);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to calculate bill", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<BillCalculationResponse>(new[] { error }));
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error parsing usage breakdown for tenant {TenantId}", request.TenantId);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to parse usage breakdown");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<BillCalculationResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calculating bill for tenant {TenantId}", request.TenantId);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred while calculating bill.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<BillCalculationResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Records a usage event for billing purposes.
    /// Checks quota limits and raises error if exceeded.
    /// </summary>
    /// <param name="request">Usage record parameters including tenant ID, usage type, quantity, and unit type.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>OK (200) if successful; BadRequest (400) for validation errors or quota violations; InternalServerError (500) for database failures.</returns>
    [HttpPost("usage/record")]
    [ProducesResponseType(typeof(ApiResponse<RecordUsageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RecordUsageResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RecordUsageResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<RecordUsageResponse>>> RecordUsageAsync(
        [FromBody] RecordUsageRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<RecordUsageResponse>(new[] { ValidationError("Request body is required.") }));
        }

        if (string.IsNullOrWhiteSpace(request.UsageType))
        {
            return BadRequest(Failure<RecordUsageResponse>(new[] { MissingField(nameof(request.UsageType)) }));
        }

        if (string.IsNullOrWhiteSpace(request.UnitType))
        {
            return BadRequest(Failure<RecordUsageResponse>(new[] { MissingField(nameof(request.UnitType)) }));
        }

        if (request.Quantity <= 0)
        {
            return BadRequest(Failure<RecordUsageResponse>(new[] { ValidationError("Quantity must be greater than zero.", nameof(request.Quantity)) }));
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new SqlCommand("dbo.sp_RecordUsage", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            command.Parameters.AddWithValue("@TenantId", request.TenantId);
            command.Parameters.AddWithValue("@UsageType", request.UsageType);
            command.Parameters.AddWithValue("@Quantity", request.Quantity);
            command.Parameters.AddWithValue("@UnitType", request.UnitType);
            command.Parameters.AddWithValue("@CostPerUnit", request.CostPerUnit ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Metadata", request.Metadata ?? (object)DBNull.Value);

            var returnValue = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
            returnValue.Direction = ParameterDirection.ReturnValue;

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                var returnCode = (int)returnValue.Value;
                var success = returnCode == 0;

                var response = new RecordUsageResponse
                {
                    Success = success,
                    Message = success ? "Usage recorded successfully" : "Failed to record usage",
                    QuotaExceeded = false
                };

                _logger.LogInformation("Recorded usage for tenant {TenantId}: {UsageType} = {Quantity} {UnitType}",
                    request.TenantId, request.UsageType, request.Quantity, request.UnitType);

                return Ok(Success(response));
            }
            catch (SqlException ex) when (ex.Message.Contains("Quota exceeded"))
            {
                // Quota violation raised by stored procedure
                _logger.LogWarning("Quota exceeded for tenant {TenantId}: {UsageType}", request.TenantId, request.UsageType);

                var response = new RecordUsageResponse
                {
                    Success = false,
                    Message = ex.Message,
                    QuotaExceeded = true
                };

                return BadRequest(Failure<RecordUsageResponse>(new[] { ErrorDetailFactory.Create("QuotaExceeded", ex.Message) }));
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error recording usage for tenant {TenantId}", request.TenantId);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to record usage", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<RecordUsageResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error recording usage for tenant {TenantId}", request.TenantId);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred while recording usage.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<RecordUsageResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Gets quota information for a tenant and usage type.
    /// Returns current usage and quota limit.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="usageType">Usage type to query (e.g., TokenUsage, VectorSearch).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>OK (200) with quota details if found; NotFound (404) if no quota configured; InternalServerError (500) for database failures.</returns>
    [HttpGet("quota")]
    [ProducesResponseType(typeof(ApiResponse<QuotaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QuotaResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<QuotaResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<QuotaResponse>>> GetQuotaAsync(
        [FromQuery] int tenantId,
        [FromQuery] string usageType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(usageType))
        {
            return BadRequest(Failure<QuotaResponse>(new[] { MissingField(nameof(usageType)) }));
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Query quota and current usage
            var query = @"
                SELECT 
                    tq.TenantId,
                    tq.UsageType,
                    tq.QuotaLimit,
                    ISNULL(SUM(ul.Quantity), 0) AS CurrentUsage,
                    tq.IsActive
                FROM billing.TenantQuotas tq
                LEFT JOIN billing.UsageLedger ul ON ul.TenantId = tq.TenantId 
                    AND ul.UsageType = tq.UsageType
                    AND ul.RecordedUtc >= DATEADD(MONTH, -1, SYSUTCDATETIME())
                WHERE tq.TenantId = @TenantId AND tq.UsageType = @UsageType
                GROUP BY tq.TenantId, tq.UsageType, tq.QuotaLimit, tq.IsActive";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TenantId", tenantId);
            command.Parameters.AddWithValue("@UsageType", usageType);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return NotFound(Failure<QuotaResponse>(new[] { ErrorDetailFactory.NotFound("quota", $"TenantId={tenantId}, UsageType={usageType}") }));
            }

            var quotaLimit = reader.GetInt64(2);
            var currentUsage = reader.GetInt64(3);

            var response = new QuotaResponse
            {
                TenantId = reader.GetInt32(0),
                UsageType = reader.GetString(1),
                QuotaLimit = quotaLimit,
                CurrentUsage = currentUsage,
                UsagePercent = quotaLimit > 0 ? (decimal)currentUsage / quotaLimit * 100 : 0,
                IsActive = reader.GetBoolean(4)
            };

            _logger.LogInformation("Retrieved quota for tenant {TenantId}, usage type {UsageType}: {CurrentUsage}/{QuotaLimit} ({UsagePercent:F1}%)",
                tenantId, usageType, currentUsage, quotaLimit, response.UsagePercent);

            return Ok(Success(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error retrieving quota for tenant {TenantId}, usage type {UsageType}", tenantId, usageType);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to retrieve quota", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<QuotaResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving quota for tenant {TenantId}, usage type {UsageType}", tenantId, usageType);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred while retrieving quota.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<QuotaResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Sets or updates quota limit for a tenant and usage type.
    /// Admin-only endpoint for quota management.
    /// </summary>
    /// <param name="request">Quota parameters including tenant ID, usage type, and quota limit.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>OK (200) with updated quota if successful; BadRequest (400) for validation errors; InternalServerError (500) for database failures.</returns>
    [HttpPost("quota")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<QuotaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QuotaResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<QuotaResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<QuotaResponse>>> SetQuotaAsync(
        [FromBody] QuotaRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<QuotaResponse>(new[] { ValidationError("Request body is required.") }));
        }

        if (string.IsNullOrWhiteSpace(request.UsageType))
        {
            return BadRequest(Failure<QuotaResponse>(new[] { MissingField(nameof(request.UsageType)) }));
        }

        if (request.QuotaLimit < 0)
        {
            return BadRequest(Failure<QuotaResponse>(new[] { ValidationError("QuotaLimit must be non-negative.", nameof(request.QuotaLimit)) }));
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // MERGE into TenantQuotas
            var mergeQuery = @"
                MERGE billing.TenantQuotas AS target
                USING (SELECT @TenantId AS TenantId, @UsageType AS UsageType) AS source
                ON target.TenantId = source.TenantId AND target.UsageType = source.UsageType
                WHEN MATCHED THEN
                    UPDATE SET QuotaLimit = @QuotaLimit, IsActive = @IsActive, UpdatedUtc = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (TenantId, UsageType, QuotaLimit, IsActive, CreatedUtc, UpdatedUtc)
                    VALUES (@TenantId, @UsageType, @QuotaLimit, @IsActive, SYSUTCDATETIME(), SYSUTCDATETIME());";

            await using var mergeCommand = new SqlCommand(mergeQuery, connection);
            mergeCommand.Parameters.AddWithValue("@TenantId", request.TenantId);
            mergeCommand.Parameters.AddWithValue("@UsageType", request.UsageType);
            mergeCommand.Parameters.AddWithValue("@QuotaLimit", request.QuotaLimit);
            mergeCommand.Parameters.AddWithValue("@IsActive", request.IsActive);

            await mergeCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            // Get current usage for response
            var currentUsage = 0L;
            var usageQuery = @"
                SELECT ISNULL(SUM(Quantity), 0)
                FROM billing.UsageLedger
                WHERE TenantId = @TenantId AND UsageType = @UsageType
                      AND RecordedUtc >= DATEADD(MONTH, -1, SYSUTCDATETIME())";

            await using var usageCommand = new SqlCommand(usageQuery, connection);
            usageCommand.Parameters.AddWithValue("@TenantId", request.TenantId);
            usageCommand.Parameters.AddWithValue("@UsageType", request.UsageType);

            var result = await usageCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result != null && result != DBNull.Value)
            {
                currentUsage = Convert.ToInt64(result);
            }

            var response = new QuotaResponse
            {
                TenantId = request.TenantId,
                UsageType = request.UsageType,
                QuotaLimit = request.QuotaLimit,
                CurrentUsage = currentUsage,
                UsagePercent = request.QuotaLimit > 0 ? (decimal)currentUsage / request.QuotaLimit * 100 : 0,
                IsActive = request.IsActive
            };

            _logger.LogInformation("Set quota for tenant {TenantId}, usage type {UsageType}: {QuotaLimit} (current usage: {CurrentUsage})",
                request.TenantId, request.UsageType, request.QuotaLimit, currentUsage);

            return Ok(Success(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error setting quota for tenant {TenantId}, usage type {UsageType}", request.TenantId, request.UsageType);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to set quota", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<QuotaResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error setting quota for tenant {TenantId}, usage type {UsageType}", request.TenantId, request.UsageType);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred while setting quota.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<QuotaResponse>(new[] { error }));
        }
    }
}
