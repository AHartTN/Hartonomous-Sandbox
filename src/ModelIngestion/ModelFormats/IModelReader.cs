namespace ModelIngestion.ModelFormats
{
    public interface IModelReader
    {
        Model Read(string modelPath);
    }
}
