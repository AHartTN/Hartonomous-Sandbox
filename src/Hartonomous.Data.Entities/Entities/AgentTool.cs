using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class AgentTool : IAgentTool
{
    public long ToolId { get; set; }

    public string ToolName { get; set; } = null!;

    public string? ToolCategory { get; set; }

    public string? Description { get; set; }

    public string ObjectType { get; set; } = null!;

    public string ObjectName { get; set; } = null!;

    public string? ParametersJson { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime? CreatedAt { get; set; }
}
