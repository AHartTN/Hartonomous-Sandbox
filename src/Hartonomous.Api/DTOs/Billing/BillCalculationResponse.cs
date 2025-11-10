using System;
using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Billing
{
    public class BillCalculationResponse
    {
        public int TenantId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public List<UsageBreakdownItem>? UsageBreakdown { get; set; }
    }
}
