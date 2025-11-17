using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IVwReconstructModelLayerWeights
{
    int ModelId { get; set; }
    string ModelName { get; set; }
    int LayerIdx { get; set; }
    string? LayerName { get; set; }
    int PositionX { get; set; }
    int PositionY { get; set; }
    int PositionZ { get; set; }
    byte[]? WeightValueBinary { get; set; }
}
