using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class CodeAtom : ICodeAtom
{
    public long CodeAtomId { get; set; }

    public string Language { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Framework { get; set; }

    public string? Description { get; set; }

    public string? CodeType { get; set; }

    public Geometry? Embedding { get; set; }

    public int? EmbeddingDimension { get; set; }

    public string? TestResults { get; set; }

    public float? QualityScore { get; set; }

    public int UsageCount { get; set; }

    public byte[]? CodeHash { get; set; }

    public string? SourceUri { get; set; }

    public string? Tags { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }
}
