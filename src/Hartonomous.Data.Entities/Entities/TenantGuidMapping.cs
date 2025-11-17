using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

/// <summary>
/// Maps Azure Entra External ID tenant GUIDs to internal integer tenant IDs. Replaces unsafe GetHashCode() approach. Each Azure AD tenant GUID gets a stable, unique integer ID for use throughout the system.
/// </summary>
public partial class TenantGuidMapping : ITenantGuidMapping
{
    public int TenantId { get; set; }

    public Guid TenantGuid { get; set; }

    public string? TenantName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public string? ModifiedBy { get; set; }
}
