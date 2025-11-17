using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class OperationProvenance : IOperationProvenance
{
    public int Id { get; set; }

    public Guid OperationId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ProvenanceValidationResults> ProvenanceValidationResults { get; set; } = new List<ProvenanceValidationResults>();
}
