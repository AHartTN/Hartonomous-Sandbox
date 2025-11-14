using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class IngestionJob : IIngestionJob
{
    public long IngestionJobId { get; set; }

    public string PipelineName { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? Status { get; set; }

    public string? SourceUri { get; set; }

    public string? Metadata { get; set; }

    public virtual ICollection<IngestionJobAtom> IngestionJobAtoms { get; set; } = new List<IngestionJobAtom>();
}
