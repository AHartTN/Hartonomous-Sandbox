using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IStatus
{
    int StatusId { get; set; }
    string Code { get; set; }
    string Name { get; set; }
    string? Description { get; set; }
    int SortOrder { get; set; }
    bool IsActive { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
