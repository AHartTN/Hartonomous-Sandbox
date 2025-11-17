using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface ITestRunResults
{
    DateTime ExecutedAt { get; set; }
    int? TotalTests { get; set; }
    int? PassedTests { get; set; }
    int? FailedTests { get; set; }
    decimal? Duration { get; set; }
    string? ResultsXml { get; set; }
}
