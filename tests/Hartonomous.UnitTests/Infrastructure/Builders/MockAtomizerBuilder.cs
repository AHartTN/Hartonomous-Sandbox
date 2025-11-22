using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Data.Entities.Entities;
using Moq;
using System.Text;

namespace Hartonomous.UnitTests.Infrastructure.Builders;

/// <summary>
/// Fluent builder for creating mock atomizers in tests.
/// Simplifies test setup and improves readability.
/// </summary>
public class MockAtomizerBuilder
{
    private int _atomCount = 1;
    private string _modality = "text";
    private string _subtype = "chunk";
    private int _priority = 5;
    private bool _canHandle = true;
    private List<AtomData> _customAtoms = new();

    /// <summary>
    /// Sets the number of atoms the atomizer will produce.
    /// </summary>
    public MockAtomizerBuilder WithAtomCount(int count)
    {
        _atomCount = count;
        return this;
    }

    /// <summary>
    /// Sets the modality for generated atoms (e.g., "text", "image", "audio").
    /// </summary>
    public MockAtomizerBuilder WithModality(string modality)
    {
        _modality = modality;
        return this;
    }

    /// <summary>
    /// Sets the subtype for generated atoms.
    /// </summary>
    public MockAtomizerBuilder WithSubtype(string subtype)
    {
        _subtype = subtype;
        return this;
    }

    /// <summary>
    /// Sets the priority for atomizer selection.
    /// </summary>
    public MockAtomizerBuilder WithPriority(int priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>
    /// Sets whether the atomizer can handle specific content types.
    /// </summary>
    public MockAtomizerBuilder CanHandle(bool canHandle)
    {
        _canHandle = canHandle;
        return this;
    }

    /// <summary>
    /// Provides custom atoms instead of auto-generated ones.
    /// </summary>
    public MockAtomizerBuilder WithCustomAtoms(List<AtomData> atoms)
    {
        _customAtoms = atoms;
        return this;
    }

    /// <summary>
    /// Builds the mock atomizer with configured behavior.
    /// </summary>
    public IAtomizer<byte[]> Build()
    {
        var mock = new Mock<IAtomizer<byte[]>>();

        // Setup Priority property
        mock.Setup(x => x.Priority).Returns(_priority);

        // Setup CanHandle method
        mock.Setup(x => x.CanHandle(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(_canHandle);

        // Setup AtomizeAsync method
        mock.Setup(x => x.AtomizeAsync(
                It.IsAny<byte[]>(),
                It.IsAny<SourceMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[] content, SourceMetadata metadata, CancellationToken ct) =>
            {
                var atoms = _customAtoms.Count > 0
                    ? _customAtoms
                    : GenerateDefaultAtoms(content, metadata);

                return new AtomizationResult
                {
                    Atoms = atoms,
                    Compositions = new List<AtomComposition>(),
                    ProcessingInfo = new ProcessingMetadata
                    {
                        TotalAtoms = atoms.Count,
                        UniqueAtoms = atoms.Count,
                        DurationMs = 10,
                        AtomizerType = "MockAtomizer",
                        DetectedFormat = _modality
                    }
                };
            });

        return mock.Object;
    }

    private List<AtomData> GenerateDefaultAtoms(byte[] content, SourceMetadata metadata)
    {
        var atoms = new List<AtomData>();

        for (int i = 0; i < _atomCount; i++)
        {
            var atomContent = $"Atom {i} content";
            var atomBytes = Encoding.UTF8.GetBytes(atomContent);
            var contentHash = System.Security.Cryptography.SHA256.HashData(atomBytes);

            atoms.Add(new AtomData
            {
                AtomicValue = atomBytes.Length <= 64 ? atomBytes : atomBytes.Take(64).ToArray(),
                ContentHash = contentHash,
                Modality = _modality,
                Subtype = _subtype,
                ContentType = metadata.ContentType,
                CanonicalText = atomContent,
                Metadata = $"{{\"index\":{i}}}"
            });
        }

        return atoms;
    }
}
