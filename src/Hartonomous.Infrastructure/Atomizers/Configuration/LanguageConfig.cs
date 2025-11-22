namespace Hartonomous.Infrastructure.Atomizers.Configuration;

internal class LanguageConfig
{
    public HashSet<string> Extensions { get; }
    public string? FunctionPattern { get; init; }
    public string? ClassPattern { get; init; }

    public LanguageConfig(params string[] extensions)
    {
        Extensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
    }
}
