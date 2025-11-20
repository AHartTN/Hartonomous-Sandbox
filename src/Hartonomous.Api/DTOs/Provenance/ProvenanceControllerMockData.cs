using Hartonomous.Api.DTOs.MLOps;
using Hartonomous.Api.DTOs.Reasoning;

namespace Hartonomous.Api.DTOs.Provenance;

/// <summary>
/// Mock data generators for ProvenanceController demo endpoints.
/// </summary>
public static class ProvenanceControllerMockData
{
    public static List<LineageNode> GenerateMockLineageNodes(long atomId, int maxDepth)
    {
        var nodes = new List<LineageNode>
        {
            new() { AtomId = atomId, Type = LineageNodeType.Result, Depth = 0, Label = "Final Inference", Confidence = 0.94, Location = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = [-122.4194, 37.7749] } },
            new() { AtomId = atomId - 1, Type = LineageNodeType.Transform, Depth = 1, Label = "Semantic Synthesis", Confidence = 0.89, Location = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = [-122.4210, 37.7745] } },
            new() { AtomId = atomId - 2, Type = LineageNodeType.Source, Depth = 2, Label = "Knowledge Atom #1", Confidence = 0.92, Location = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = [-122.4180, 37.7755] } },
            new() { AtomId = atomId - 3, Type = LineageNodeType.Source, Depth = 2, Label = "Knowledge Atom #2", Confidence = 0.87, Location = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = [-122.4200, 37.7740] } },
            new() { AtomId = atomId - 4, Type = LineageNodeType.Validation, Depth = 1, Label = "Error Check", Confidence = 0.96, Location = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = [-122.4190, 37.7752] } }
        };

        return nodes;
    }

    public static List<LineageEdge> GenerateMockLineageEdges(long atomId)
    {
        return new List<LineageEdge>
        {
            new() { From = atomId - 1, To = atomId, Type = "PRODUCES", Weight = 0.89, Label = "synthesis" },
            new() { From = atomId - 2, To = atomId - 1, Type = "CONTRIBUTES", Weight = 0.73, Label = "semantic_match" },
            new() { From = atomId - 3, To = atomId - 1, Type = "CONTRIBUTES", Weight = 0.68, Label = "context_support" },
            new() { From = atomId - 4, To = atomId, Type = "VALIDATES", Weight = 0.96, Label = "error_free" }
        };
    }

    public static SpatialVisualizationData GenerateMockSpatialData(long atomId)
    {
        return new SpatialVisualizationData
        {
            Type = GeoJsonType.FeatureCollection,
            Features = new List<GeoJsonFeature>
            {
                new() 
                { 
                    Type = GeoJsonType.Feature,
                    Geometry = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = [-122.4194, 37.7749] },
                    Properties = new Dictionary<string, object> { ["atomId"] = atomId, ["type"] = "result", ["confidence"] = 0.94 }
                },
                new() 
                { 
                    Type = GeoJsonType.Feature,
                    Geometry = new GeoJsonLineString 
                    { 
                        Type = GeoJsonType.LineString, 
                        Coordinates = new List<double[]> { new[] { -122.4180, 37.7755 }, new[] { -122.4194, 37.7749 } } 
                    },
                    Properties = new Dictionary<string, object> { ["type"] = "lineage", ["weight"] = 0.89 }
                }
            },
            BoundingBox = new double[] { -122.4220, 37.7730, -122.4170, 37.7765 }
        };
    }

    public static List<ReasoningPath> GenerateMockSessionPaths(long sessionId)
    {
        return new List<ReasoningPath>
        {
            new() 
            { 
                PathId = "path_1", 
                Status = PathStatus.Completed, 
                Confidence = 0.92, 
                Steps = 7, 
                Waypoints = new List<GeoJsonPoint> 
                {
                    new() { Type = GeoJsonType.Point, Coordinates = [-122.4194, 37.7749] },
                    new() { Type = GeoJsonType.Point, Coordinates = [-122.4200, 37.7755] },
                    new() { Type = GeoJsonType.Point, Coordinates = [-122.4210, 37.7760] }
                }
            },
            new() 
            { 
                PathId = "path_2", 
                Status = PathStatus.Pruned, 
                Confidence = 0.67, 
                Steps = 4, 
                Reason = "Low confidence threshold",
                Waypoints = new List<GeoJsonPoint> 
                {
                    new() { Type = GeoJsonType.Point, Coordinates = [-122.4194, 37.7749] },
                    new() { Type = GeoJsonType.Point, Coordinates = [-122.4180, 37.7740] }
                }
            }
        };
    }

    public static SpatialVisualizationData GenerateMockPathSpatialData()
    {
        return new SpatialVisualizationData
        {
            Type = GeoJsonType.FeatureCollection,
            Features = new List<GeoJsonFeature>
            {
                new() 
                { 
                    Type = GeoJsonType.Feature,
                    Geometry = new GeoJsonLineString 
                    { 
                        Type = GeoJsonType.LineString, 
                        Coordinates = new List<double[]> 
                        { 
                            new[] { -122.4194, 37.7749 }, 
                            new[] { -122.4200, 37.7755 },
                            new[] { -122.4210, 37.7760 }
                        } 
                    },
                    Properties = new Dictionary<string, object> { ["pathId"] = "path_1", ["status"] = "completed", ["confidence"] = 0.92 }
                }
            },
            BoundingBox = new double[] { -122.4220, 37.7740, -122.4180, 37.7765 }
        };
    }

    public static List<ErrorCluster> GenerateMockErrorClusters()
    {
        return new List<ErrorCluster>
        {
            new() 
            { 
                ClusterId = "cluster_1", 
                ErrorType = "SemanticAmbiguity", 
                ErrorCount = 34, 
                Centroid = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = [-122.4195, 37.7750] },
                Radius = 0.5,
                FirstOccurrence = DateTime.UtcNow.AddDays(-6),
                LastOccurrence = DateTime.UtcNow.AddHours(-3),
                Severity = ErrorSeverity.Medium
            },
            new() 
            { 
                ClusterId = "cluster_2", 
                ErrorType = "ConfidenceThreshold", 
                ErrorCount = 21, 
                Centroid = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = [-122.4210, 37.7765] },
                Radius = 0.3,
                FirstOccurrence = DateTime.UtcNow.AddDays(-4),
                LastOccurrence = DateTime.UtcNow.AddHours(-12),
                Severity = ErrorSeverity.Low
            }
        };
    }

    public static SpatialVisualizationData GenerateMockErrorHeatmap()
    {
        return new SpatialVisualizationData
        {
            Type = GeoJsonType.FeatureCollection,
            Features = new List<GeoJsonFeature>
            {
                new() 
                { 
                    Type = GeoJsonType.Feature,
                    Geometry = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = [-122.4195, 37.7750] },
                    Properties = new Dictionary<string, object> { ["intensity"] = 34, ["errorType"] = "SemanticAmbiguity" }
                },
                new() 
                { 
                    Type = GeoJsonType.Feature,
                    Geometry = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = [-122.4210, 37.7765] },
                    Properties = new Dictionary<string, object> { ["intensity"] = 21, ["errorType"] = "ConfidenceThreshold" }
                }
            },
            BoundingBox = new double[] { -122.4220, 37.7740, -122.4180, 37.7770 }
        };
    }

    public static List<InfluencingAtom> GenerateMockInfluences(long atomId, double minInfluence)
    {
        return new List<InfluencingAtom>
        {
            new() 
            { 
                AtomId = atomId - 5, 
                InfluenceWeight = 0.89, 
                InfluenceType = InfluenceType.Direct,
                Label = "Primary Context Source",
                Location = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = [-122.4180, 37.7755] },
                Distance = 0.8
            },
            new() 
            { 
                AtomId = atomId - 12, 
                InfluenceWeight = 0.67, 
                InfluenceType = InfluenceType.Indirect,
                Label = "Supporting Evidence",
                Location = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = [-122.4200, 37.7740] },
                Distance = 1.2
            },
            new() 
            { 
                AtomId = atomId - 23, 
                InfluenceWeight = 0.42, 
                InfluenceType = InfluenceType.Indirect,
                Label = "Historical Pattern",
                Location = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = [-122.4215, 37.7760] },
                Distance = 2.3
            }
        };
    }

    public static SpatialVisualizationData GenerateMockInfluenceSpatialData()
    {
        return new SpatialVisualizationData
        {
            Type = GeoJsonType.FeatureCollection,
            Features = new List<GeoJsonFeature>
            {
                new() 
                { 
                    Type = GeoJsonType.Feature,
                    Geometry = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = [-122.4180, 37.7755] },
                    Properties = new Dictionary<string, object> { ["influence"] = 0.89, ["type"] = "direct" }
                },
                new() 
                { 
                    Type = GeoJsonType.Feature,
                    Geometry = new GeoJsonLineString 
                    { 
                        Type = GeoJsonType.LineString, 
                        Coordinates = new List<double[]> 
                        { 
                            new[] { -122.4180, 37.7755 }, 
                            new[] { -122.4194, 37.7749 }
                        } 
                    },
                    Properties = new Dictionary<string, object> { ["weight"] = 0.89 }
                }
            },
            BoundingBox = new double[] { -122.4220, 37.7735, -122.4170, 37.7770 }
        };
    }
}
