using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Interfaces.Query;
using Hartonomous.Core.Validation;
using Hartonomous.Data.Entities;
using Hartonomous.Shared.Contracts.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Service for querying atom data with semantic connections.
/// Executes efficient SQL JOINs to retrieve atom details with relationships.
/// </summary>
public class AtomQueryService : ServiceBase<AtomQueryService>, IAtomQueryService
{
    private readonly HartonomousDbContext _context;

    public AtomQueryService(
        HartonomousDbContext context,
        ILogger<AtomQueryService> logger)
        : base(logger)
    {
        _context = Guard.NotNull(context, nameof(context));
    }

    public async Task<AtomDetailDTO?> GetAtomAsync(long atomId)
    {
        return await ExecuteWithTelemetryAsync(
            $"GetAtom ({atomId})",
            async () => await GetAtomInternalAsync(atomId),
            CancellationToken.None);
    }

    private async Task<AtomDetailDTO?> GetAtomInternalAsync(long atomId)
    {
        Guard.Positive(atomId, nameof(atomId));

        // Get the atom
        var atom = await _context.Atoms
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AtomId == atomId);

        if (atom == null)
        {
            Logger.LogWarning("Atom not found: {AtomId}", atomId);
            return null;
        }

        // Build the DTO
        var atomDetail = new AtomDetailDTO
        {
            AtomId = atom.AtomId,
            ContentHash = atom.ContentHash,
            AtomicValue = atom.AtomicValue != null ? Convert.ToBase64String(atom.AtomicValue) : string.Empty,
            CanonicalText = atom.CanonicalText,
            Modality = atom.Modality,
            Subtype = atom.Subtype,
            ContentType = atom.ContentType,
            Metadata = atom.Metadata,
            TenantId = atom.TenantId,
            CreatedAt = DateTime.UtcNow // Atoms don't have CreatedAt, using current time as placeholder
        };

        // Get parent relationships (atoms that this atom is derived from - SourceAtom -> TargetAtom)
        var parents = await _context.AtomRelations
            .AsNoTracking()
            .Where(r => r.TargetAtomId == atomId)
            .Join(
                _context.Atoms,
                relation => relation.SourceAtomId,
                parentAtom => parentAtom.AtomId,
                (relation, parentAtom) => new AtomRelationDTO
                {
                    RelatedAtomId = parentAtom.AtomId,
                    RelationType = relation.RelationType,
                    SemanticSimilarity = relation.Confidence,
                    RelatedAtomicValue = parentAtom.CanonicalText ?? (parentAtom.AtomicValue != null ? Convert.ToBase64String(parentAtom.AtomicValue) : null),
                    RelatedModality = parentAtom.Modality
                })
            .ToListAsync();

        atomDetail.Parents = parents;

        // Get child relationships (atoms derived from this atom - this is SourceAtom)
        var children = await _context.AtomRelations
            .AsNoTracking()
            .Where(r => r.SourceAtomId == atomId)
            .Join(
                _context.Atoms,
                relation => relation.TargetAtomId,
                childAtom => childAtom.AtomId,
                (relation, childAtom) => new AtomRelationDTO
                {
                    RelatedAtomId = childAtom.AtomId,
                    RelationType = relation.RelationType,
                    SemanticSimilarity = relation.Confidence,
                    RelatedAtomicValue = childAtom.CanonicalText ?? (childAtom.AtomicValue != null ? Convert.ToBase64String(childAtom.AtomicValue) : null),
                    RelatedModality = childAtom.Modality
                })
            .ToListAsync();

        atomDetail.Children = children;

        Logger.LogInformation(
            "Retrieved atom {AtomId} with {ParentCount} parents and {ChildCount} children",
            atomId, parents.Count, children.Count);

        return atomDetail;
    }

    public async Task<IEnumerable<AtomDetailDTO>> GetAtomsByHashAsync(byte[] contentHash)
    {
        Guard.NotNullOrEmpty(contentHash, nameof(contentHash));

        var atoms = await _context.Atoms
            .AsNoTracking()
            .Where(a => a.ContentHash == contentHash)
            .Select(a => new AtomDetailDTO
            {
                AtomId = a.AtomId,
                ContentHash = a.ContentHash,
                AtomicValue = a.AtomicValue != null ? Convert.ToBase64String(a.AtomicValue) : string.Empty,
                CanonicalText = a.CanonicalText,
                Modality = a.Modality,
                Subtype = a.Subtype,
                ContentType = a.ContentType,
                Metadata = a.Metadata,
                TenantId = a.TenantId,
                CreatedAt = DateTime.UtcNow
            })
            .ToListAsync();

        Logger.LogInformation("Found {Count} atoms with matching hash", atoms.Count);

        return atoms;
    }

    public async Task<IEnumerable<AtomDetailDTO>> GetAtomsByTenantAsync(int tenantId, int skip = 0, int take = 100)
    {
        if (tenantId < 0) throw new ArgumentException("TenantId must be non-negative", nameof(tenantId));
        if (skip < 0) throw new ArgumentException("Skip must be non-negative", nameof(skip));
        if (take < 1 || take > 1000) throw new ArgumentException("Take must be between 1 and 1000", nameof(take));

        var atoms = await _context.Atoms
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.AtomId) // Use AtomId instead of CreatedAt
            .Skip(skip)
            .Take(take)
            .Select(a => new AtomDetailDTO
            {
                AtomId = a.AtomId,
                ContentHash = a.ContentHash,
                AtomicValue = a.AtomicValue != null ? Convert.ToBase64String(a.AtomicValue) : string.Empty,
                CanonicalText = a.CanonicalText,
                Modality = a.Modality,
                Subtype = a.Subtype,
                ContentType = a.ContentType,
                Metadata = a.Metadata,
                TenantId = a.TenantId,
                CreatedAt = DateTime.UtcNow
            })
            .ToListAsync();

        Logger.LogInformation(
            "Retrieved {Count} atoms for tenant {TenantId} (skip: {Skip}, take: {Take})",
            atoms.Count, tenantId, skip, take);

        return atoms;
    }
}
