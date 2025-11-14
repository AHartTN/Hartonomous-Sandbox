using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IAgentTool
{
    long ToolId { get; set; }
    string ToolName { get; set; }
    string? ToolCategory { get; set; }
    string? Description { get; set; }
    string ObjectType { get; set; }
    string ObjectName { get; set; }
    string? ParametersJson { get; set; }
    bool IsEnabled { get; set; }
    DateTime? CreatedAt { get; set; }
}
