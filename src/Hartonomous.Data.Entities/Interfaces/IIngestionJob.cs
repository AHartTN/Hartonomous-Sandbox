using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IIngestionJob
{
    long IngestionJobId { get; set; }
    string PipelineName { get; set; }
    DateTime StartedAt { get; set; }
    DateTime? CompletedAt { get; set; }
    string? Status { get; set; }
    string? SourceUri { get; set; }
    string? Metadata { get; set; }
    ICollection<IngestionJobAtom> IngestionJobAtoms { get; set; }
}
