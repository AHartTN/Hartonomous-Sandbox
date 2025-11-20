using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.MLOps;


public class ExperimentsResponse
{
    public List<Experiment> ActiveExperiments { get; set; } = new();
    public int CompletedExperiments { get; set; }
    public long TotalSampleSize { get; set; }
    public bool DemoMode { get; set; }
}
