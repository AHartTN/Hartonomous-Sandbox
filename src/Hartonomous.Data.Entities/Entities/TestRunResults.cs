using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class TestRunResults : ITestRunResults
{
    public DateTime ExecutedAt { get; set; }

    public int? TotalTests { get; set; }

    public int? PassedTests { get; set; }

    public int? FailedTests { get; set; }

    public decimal? Duration { get; set; }

    public string? ResultsXml { get; set; }
}
