using System.Linq;
using Hartonomous.Core.Entities;
using Hartonomous.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Hartonomous.Core.Tests;

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

        var streamProperty = entity.FindProperty(nameof(GenerationStream.Stream));
        Assert.NotNull(streamProperty);
        Assert.Equal("provenance.AtomicStream", streamProperty!.GetColumnType());
        Assert.False(streamProperty.IsNullable);

        var payloadProperty = entity.FindProperty(nameof(GenerationStream.PayloadSizeBytes));
        Assert.NotNull(payloadProperty);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, payloadProperty!.ValueGenerated);
        Assert.Equal("CONVERT(BIGINT, DATALENGTH([Stream]))", payloadProperty.GetComputedColumnSql());
        Assert.Equal(PropertySaveBehavior.Ignore, payloadProperty.GetAfterSaveBehavior());

        var scopeIndex = Assert.Single(entity.GetIndexes(), i => i.GetDatabaseName() == "IX_GenerationStreams_Scope");
        Assert.Equal(nameof(GenerationStream.Scope), Assert.Single(scopeIndex.Properties).Name);

        var modelIndex = Assert.Single(entity.GetIndexes(), i => i.GetDatabaseName() == "IX_GenerationStreams_Model");
        Assert.Equal(nameof(GenerationStream.Model), Assert.Single(modelIndex.Properties).Name);
    }
}
