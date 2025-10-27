namespace ModelIngestion
{
    public interface IModelReader
    {
        Model Read(string modelPath);
    }
}
