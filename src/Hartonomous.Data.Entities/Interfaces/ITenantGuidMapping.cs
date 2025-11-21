using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

/// <summary>
/// Maps Azure Entra External ID tenant GUIDs to stable internal integer tenant IDs. Replaces unsafe GetHashCode() approach with ACID-compliant GUID-to-INT mapping.
/// </summary>
public interface ITenantGuidMapping
{
    int TenantId { get; set; }
    Guid TenantGuid { get; set; }
    string TenantName { get; set; }
    bool IsActive { get; set; }
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTime? ModifiedAt { get; set; }
    string? ModifiedBy { get; set; }
    ICollection<BillingCredit> BillingCredits { get; set; }
    ICollection<BillingPayment> BillingPayments { get; set; }
    ICollection<TenantSubscription> TenantSubscriptions { get; set; }
}
