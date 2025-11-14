using System.Linq;
using Hartonomous.Core.Entities;
using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Hartonomous.UnitTests.Core;

public class GenerationStreamConfigurationTests
{
    [Fact]
    public void Configure_BindsAtomicStreamColumn()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        var entityBuilder = modelBuilder.Entity<GenerationStream>();

        new GenerationStreamConfiguration().Configure(entityBuilder);

        var entity = modelBuilder.Model.FindEntityType(typeof(GenerationStream));
        Assert.NotNull(entity);
        Assert.Equal("provenance", entity!.GetSchema());

        var streamProperty = entity.FindProperty(nameof(GenerationStream.ProvenanceStream));
        Assert.NotNull(streamProperty);
        Assert.Equal("varbinary(max)", streamProperty!.GetColumnType());
        Assert.True(streamProperty.IsNullable);

        var scopeIndex = Assert.Single(entity.GetIndexes(), i => i.GetDatabaseName() == "IX_GenerationStreams_Scope");
        Assert.Equal(nameof(GenerationStream.Scope), Assert.Single(scopeIndex.Properties).Name);

        var modelIndex = Assert.Single(entity.GetIndexes(), i => i.GetDatabaseName() == "IX_GenerationStreams_Model");
        Assert.Equal(nameof(GenerationStream.Model), Assert.Single(modelIndex.Properties).Name);
    }
}
