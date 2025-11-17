using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class VwReconstructModelLayerWeight : IVwReconstructModelLayerWeight
{
    public int ModelId { get; set; }

    public string ModelName { get; set; } = null!;

    public int LayerIdx { get; set; }

    public string? LayerName { get; set; }

    public int PositionX { get; set; }

    public int PositionY { get; set; }

    public int PositionZ { get; set; }

    public byte[]? WeightValueBinary { get; set; }
}
