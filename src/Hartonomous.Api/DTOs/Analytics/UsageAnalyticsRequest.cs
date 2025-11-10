using System;
using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Analytics
{
    public class UsageAnalyticsRequest
    {
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        public string? Modality { get; set; }
        public string? GroupBy { get; set; } = "day"; // day, week, month
    }
}
