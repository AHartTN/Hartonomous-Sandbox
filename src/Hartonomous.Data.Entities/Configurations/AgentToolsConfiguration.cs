using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hartonomous.Data.Entities.Configurations;

public class AgentToolsConfiguration : IEntityTypeConfiguration<AgentTools>
{
    public void Configure(EntityTypeBuilder<AgentTools> builder)
    {
        builder.ToTable("AgentTools", "dbo");
        builder.HasKey(e => new { e.ToolId });

        builder.Property(e => e.ToolId)
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            ;

        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime2")
            ;

        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(2000)")
            .HasMaxLength(2000)
            ;

        builder.Property(e => e.IsEnabled)
            .HasColumnType("bit")
            ;

        builder.Property(e => e.ObjectName)
            .HasColumnType("nvarchar(256)")
            .HasMaxLength(256)
            .IsRequired()
            ;

        builder.Property(e => e.ObjectType)
            .HasColumnType("nvarchar(128)")
            .HasMaxLength(128)
            .IsRequired()
            ;

        builder.Property(e => e.ParametersJson)
            .HasColumnType("json")
            ;

        builder.Property(e => e.ToolCategory)
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            ;

        builder.Property(e => e.ToolName)
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired()
            ;

        builder.HasIndex(e => new { e.ToolName })
            .HasDatabaseName("UQ__AgentToo__006DA2713DA67B8A")
            .IsUnique()
            ;
    }
}
