using Microsoft.ML.OnnxRuntime;

namespace ModelIngestion
{
    public class OnnxModelReader : IModelReader
    {
        public Model Read(string modelPath)
        {
            var model = new Model();

            using (var session = new InferenceSession(modelPath))
            {
                model.Name = session.ModelMetadata.GraphName;
                model.Type = "ONNX";
                model.Architecture = session.ModelMetadata.Domain;

                foreach (var input in session.InputMetadata)
                {
                    var layer = new Layer
                    {
                        Name = input.Key,
                        Type = "Input",
                        Parameters = new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "shape", input.Value.Dimensions }
                        }
                    };
                    model.Layers.Add(layer);
                }

                foreach (var output in session.OutputMetadata)
                {
                    var layer = new Layer
                    {
                        Name = output.Key,
                        Type = "Output",
                        Parameters = new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "shape", output.Value.Dimensions }
                        }
                    };
                    model.Layers.Add(layer);
                }
            }

            return model;
        }
    }
}
