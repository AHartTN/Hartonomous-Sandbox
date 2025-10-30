using Hartonomous.Core.Entities;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

public class InferenceRepository : BaseLongRepository<InferenceRequest, HartonomousDbContext>, IInferenceRepository
{

    public async Task<InferenceRequest?> GetByIdAsync(long inferenceId, CancellationToken cancellationToken = default)
    {
        return await _context.InferenceRequests
            .Include(ir => ir.Steps)
            .FirstOrDefaultAsync(ir => ir.InferenceId == inferenceId, cancellationToken);
    }

    public async Task<IEnumerable<InferenceRequest>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        return await _context.InferenceRequests
            .OrderByDescending(ir => ir.RequestTimestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InferenceRequest>> GetByTaskTypeAsync(string taskType, int count = 100, CancellationToken cancellationToken = default)
    {
        return await _context.InferenceRequests
            .Where(ir => ir.TaskType == taskType)
            .OrderByDescending(ir => ir.RequestTimestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<InferenceRequest> AddAsync(InferenceRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding inference request for task type: {TaskType}", request.TaskType);
        
        _context.InferenceRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Inference request added with ID: {InferenceId}", request.InferenceId);
        return request;
    }

    public async Task UpdateAsync(InferenceRequest request, CancellationToken cancellationToken = default)
    {
        _context.InferenceRequests.Update(request);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InferenceRequests.CountAsync(cancellationToken);
    }

    public async Task<double> GetAverageDurationAsync(string? taskType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.InferenceRequests.AsQueryable();
        
        if (!string.IsNullOrEmpty(taskType))
        {
            query = query.Where(ir => ir.TaskType == taskType);
        }
        
        return await query
            .Where(ir => ir.TotalDurationMs.HasValue)
            .AverageAsync(ir => ir.TotalDurationMs!.Value, cancellationToken);
    }
}
