using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IOperationProvenance
{
    int Id { get; set; }
    Guid OperationId { get; set; }
    DateTime CreatedAt { get; set; }
    ICollection<ProvenanceValidationResult> ProvenanceValidationResults { get; set; }
}
