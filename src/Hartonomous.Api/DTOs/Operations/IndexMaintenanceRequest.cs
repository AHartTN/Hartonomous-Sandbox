namespace Hartonomous.Api.DTOs.Operations;

public class IndexMaintenanceRequest
{
    public string? IndexName { get; set; }
    public string? TableName { get; set; }
    public string Operation { get; set; } = "rebuild"; // rebuild, reorganize, update_statistics
    public int? FillFactor { get; set; }
    public bool Online { get; set; } = true;
}
