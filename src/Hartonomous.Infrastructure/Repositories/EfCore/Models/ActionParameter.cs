namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

/// <summary>
/// Parameter for an executed action
/// </summary>
public class ActionParameter
{
    public Guid ActionId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string ParameterValue { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }
}
