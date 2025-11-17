using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

/// <summary>
/// Maps Azure Entra External ID tenant GUIDs to internal integer tenant IDs. Replaces unsafe GetHashCode() approach. Each Azure AD tenant GUID gets a stable, unique integer ID for use throughout the system.
/// </summary>
public interface ITenantGuidMapping
{
    int TenantId { get; set; }
    Guid TenantGuid { get; set; }
    string? TenantName { get; set; }
    bool IsActive { get; set; }
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTime? ModifiedAt { get; set; }
    string? ModifiedBy { get; set; }
}
