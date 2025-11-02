using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Repositories;

public class AtomicPixelRepository : IAtomicPixelRepository
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<AtomicPixelRepository> _logger;

    public AtomicPixelRepository(HartonomousDbContext context, ILogger<AtomicPixelRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AtomicPixel?> GetByHashAsync(byte[] pixelHash, CancellationToken cancellationToken = default)
    {
        return await _context.AtomicPixels.FindAsync(new object[] { pixelHash }, cancellationToken);
    }

    public async Task<AtomicPixel> AddAsync(AtomicPixel pixel, CancellationToken cancellationToken = default)
    {
        _context.AtomicPixels.Add(pixel);
        await _context.SaveChangesAsync(cancellationToken);
        return pixel;
    }

    public async Task UpdateReferenceCountAsync(byte[] pixelHash, CancellationToken cancellationToken = default)
    {
        var pixel = await GetByHashAsync(pixelHash, cancellationToken);
        if (pixel != null)
        {
            pixel.ReferenceCount++;
            pixel.LastReferenced = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<long> GetReferenceCountAsync(byte[] pixelHash, CancellationToken cancellationToken = default)
    {
        var pixel = await GetByHashAsync(pixelHash, cancellationToken);
        return pixel?.ReferenceCount ?? 0;
    }
}