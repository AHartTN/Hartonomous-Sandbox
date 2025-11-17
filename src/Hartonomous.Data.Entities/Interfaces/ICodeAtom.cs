using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface ICodeAtom
{
    long CodeAtomId { get; set; }
    string Language { get; set; }
    string Code { get; set; }
    string? Framework { get; set; }
    string? Description { get; set; }
    string? CodeType { get; set; }
    Geometry? Embedding { get; set; }
    int? EmbeddingDimension { get; set; }
    string? TestResults { get; set; }
    float? QualityScore { get; set; }
    int UsageCount { get; set; }
    byte[]? CodeHash { get; set; }
    string? SourceUri { get; set; }
    string? Tags { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? CreatedBy { get; set; }
}
